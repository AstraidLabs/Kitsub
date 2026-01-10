using Spectre.Console.Cli;

namespace Kitsub.Cli;

public abstract class ToolSettings : CommandSettings
{
    [CommandOption("--ffmpeg <PATH>")]
    public string FfmpegPath { get; init; } = "ffmpeg";

    [CommandOption("--ffprobe <PATH>")]
    public string FfprobePath { get; init; } = "ffprobe";

    [CommandOption("--mkvmerge <PATH>")]
    public string MkvmergePath { get; init; } = "mkvmerge";

    [CommandOption("--mkvpropedit <PATH>")]
    public string MkvpropeditPath { get; init; } = "mkvpropedit";

    [CommandOption("--dry-run")]
    public bool DryRun { get; init; }

    [CommandOption("--verbose")]
    public bool Verbose { get; init; }

    [CommandOption("--log-file <PATH>")]
    public string LogFile { get; init; } = "logs/kitsub.log";

    [CommandOption("--log-level <trace|debug|info|warn|error>")]
    public string LogLevel { get; init; } = "info";

    [CommandOption("--no-log")]
    public bool NoLog { get; init; }
}
