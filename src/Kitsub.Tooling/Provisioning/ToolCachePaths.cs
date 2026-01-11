// Summary: Defines cache layout for provisioned tool binaries.
using Microsoft.Extensions.Logging;

namespace Kitsub.Tooling.Provisioning;

/// <summary>Builds cache paths for downloaded tool archives and extracted binaries.</summary>
public sealed class ToolCachePaths
{
    private readonly ILogger<ToolCachePaths> _logger;

    public ToolCachePaths(ILogger<ToolCachePaths> logger)
    {
        _logger = logger;
    }

    /// <summary>Gets the root cache directory, applying overrides when provided.</summary>
    public string GetCacheRoot(string? overrideDirectory)
    {
        if (!string.IsNullOrWhiteSpace(overrideDirectory))
        {
            return Path.GetFullPath(overrideDirectory);
        }

        var envOverride = Environment.GetEnvironmentVariable("KITSUB_TOOLS_CACHE_DIR");
        if (!string.IsNullOrWhiteSpace(envOverride))
        {
            return Path.GetFullPath(envOverride);
        }

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var root = Path.Combine(localAppData, "Kitsub", "tools");
        _logger.LogDebug("Using default tools cache root {CacheRoot}", root);
        return root;
    }

    /// <summary>Gets the cache directory for a specific RID and toolset version.</summary>
    public string GetToolsetRoot(string rid, string toolsetVersion, string? overrideDirectory)
    {
        return Path.Combine(GetCacheRoot(overrideDirectory), rid, toolsetVersion);
    }
}
