// Summary: Manages downloading and extracting Windows tool archives.
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using Kitsub.Tooling;
using Microsoft.Extensions.Logging;
using SevenZipExtractor;

namespace Kitsub.Tooling.Provisioning;

/// <summary>Represents a resolved tool bundle location.</summary>
public sealed record ToolBundleResult(string BaseDirectory, ToolSource Source, ToolPaths Paths);

/// <summary>Handles provisioning of tool binaries via manifest-defined archives.</summary>
public sealed class ToolBundleManager
{
    private const string LockFileName = ".provision.lock";
    private const string HashManifestName = "tool-hashes.json";
    private static readonly Regex Sha256Regex = new("[0-9a-fA-F]{64}", RegexOptions.Compiled);
    private static readonly HttpClient HttpClient = new();

    private readonly ToolManifestLoader _manifestLoader;
    private readonly ToolCachePaths _cachePaths;
    private readonly ILogger<ToolBundleManager> _logger;
    private ToolsManifest? _manifest;

    /// <summary>Initializes a new instance of the tool bundle manager.</summary>
    public ToolBundleManager(ToolManifestLoader manifestLoader, ToolCachePaths cachePaths, ILogger<ToolBundleManager> logger)
    {
        _manifestLoader = manifestLoader;
        _cachePaths = cachePaths;
        _logger = logger;
    }

    /// <summary>Gets the loaded tools manifest.</summary>
    public ToolsManifest Manifest => _manifest ??= _manifestLoader.Load();

    /// <summary>Attempts to resolve tools from a bundled portable layout.</summary>
    public ToolBundleResult? TryGetBundledToolset(string rid)
    {
        if (!Manifest.Rids.TryGetValue(rid, out var ridEntry))
        {
            _logger.LogWarning("Tools manifest missing RID {Rid}", rid);
            return null;
        }

        var baseDirectory = Path.Combine(AppContext.BaseDirectory, "tools", rid);
        var paths = BuildToolPaths(baseDirectory, ridEntry);
        if (!AreAllToolsPresent(paths))
        {
            _logger.LogDebug("Bundled tools not present for RID {Rid}", rid);
            return null;
        }

        _logger.LogInformation("Using bundled tools from {Directory}", baseDirectory);
        return new ToolBundleResult(baseDirectory, ToolSource.Bundled, paths);
    }

    /// <summary>Ensures the cache toolset is present, downloading and extracting as needed.</summary>
    public async Task<ToolBundleResult?> EnsureCachedToolsetAsync(
        string rid,
        ToolResolveOptions options,
        CancellationToken cancellationToken,
        bool force,
        IProgress<ToolProvisionProgress>? progress = null)
    {
        if (!Manifest.Rids.TryGetValue(rid, out var ridEntry))
        {
            _logger.LogWarning("Tools manifest missing RID {Rid}", rid);
            return null;
        }

        var toolsetRoot = _cachePaths.GetToolsetRoot(rid, Manifest.ToolsetVersion, options.ToolsCacheDir);
        var paths = BuildToolPaths(toolsetRoot, ridEntry);

        if (!force && AreAllToolsPresent(paths) && AreToolHashesValid(paths, toolsetRoot))
        {
            _logger.LogInformation("Using cached tools from {Directory}", toolsetRoot);
            return new ToolBundleResult(toolsetRoot, ToolSource.Cache, paths);
        }

        Directory.CreateDirectory(toolsetRoot);
        using var lockHandle = AcquireLock(Path.Combine(toolsetRoot, LockFileName));

        if (!force && AreAllToolsPresent(paths) && AreToolHashesValid(paths, toolsetRoot))
        {
            _logger.LogInformation("Using cached tools from {Directory}", toolsetRoot);
            return new ToolBundleResult(toolsetRoot, ToolSource.Cache, paths);
        }

        if (options.DryRun)
        {
            _logger.LogInformation("Dry-run enabled; skipping tool provisioning.");
            return new ToolBundleResult(toolsetRoot, ToolSource.Cache, paths);
        }

        _logger.LogInformation("Provisioning tools into cache {Directory}", toolsetRoot);
        await ProvisionToolAsync("ffmpeg", ridEntry.Ffmpeg!, Path.Combine(toolsetRoot, "ffmpeg"), cancellationToken, progress)
            .ConfigureAwait(false);
        await ProvisionToolAsync("mkvtoolnix", ridEntry.Mkvtoolnix!, Path.Combine(toolsetRoot, "mkvtoolnix"), cancellationToken, progress)
            .ConfigureAwait(false);

        WriteToolHashes(paths, toolsetRoot);
        return new ToolBundleResult(toolsetRoot, ToolSource.Cache, paths);
    }

    /// <summary>Deletes the cache directory for a specific toolset.</summary>
    public void CleanCache(string rid, string? toolsCacheDir)
    {
        var toolsetRoot = _cachePaths.GetToolsetRoot(rid, Manifest.ToolsetVersion, toolsCacheDir);
        if (!Directory.Exists(toolsetRoot))
        {
            _logger.LogInformation("Tools cache directory does not exist: {Directory}", toolsetRoot);
            return;
        }

        Directory.Delete(toolsetRoot, recursive: true);
        _logger.LogInformation("Deleted tools cache directory {Directory}", toolsetRoot);
    }

