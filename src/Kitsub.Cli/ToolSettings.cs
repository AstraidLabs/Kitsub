// Summary: Defines shared CLI options for locating external tools and configuring logging behavior.
using Spectre.Console.Cli;

namespace Kitsub.Cli;

/// <summary>Represents common command-line settings for tool discovery and logging.</summary>
public abstract class ToolSettings : CommandSettings
{
    [CommandOption("--ffmpeg <PATH>")]
    /// <summary>Gets the path or command name used to invoke ffmpeg.</summary>
    public string? FfmpegPath { get; set; }

    [CommandOption("--ffprobe <PATH>")]
    /// <summary>Gets the path or command name used to invoke ffprobe.</summary>
    public string? FfprobePath { get; set; }

    [CommandOption("--mkvmerge <PATH>")]
    /// <summary>Gets the path or command name used to invoke mkvmerge.</summary>
    public string? MkvmergePath { get; set; }

    [CommandOption("--mkvpropedit <PATH>")]
    /// <summary>Gets the path or command name used to invoke mkvpropedit.</summary>
    public string? MkvpropeditPath { get; set; }

    [CommandOption("--mediainfo <PATH>")]
    /// <summary>Gets the path or command name used to invoke mediainfo.</summary>
    public string? MediainfoPath { get; set; }

    [CommandOption("--prefer-bundled <BOOL>")]
    /// <summary>Gets a value indicating whether bundled tools are preferred.</summary>
    public bool? PreferBundled { get; set; }

    [CommandOption("--prefer-path <BOOL>")]
    /// <summary>Gets a value indicating whether PATH resolution is preferred.</summary>
    public bool? PreferPath { get; set; }

    [CommandOption("--tools-cache-dir <PATH>")]
    /// <summary>Gets an optional override for the tools cache directory.</summary>
    public string? ToolsCacheDir { get; set; }

    [CommandOption("--dry-run")]
    /// <summary>Gets a value indicating whether commands should run without making changes.</summary>
    public bool DryRun { get; set; }

    [CommandOption("--verbose")]
    /// <summary>Gets a value indicating whether verbose output is enabled.</summary>
    public bool Verbose { get; set; }

    [CommandOption("--log-file <PATH>")]
    /// <summary>Gets the log file path used when logging is enabled.</summary>
    public string? LogFile { get; set; }

    [CommandOption("--log-level <trace|debug|info|warn|error>")]
    /// <summary>Gets the minimum log level used for logging output.</summary>
    public string? LogLevel { get; set; }

    [CommandOption("--no-log")]
    /// <summary>Gets a value indicating whether logging is disabled.</summary>
    public bool? NoLog { get; set; }

    [CommandOption("--no-banner")]
    /// <summary>Gets a value indicating whether the startup banner is suppressed.</summary>
    public bool? NoBanner { get; set; }

    [CommandOption("--no-color")]
    /// <summary>Gets a value indicating whether ANSI color output is disabled.</summary>
    public bool? NoColor { get; set; }

    [CommandOption("--progress <auto|on|off>")]
    /// <summary>Gets the progress rendering mode for tool provisioning.</summary>
    public UiProgressMode? Progress { get; set; }
}
