// Summary: Resolves external tool executable paths using overrides, bundling, and PATH.
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace Kitsub.Tooling.Bundling;

/// <summary>Resolves tool paths from overrides, bundled tools, and PATH.</summary>
public sealed class ToolResolver
{
    private readonly ToolBundleManager _bundleManager;
    private readonly ToolResolverOptions _options;
    private readonly ILogger<ToolResolver> _logger;

    /// <summary>Initializes a new instance with required dependencies.</summary>
    public ToolResolver(ToolBundleManager bundleManager, ToolResolverOptions options, ILogger<ToolResolver> logger)
    {
        _bundleManager = bundleManager;
        _options = options;
        _logger = logger;
    }

    /// <summary>Resolves all tool paths using the configured preferences.</summary>
    /// <param name="overrides">User-provided override paths.</param>
    /// <returns>The resolved tool paths with metadata.</returns>
    public ToolPathsResolved ResolveAll(ToolPaths overrides)
    {
        var manifest = _bundleManager.Manifest;
        var rid = _bundleManager.GetRuntimeRid();
        var toolsetVersion = manifest.ToolsetVersion;

        ToolBundleResult? bundled = null;
        ToolBundleResult? extracted = null;

        if (_options.PreferBundled)
        {
            bundled = _bundleManager.TryGetBundledToolset(rid);
            if (bundled is null)
            {
                extracted = _bundleManager.TryGetExtractedToolset(rid);
            }
        }

        var ffmpeg = ResolveTool("ffmpeg", overrides.Ffmpeg, bundled, extracted, toolsetVersion);
        var ffprobe = ResolveTool("ffprobe", overrides.Ffprobe, bundled, extracted, toolsetVersion);
        var mkvmerge = ResolveTool("mkvmerge", overrides.Mkvmerge, bundled, extracted, toolsetVersion);
        var mkvpropedit = ResolveTool("mkvpropedit", overrides.Mkvpropedit, bundled, extracted, toolsetVersion);

        LogResolvedTool("ffmpeg", ffmpeg);
        LogResolvedTool("ffprobe", ffprobe);
        LogResolvedTool("mkvmerge", mkvmerge);
        LogResolvedTool("mkvpropedit", mkvpropedit);

        return new ToolPathsResolved(rid, toolsetVersion, ffmpeg, ffprobe, mkvmerge, mkvpropedit);
    }

    private ToolPathResolution ResolveTool(
        string toolName,
        string? overridePath,
        ToolBundleResult? bundled,
        ToolBundleResult? extracted,
        string toolsetVersion)
    {
        if (!string.IsNullOrWhiteSpace(overridePath))
        {
            return new ToolPathResolution(overridePath, ToolPathSource.Override);
        }

        var preferPath = _options.PreferPath;
        var preferBundled = _options.PreferBundled;

        if (preferPath)
        {
            var pathResolution = ResolveFromPath(toolName);
            if (pathResolution is not null)
            {
                return pathResolution;
            }
        }

        if (preferBundled)
        {
            if (bundled is not null)
            {
                return new ToolPathResolution(GetBundledPath(toolName, bundled.Paths), ToolPathSource.Bundled);
            }

            if (extracted is not null)
            {
                return new ToolPathResolution(GetBundledPath(toolName, extracted.Paths), ToolPathSource.Extracted);
            }
        }

        var fallbackPath = ResolveFromPath(toolName) ?? new ToolPathResolution(toolName, ToolPathSource.Path);
        if (fallbackPath.Path == toolName)
        {
            _logger.LogWarning("Falling back to PATH for {Tool} (toolset {ToolsetVersion})", toolName, toolsetVersion);
        }

        return fallbackPath;
    }

    private static string GetBundledPath(string toolName, ToolPaths paths)
    {
        return toolName switch
        {
            "ffmpeg" => paths.Ffmpeg!,
            "ffprobe" => paths.Ffprobe!,
            "mkvmerge" => paths.Mkvmerge!,
            "mkvpropedit" => paths.Mkvpropedit!,
            _ => throw new InvalidOperationException($"Unknown tool: {toolName}")
        };
    }

    private ToolPathResolution? ResolveFromPath(string toolName)
    {
        var path = FindOnPath(toolName);
        return path is null ? null : new ToolPathResolution(path, ToolPathSource.Path);
    }

    private void LogResolvedTool(string toolName, ToolPathResolution resolution)
    {
        _logger.LogInformation("Resolved {Tool} => {Path} ({Source})", toolName, resolution.Path, resolution.Source);
    }

    private static string? FindOnPath(string toolName)
    {
        var pathVariable = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(pathVariable))
        {
            return null;
        }

        var extensions = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? (Environment.GetEnvironmentVariable("PATHEXT")?.Split(';', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>())
            : Array.Empty<string>();

        foreach (var entry in pathVariable.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            var candidate = Path.Combine(entry, toolName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            foreach (var ext in extensions)
            {
                var extended = candidate + ext.ToLowerInvariant();
                if (File.Exists(extended))
                {
                    return extended;
                }
            }
        }

        return null;
    }
}