    private static FileStream AcquireLock(string lockPath)
    {
        var directory = Path.GetDirectoryName(lockPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        return new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
    }

    private static ToolPaths BuildToolPaths(string baseDirectory, ToolsManifestRid ridEntry)
    {
        var ffmpegPath = ResolveToolPath(ridEntry.Ffmpeg!, "ffmpeg.exe", baseDirectory, "ffmpeg");
        var ffprobePath = ResolveToolPath(ridEntry.Ffmpeg!, "ffprobe.exe", baseDirectory, "ffmpeg");
        var mkvmergePath = ResolveToolPath(ridEntry.Mkvtoolnix!, "mkvmerge.exe", baseDirectory, "mkvtoolnix");
        var mkvpropeditPath = ResolveToolPath(ridEntry.Mkvtoolnix!, "mkvpropedit.exe", baseDirectory, "mkvtoolnix");

        return new ToolPaths(ffmpegPath, ffprobePath, mkvmergePath, mkvpropeditPath);
    }

    private static string ResolveToolPath(ToolArchiveDefinition definition, string key, string baseDirectory, string toolRoot)
    {
        var relative = definition.ExtractMap[key];
        var normalized = relative.Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(baseDirectory, toolRoot, normalized);
    }

    private static bool AreAllToolsPresent(ToolPaths paths)
    {
        return File.Exists(paths.Ffmpeg) &&
               File.Exists(paths.Ffprobe) &&
               File.Exists(paths.Mkvmerge) &&
               File.Exists(paths.Mkvpropedit);
    }

    private bool AreToolHashesValid(ToolPaths paths, string toolsetRoot)
    {
        var hashPath = Path.Combine(toolsetRoot, HashManifestName);
        if (!File.Exists(hashPath))
        {
            _logger.LogDebug("Tool hash manifest not found at {Path}", hashPath);
            return false;
        }

        var manifest = JsonSerializer.Deserialize<ToolHashManifest>(File.ReadAllText(hashPath));
        if (manifest?.Files is null || manifest.Files.Count == 0)
        {
            _logger.LogDebug("Tool hash manifest invalid at {Path}", hashPath);
            return false;
        }

        return VerifyHash(paths.Ffmpeg, manifest, toolsetRoot) &&
               VerifyHash(paths.Ffprobe, manifest, toolsetRoot) &&
               VerifyHash(paths.Mkvmerge, manifest, toolsetRoot) &&
               VerifyHash(paths.Mkvpropedit, manifest, toolsetRoot);
    }

    private bool VerifyHash(string path, ToolHashManifest manifest, string toolsetRoot)
    {
        var relative = Path.GetRelativePath(toolsetRoot, path);
        if (!manifest.Files.TryGetValue(relative, out var expected))
        {
            _logger.LogDebug("Missing hash entry for {Path}", relative);
            return false;
        }

        var actual = ComputeSha256(path);
        _logger.LogDebug("Hash for {Path}: {Hash}", relative, actual);
        return string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);
    }

    private void WriteToolHashes(ToolPaths paths, string toolsetRoot)
    {
        var manifest = new ToolHashManifest
        {
            Files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [Path.GetRelativePath(toolsetRoot, paths.Ffmpeg)] = ComputeSha256(paths.Ffmpeg),
                [Path.GetRelativePath(toolsetRoot, paths.Ffprobe)] = ComputeSha256(paths.Ffprobe),
                [Path.GetRelativePath(toolsetRoot, paths.Mkvmerge)] = ComputeSha256(paths.Mkvmerge),
                [Path.GetRelativePath(toolsetRoot, paths.Mkvpropedit)] = ComputeSha256(paths.Mkvpropedit)
            }
        };

