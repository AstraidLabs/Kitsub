// Summary: Manages bundled tool discovery and extraction for single-file scenarios.
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;

namespace Kitsub.Tooling.Bundling;

/// <summary>Represents the source location of a tool bundle.</summary>
public enum ToolBundleLocation
{
    Bundled,
    Extracted
}

/// <summary>Represents a set of resolved tool paths for a tool bundle.</summary>
/// <param name="BaseDirectory">The root directory containing tool executables.</param>
/// <param name="Location">The source location of the toolset.</param>
/// <param name="Paths">The tool paths rooted at <paramref name="BaseDirectory"/>.</param>
public sealed record ToolBundleResult(string BaseDirectory, ToolBundleLocation Location, ToolPaths Paths);

/// <summary>Coordinates locating and extracting bundled toolsets.</summary>
public sealed class ToolBundleManager
{
    private const int UnixExecutableMode = 0x1ED; // 0755
    private readonly ToolManifestLoader _manifestLoader;
    private readonly ToolResolverOptions _options;
    private readonly ILogger<ToolBundleManager> _logger;
    private ToolManifest? _manifest;

    /// <summary>Initializes a new instance with manifest loader and options.</summary>
    public ToolBundleManager(ToolManifestLoader manifestLoader, ToolResolverOptions options, ILogger<ToolBundleManager> logger)
    {
        _manifestLoader = manifestLoader;
        _options = options;
        _logger = logger;
    }

    /// <summary>Gets the tools manifest.</summary>
    public ToolManifest Manifest => _manifest ??= _manifestLoader.Load();

