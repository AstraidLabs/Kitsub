// Summary: Defines CLI settings for the release mux workflow.
using Spectre.Console;
using Spectre.Console.Cli;

namespace Kitsub.Cli;

/// <summary>Defines command-line settings for the release mux workflow.</summary>
public sealed class ReleaseMuxSettings : ToolSettings
{
    [CommandOption("--in <FILE>")]
    /// <summary>Gets the input MKV file path.</summary>
    public string? InputMkv { get; set; }

    [CommandOption("--sub <FILE>")]
    /// <summary>Gets the subtitle file path (single-sub mode).</summary>
    public string? SubtitlePath { get; set; }

    [CommandOption("--lang <ISO>")]
    /// <summary>Gets the optional subtitle language tag.</summary>
    public string? Language { get; set; }

    [CommandOption("--title <TEXT>")]
    /// <summary>Gets the optional subtitle title.</summary>
    public string? Title { get; set; }

    [CommandOption("--default")]
    /// <summary>Gets a value indicating whether the subtitle should be default.</summary>
    public bool? Default { get; set; }

    [CommandOption("--no-default")]
    /// <summary>Gets a value indicating whether the subtitle should not be default.</summary>
    public bool? NoDefault { get; set; }

    [CommandOption("--forced")]
    /// <summary>Gets a value indicating whether the subtitle should be forced.</summary>
    public bool? Forced { get; set; }

    [CommandOption("--no-forced")]
    /// <summary>Gets a value indicating whether the subtitle should not be forced.</summary>
    public bool? NoForced { get; set; }

    [CommandOption("--fonts <DIR>")]
    /// <summary>Gets the optional fonts directory.</summary>
    public string? FontsDir { get; set; }

    [CommandOption("--out <FILE>")]
    /// <summary>Gets the optional output MKV file path.</summary>
    public string? Output { get; set; }

    [CommandOption("--out-dir <DIR>")]
    /// <summary>Gets the optional output directory for the release file.</summary>
    public string? OutputDir { get; set; }

    [CommandOption("--force")]
    /// <summary>Gets a value indicating whether existing output files should be overwritten.</summary>
    public bool Force { get; set; }

    [CommandOption("--strict")]
    /// <summary>Gets a value indicating whether warnings should be treated as errors.</summary>
    public bool? Strict { get; set; }

    [CommandOption("--spec <PATH>")]
    /// <summary>Gets the optional release spec JSON file path.</summary>
    public string? SpecPath { get; set; }

    /// <summary>Validates the provided settings for the release mux workflow.</summary>
    /// <returns>A validation result indicating success or failure.</returns>
    public override ValidationResult Validate()
    {
        if (!string.IsNullOrWhiteSpace(Output) && !string.IsNullOrWhiteSpace(OutputDir))
        {
            return ValidationResult.Error("Use either --out or --out-dir, not both.");
        }

        if (Default == true && NoDefault == true)
        {
            return ValidationResult.Error("Use either --default or --no-default, not both.");
        }

        if (Forced == true && NoForced == true)
        {
            return ValidationResult.Error("Use either --forced or --no-forced, not both.");
        }

        if (!string.IsNullOrWhiteSpace(SpecPath))
        {
            return File.Exists(SpecPath)
                ? ValidationResult.Success()
                : ValidationResult.Error($"Spec file not found: {SpecPath}");
        }

        if (string.IsNullOrWhiteSpace(InputMkv))
        {
            return ValidationResult.Error("Input MKV is required. Use --spec for multi-sub mode.");
        }

        if (string.IsNullOrWhiteSpace(SubtitlePath))
        {
            return ValidationResult.Error("Subtitle file is required. Use --spec for multi-sub mode.");
        }

        return ValidationResult.Success();
    }
}
