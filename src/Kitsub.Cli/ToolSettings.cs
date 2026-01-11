// Summary: Defines shared CLI options for locating external tools and configuring logging behavior.
using Spectre.Console.Cli;

namespace Kitsub.Cli;

/// <summary>Represents common command-line settings for tool discovery and logging.</summary>
public abstract class ToolSettings : CommandSettings
{
    [CommandOption("--ffmpeg <PATH>")]
    /// <summary>Gets the path or command name used to invoke ffmpeg.</summary>
    public string? FfmpegPath { get; init; }

    [CommandOption("--ffprobe <PATH>")]
    /// <summary>Gets the path or command name used to invoke ffprobe.</summary>
    public string? FfprobePath { get; init; }

    [CommandOption("--mkvmerge <PATH>")]
    /// <summary>Gets the path or command name used to invoke mkvmerge.</summary>
    public string? MkvmergePath { get; init; }

    [CommandOption("--mkvpropedit <PATH>")]
    /// <summary>Gets the path or command name used to invoke mkvpropedit.</summary>
    public string? MkvpropeditPath { get; init; }

    [CommandOption("--prefer-bundled <BOOL>")]
    /// <summary>Gets a value indicating whether bundled tools are preferred.</summary>
    public bool PreferBundled { get; init; } = true;

    [CommandOption("--prefer-path <BOOL>")]
    /// <summary>Gets a value indicating whether PATH resolution is preferred.</summary>
    public bool PreferPath { get; init; }

    [CommandOption("--tools-cache-dir <PATH>")]
    /// <summary>Gets an optional override for the tools cache directory.</summary>
    public string? ToolsCacheDir { get; init; }

    [CommandOption("--dry-run")]
    /// <summary>Gets a value indicating whether commands should run without making changes.</summary>
    public bool DryRun { get; init; }

    [CommandOption("--verbose")]
    /// <summary>Gets a value indicating whether verbose output is enabled.</summary>
    public bool Verbose { get; init; }

    [CommandOption("--log-file <PATH>")]
    /// <summary>Gets the log file path used when logging is enabled.</summary>
    public string LogFile { get; init; } = "logs/kitsub.log";

    [CommandOption("--log-level <trace|debug|info|warn|error>")]
    /// <summary>Gets the minimum log level used for logging output.</summary>
    public string LogLevel { get; init; } = "info";

    [CommandOption("--no-log")]
    /// <summary>Gets a value indicating whether logging is disabled.</summary>
    public bool NoLog { get; init; }

    [CommandOption("--no-banner")]
    /// <summary>Gets a value indicating whether the startup banner is suppressed.</summary>
    public bool NoBanner { get; init; }
}
