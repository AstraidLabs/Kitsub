// Summary: Exposes configuration loading routines through dependency injection.
using Kitsub.Core;

namespace Kitsub.Cli;

/// <summary>Provides access to Kitsub configuration files.</summary>
public sealed class AppConfigService
{
    private readonly AppConfigLoader _loader = new();

    public ConfigPaths GetPaths() => _loader.GetConfigPaths();

    public ConfigLoadResult LoadGlobalConfig() => _loader.LoadGlobalConfig();

    public AppConfig LoadEffectiveConfig() => _loader.LoadEffectiveConfig();

    public ConfigStatus GetGlobalStatus()
    {
        var paths = GetPaths();
        if (!File.Exists(paths.GlobalConfigPath))
        {
            return new ConfigStatus(paths.GlobalConfigPath, found: false, valid: true, error: null);
        }

        try
        {
            _ = _loader.LoadGlobalConfig();
            return new ConfigStatus(paths.GlobalConfigPath, found: true, valid: true, error: null);
        }
        catch (ConfigurationException ex)
        {
            return new ConfigStatus(paths.GlobalConfigPath, found: true, valid: false, error: ex.Message);
        }
    }
}

/// <summary>Represents the status of the global configuration file.</summary>
public sealed record ConfigStatus(string Path, bool Found, bool Valid, string? Error);
