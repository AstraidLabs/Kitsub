// Summary: Resolves tool executable paths using overrides, bundled tools, cache provisioning, and PATH.
using Kitsub.Tooling;
using Microsoft.Extensions.Logging;

namespace Kitsub.Tooling.Provisioning;

/// <summary>Resolves external tool paths with Windows provisioning support.</summary>
public sealed class ToolResolver
{
    private readonly ToolBundleManager _bundleManager;
    private readonly WindowsRidDetector _ridDetector;
    private readonly ILogger<ToolResolver> _logger;

    public ToolResolver(ToolBundleManager bundleManager, WindowsRidDetector ridDetector, ILogger<ToolResolver> logger)
    {
        _bundleManager = bundleManager;
        _ridDetector = ridDetector;
        _logger = logger;
    }

    /// <summary>Resolves tool paths according to overrides, bundled tools, cache provisioning, and PATH.</summary>
    public ToolResolution Resolve(
        ToolOverrides overrides,
        ToolResolveOptions options,
        IProgress<ToolProvisionProgress>? progress = null)
    {
        var rid = _ridDetector.GetRuntimeRid();
        var manifest = _bundleManager.Manifest;
        var toolsetVersion = manifest.ToolsetVersion;

        ToolBundleResult? bundled = null;
        ToolBundleResult? cached = null;

        if (_ridDetector.IsWindows && manifest.Rids.ContainsKey(rid))
        {
            if (options.PreferBundled)
            {
                bundled = _bundleManager.TryGetBundledToolset(rid);
            }

            if (bundled is null && !options.PreferPath)
            {
                cached = _bundleManager.EnsureCachedToolsetAsync(rid, options, CancellationToken.None, force: false, progress)
                    .GetAwaiter().GetResult();
            }
        }
        else
        {
            _logger.LogWarning("Tool provisioning unavailable for RID {Rid}; falling back to PATH.", rid);
        }

        var ffmpeg = ResolveTool("ffmpeg", overrides.Ffmpeg, bundled, cached, options);
        var ffprobe = ResolveTool("ffprobe", overrides.Ffprobe, bundled, cached, options);
        var mkvmerge = ResolveTool("mkvmerge", overrides.Mkvmerge, bundled, cached, options);
        var mkvpropedit = ResolveTool("mkvpropedit", overrides.Mkvpropedit, bundled, cached, options);

        LogResolved("ffmpeg", ffmpeg);
        LogResolved("ffprobe", ffprobe);
        LogResolved("mkvmerge", mkvmerge);
        LogResolved("mkvpropedit", mkvpropedit);

        return new ToolResolution(rid, toolsetVersion, ffmpeg, ffprobe, mkvmerge, mkvpropedit);
    }

    private ToolPathResolution ResolveTool(
        string toolName,
        string? overridePath,
        ToolBundleResult? bundled,
        ToolBundleResult? cached,
        ToolResolveOptions options)
    {
        if (!string.IsNullOrWhiteSpace(overridePath))
        {
            var resolved = Path.GetFullPath(overridePath);
            if (File.Exists(resolved))
            {
                return new ToolPathResolution(resolved, ToolSource.Override);
            }

            _logger.LogWarning("Override path for {Tool} does not exist: {Path}", toolName, overridePath);
        }

        if (options.PreferBundled && bundled is not null)
        {
            return new ToolPathResolution(GetPath(toolName, bundled.Paths), ToolSource.Bundled);
        }

        if (!options.PreferPath && cached is not null)
        {
            return new ToolPathResolution(GetPath(toolName, cached.Paths), ToolSource.Cache);
        }

        _logger.LogWarning("Falling back to PATH for {Tool}", toolName);
        return new ToolPathResolution(toolName, ToolSource.Path);
    }

    private static string GetPath(string toolName, ToolPaths paths)
    {
        return toolName switch
        {
            "ffmpeg" => paths.Ffmpeg,
            "ffprobe" => paths.Ffprobe,
            "mkvmerge" => paths.Mkvmerge,
            "mkvpropedit" => paths.Mkvpropedit,
            _ => throw new InvalidOperationException($"Unknown tool: {toolName}")
        };
    }

    private void LogResolved(string toolName, ToolPathResolution resolution)
    {
        _logger.LogInformation("Resolved {Tool} => {Path} ({Source})", toolName, resolution.Path, resolution.Source);
    }

    // PATH fallback intentionally returns the tool name to defer resolution to the host environment.
}
