// Summary: Defines resolved configuration file locations.
namespace Kitsub.Cli;

/// <summary>Represents resolved configuration paths.</summary>
public sealed record ConfigPaths(
    string GlobalConfigPath,
    string FfmpegOverridePath,
    string FfprobeOverridePath,
    string MkvmergeOverridePath,
    string MkvpropeditOverridePath
);

/// <summary>Represents the result of loading a configuration file.</summary>
public sealed record ConfigLoadResult(bool Found, string Path, AppConfig? Config);
