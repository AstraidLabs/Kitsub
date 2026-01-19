// Summary: Defines global CLI options shared across commands.
using Spectre.Console.Cli;

namespace Kitsub.Cli;

/// <summary>Represents global command-line settings.</summary>
public abstract class GlobalSettings : CommandSettings
{
    [CommandOption("--yes")]
    /// <summary>Gets a value indicating whether prompts should be auto-accepted.</summary>
    public bool AssumeYes { get; set; }

    [CommandOption("--no-provision")]
    /// <summary>Gets a value indicating whether tool provisioning is disabled.</summary>
    public bool NoProvision { get; set; }

    [CommandOption("--no-startup-prompt")]
    /// <summary>Gets a value indicating whether startup tool prompts are disabled.</summary>
    public bool NoStartupPrompt { get; set; }

    [CommandOption("--check-updates")]
    /// <summary>Gets a value indicating whether update checks should be forced at startup.</summary>
    public bool CheckUpdates { get; set; }
}