    /// <summary>Gets the runtime identifier for the current process.</summary>
    public string GetRuntimeRid()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return RuntimeInformation.OSArchitecture == Architecture.Arm64 ? "win-arm64" : "win-x64";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return RuntimeInformation.OSArchitecture == Architecture.Arm64 ? "osx-arm64" : "osx-x64";
        }

        return RuntimeInformation.OSArchitecture == Architecture.Arm64 ? "linux-arm64" : "linux-x64";
    }

    /// <summary>Attempts to resolve tool paths from the portable bundled folder.</summary>
    /// <param name="rid">The runtime identifier to resolve.</param>
    /// <returns>The tool bundle result if found; otherwise, null.</returns>
    public ToolBundleResult? TryGetBundledToolset(string rid)
    {
        var manifest = Manifest;
        if (!manifest.Rids.TryGetValue(rid, out var ridEntry))
        {
            _logger.LogDebug("Manifest does not include RID {Rid}", rid);
            return null;
        }

        var baseDirectory = Path.Combine(AppContext.BaseDirectory, "tools", rid);
        var paths = BuildToolPaths(baseDirectory, ridEntry);
        if (paths is null)
        {
            return null;
        }

        if (!AreAllToolsPresent(paths))
        {
            _logger.LogDebug("Bundled tools not present for RID {Rid}", rid);
            return null;
        }

        _logger.LogInformation("Using bundled tools from {BaseDirectory}", baseDirectory);
        return new ToolBundleResult(baseDirectory, ToolBundleLocation.Bundled, paths);
    }

    /// <summary>Attempts to resolve tool paths from the extracted cache, extracting if needed.</summary>
    /// <param name="rid">The runtime identifier to resolve.</param>
    /// <returns>The tool bundle result if available; otherwise, null.</returns>
    public ToolBundleResult? TryGetExtractedToolset(string rid)
    {
        var manifest = Manifest;
        if (!manifest.Rids.TryGetValue(rid, out var ridEntry))
        {
            _logger.LogDebug("Manifest does not include RID {Rid}", rid);
            return null;
        }

        var cacheRoot = GetCacheRoot();
        var versionDirectory = Path.Combine(cacheRoot, rid, manifest.ToolsetVersion);
        var paths = BuildToolPaths(versionDirectory, ridEntry);
        if (paths is null)
        {
            return null;
        }

        var needsExtraction = !AreAllToolsPresent(paths) || !VerifyToolHashes(paths, ridEntry);
        if (!needsExtraction)
        {
            _logger.LogInformation("Using cached tools from {BaseDirectory}", versionDirectory);
            return new ToolBundleResult(versionDirectory, ToolBundleLocation.Extracted, paths);
        }

        using var lockHandle = AcquireExtractionLock(Path.Combine(cacheRoot, rid));
        if (lockHandle is null)
        {
            _logger.LogWarning("Failed to acquire extraction lock; using PATH fallback.");
            return null;
        }

        if (!AreAllToolsPresent(paths) || !VerifyToolHashes(paths, ridEntry))
        {
            var archiveStream = _manifestLoader.TryOpenArchiveStream(rid);
            if (archiveStream is null)
            {
                _logger.LogDebug("No embedded archive available to extract tools for RID {Rid}", rid);
                return null;
            }

            _logger.LogInformation("Extracting tools for RID {Rid} to {Directory}", rid, versionDirectory);
            if (Directory.Exists(versionDirectory))
            {
                Directory.Delete(versionDirectory, recursive: true);
            }

            Directory.CreateDirectory(versionDirectory);
            ExtractArchive(archiveStream, versionDirectory);
            EnsureExecutables(paths);

            if (!VerifyToolHashes(paths, ridEntry))
            {
                throw new InvalidOperationException("Extracted tools failed hash verification.");
            }
        }

        return new ToolBundleResult(versionDirectory, ToolBundleLocation.Extracted, paths);
    }

    /// <summary>Deletes the extracted tool cache directory.</summary>
    public void CleanCache()
    {
        var cacheRoot = GetCacheRoot();
        if (!Directory.Exists(cacheRoot))
        {
            _logger.LogInformation("Tools cache directory does not exist: {CacheRoot}", cacheRoot);
            return;
        }

        Directory.Delete(cacheRoot, recursive: true);
        _logger.LogInformation("Deleted tools cache directory {CacheRoot}", cacheRoot);
    }

    private static ToolPaths? BuildToolPaths(string baseDirectory, ToolManifestRid ridEntry)
    {
        if (ridEntry.Ffmpeg is null || ridEntry.Ffprobe is null || ridEntry.Mkvmerge is null || ridEntry.Mkvpropedit is null)
        {
            return null;
        }

        return new ToolPaths(
            ResolvePath(baseDirectory, ridEntry.Ffmpeg.Path),
            ResolvePath(baseDirectory, ridEntry.Ffprobe.Path),
            ResolvePath(baseDirectory, ridEntry.Mkvmerge.Path),
            ResolvePath(baseDirectory, ridEntry.Mkvpropedit.Path));
    }

    private static string ResolvePath(string baseDirectory, string relativePath)
    {
        var normalized = relativePath.Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(baseDirectory, normalized);
    }

    private bool AreAllToolsPresent(ToolPaths paths)
    {
        return File.Exists(paths.Ffmpeg!) &&
               File.Exists(paths.Ffprobe!) &&
               File.Exists(paths.Mkvmerge!) &&
               File.Exists(paths.Mkvpropedit!);
    }

    private bool VerifyToolHashes(ToolPaths paths, ToolManifestRid ridEntry)
    {
        var checks = new (string Path, string? Sha, string Name)[]
        {
            (paths.Ffmpeg!, ridEntry.Ffmpeg?.Sha256, "ffmpeg"),
            (paths.Ffprobe!, ridEntry.Ffprobe?.Sha256, "ffprobe"),
            (paths.Mkvmerge!, ridEntry.Mkvmerge?.Sha256, "mkvmerge"),
            (paths.Mkvpropedit!, ridEntry.Mkvpropedit?.Sha256, "mkvpropedit")
        };

        foreach (var check in checks)
        {
            if (string.IsNullOrWhiteSpace(check.Sha))
            {
                _logger.LogDebug("Skipping hash verification for {Tool}", check.Name);
                continue;
            }

            var hash = ComputeSha256(check.Path);
            var matches = hash.Equals(check.Sha, StringComparison.OrdinalIgnoreCase);
            _logger.LogDebug("Hash verification for {Tool}: {Matches}", check.Name, matches);
            if (!matches)
            {
                return false;
            }
        }

        return true;
    }

    private static string ComputeSha256(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static void ExtractArchive(Stream archiveStream, string destinationDirectory)
    {
        using var archive = new ZipArchive(archiveStream, ZipArchiveMode.Read, leaveOpen: false);
        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrEmpty(entry.FullName))
            {
                continue;
            }

            var sanitizedPath = entry.FullName.Replace('/', Path.DirectorySeparatorChar);
            var fullPath = Path.GetFullPath(Path.Combine(destinationDirectory, sanitizedPath));
            if (!fullPath.StartsWith(Path.GetFullPath(destinationDirectory), StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Archive entry path traversal detected.");
            }

            if (entry.FullName.EndsWith("/", StringComparison.Ordinal) || entry.FullName.EndsWith("\\", StringComparison.Ordinal))
            {
                Directory.CreateDirectory(fullPath);
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            entry.ExtractToFile(fullPath, overwrite: true);
        }
    }

    private void EnsureExecutables(ToolPaths paths)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        SetExecutable(paths.Ffmpeg!);
        SetExecutable(paths.Ffprobe!);
        SetExecutable(paths.Mkvmerge!);
        SetExecutable(paths.Mkvpropedit!);
    }

    private static void SetExecutable(string path)
    {
        chmod(path, UnixExecutableMode);
    }

    private static FileStream? AcquireExtractionLock(string baseDirectory)
    {
        Directory.CreateDirectory(baseDirectory);
        var lockPath = Path.Combine(baseDirectory, ".extract.lock");
        try
        {
            return new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }
        catch (IOException)
        {
            return null;
        }
    }

    private string GetCacheRoot()
    {
        if (!string.IsNullOrWhiteSpace(_options.ToolsCacheDirectory))
        {
            return _options.ToolsCacheDirectory!;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(baseDir, "Kitsub", "tools");
        }

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, ".kitsub", "tools");
    }

    [DllImport("libc", SetLastError = true)]
    private static extern int chmod(string pathname, int mode);
}