        var json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(toolsetRoot, HashManifestName), json);
    }

    private static string ComputeSha256(string path)
    {
        using var sha = SHA256.Create();
        using var stream = File.OpenRead(path);
        var hash = sha.ComputeHash(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private async Task ProvisionToolAsync(
        string toolName,
        ToolArchiveDefinition definition,
        string toolRoot,
        CancellationToken cancellationToken,
        IProgress<ToolProvisionProgress>? progress)
    {
        if (!string.Equals(definition.ArchiveType, "7z", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Unsupported archive type for {toolName}: {definition.ArchiveType}.");
        }

        if (Directory.Exists(toolRoot))
        {
            Directory.Delete(toolRoot, recursive: true);
        }

        Directory.CreateDirectory(toolRoot);

        var tempDirectory = Path.Combine(Path.GetTempPath(), "kitsub-tools", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        var archivePath = Path.Combine(tempDirectory, Path.GetFileName(new Uri(definition.ArchiveUrl).AbsolutePath));

        try
        {
            _logger.LogInformation("Starting download for {Tool} from {Url}", toolName, definition.ArchiveUrl);
            await DownloadFileAsync(toolName, definition.ArchiveUrl, archivePath, progress, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Completed download for {Tool}", toolName);

            var expectedHash = await ResolveExpectedHashAsync(definition, cancellationToken).ConfigureAwait(false);
            var actualHash = ComputeSha256(archivePath);
            _logger.LogDebug("{Tool} archive SHA256: {Hash}", toolName, actualHash);

            if (!string.Equals(expectedHash, actualHash, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"SHA256 verification failed for {toolName} archive.");
            }

            _logger.LogInformation("Starting extraction for {Tool}", toolName);
            using var archive = new ArchiveFile(archivePath);
            ExtractEntries(archive, definition.ExtractMap, toolName, toolRoot, progress);
            _logger.LogInformation("Completed extraction for {Tool}", toolName);
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }
    }

    private void ExtractEntries(
        ArchiveFile archive,
        Dictionary<string, string> extractMap,
        string toolName,
        string toolRoot,
        IProgress<ToolProvisionProgress>? progress)
    {
        var totalFiles = extractMap.Count;
        var filesDone = 0;

        foreach (var (toolKey, archivePath) in extractMap)
        {
            var normalized = NormalizeArchivePath(archivePath);
            var entry = archive.Entries.FirstOrDefault(e =>
                !e.IsFolder && NormalizeArchivePath(e.FileName).EndsWith(normalized, StringComparison.OrdinalIgnoreCase));

            if (entry is null)
            {
                throw new InvalidOperationException($"Missing {toolKey} entry '{archivePath}' in archive.");
            }

            var destination = Path.Combine(toolRoot, archivePath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);

            progress?.Report(new ToolProvisionProgress
            {
                ToolName = toolName,
                ProvisionStage = ToolProvisionProgress.Stage.Extract,
                FilesTotal = totalFiles,
                FilesDone = filesDone,
                CurrentItem = entry.FileName
            });

            entry.Extract(destination);
            filesDone++;

            progress?.Report(new ToolProvisionProgress
            {
                ToolName = toolName,
                ProvisionStage = ToolProvisionProgress.Stage.Extract,
                FilesTotal = totalFiles,
                FilesDone = filesDone,
                CurrentItem = entry.FileName
            });
        }
    }

    private async Task<string> ResolveExpectedHashAsync(ToolArchiveDefinition definition, CancellationToken cancellationToken)
    {
        var content = await HttpClient.GetStringAsync(definition.Sha256Url, cancellationToken).ConfigureAwait(false);

        return ParseSha256Content(content, definition.Sha256Entry);
    }

    private async Task DownloadFileAsync(
        string toolName,
        string url,
        string destination,
        IProgress<ToolProvisionProgress>? progress,
        CancellationToken cancellationToken)
    {
        using var response = await HttpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength;
        var buffer = new byte[64 * 1024];
        long totalRead = 0;
        var stopwatch = Stopwatch.StartNew();
        var lastReport = TimeSpan.Zero;
        var nextReportBytes = 256L * 1024;

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using var file = File.Create(destination);

        while (true)
        {
            var read = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                break;
            }

            await file.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
            totalRead += read;

            if (totalRead >= nextReportBytes || stopwatch.Elapsed - lastReport >= TimeSpan.FromMilliseconds(100))
            {
                ReportDownloadProgress(toolName, totalBytes, totalRead, destination, progress);
                nextReportBytes = totalRead + (256L * 1024);
                lastReport = stopwatch.Elapsed;
            }
        }

        ReportDownloadProgress(toolName, totalBytes, totalRead, destination, progress);
    }

    private static void ReportDownloadProgress(
        string toolName,
        long? totalBytes,
        long currentBytes,
        string destination,
        IProgress<ToolProvisionProgress>? progress)
    {
        progress?.Report(new ToolProvisionProgress
        {
            ToolName = toolName,
            ProvisionStage = ToolProvisionProgress.Stage.Download,
            TotalBytes = totalBytes,
            CurrentBytes = currentBytes,
            CurrentItem = Path.GetFileName(destination)
        });
    }

    private static string NormalizeArchivePath(string value)
    {
        return value.Replace('\\', '/');
    }

    internal static string ParseSha256Content(string content, string? sha256Entry)
    {
        if (string.IsNullOrWhiteSpace(sha256Entry))
        {
            var match = Sha256Regex.Match(content);
            if (!match.Success)
            {
                throw new InvalidOperationException("Unable to parse SHA256 from manifest source.");
            }

            return match.Value.ToLowerInvariant();
        }

        foreach (var line in content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (!line.Contains(sha256Entry, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var match = Sha256Regex.Match(line);
            if (match.Success)
            {
                return match.Value.ToLowerInvariant();
            }
        }

        throw new InvalidOperationException($"SHA256 entry '{sha256Entry}' not found in manifest source.");
    }

    private sealed class ToolHashManifest
    {
        public Dictionary<string, string> Files { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    }
}
