// Summary: Implements the CLI command that muxes subtitle files into MKV containers.
using Kitsub.Core;
using Kitsub.Tooling.Provisioning;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Kitsub.Cli;

/// <summary>Executes subtitle muxing into MKV files.</summary>
public sealed class MuxCommand : CommandBase<MuxCommand.Settings>
{
    private readonly ToolResolver _toolResolver;

    /// <summary>Defines command-line settings for muxing subtitles.</summary>
    public sealed class Settings : ToolSettings
    {
        [CommandOption("--in <MKV>")]
        /// <summary>Gets the input MKV file path.</summary>
        public string InputMkv { get; init; } = string.Empty;

        [CommandOption("--sub <FILE>")]
        /// <summary>Gets the subtitle files to mux.</summary>
        public string[] Subtitles { get; init; } = Array.Empty<string>();

        [CommandOption("--lang <ISO>")]
        /// <summary>Gets the optional language tag for subtitle tracks.</summary>
        public string? Language { get; init; }

        [CommandOption("--title <NAME>")]
        /// <summary>Gets the optional title for subtitle tracks.</summary>
        public string? Title { get; init; }

        [CommandOption("--default")]
        /// <summary>Gets a value indicating whether tracks should be marked as default.</summary>
        public bool Default { get; init; }

        [CommandOption("--no-default")]
        /// <summary>Gets a value indicating whether tracks should not be marked as default.</summary>
        public bool NoDefault { get; init; }

        [CommandOption("--forced")]
        /// <summary>Gets a value indicating whether tracks should be marked as forced.</summary>
        public bool Forced { get; init; }

        [CommandOption("--no-forced")]
        /// <summary>Gets a value indicating whether tracks should not be marked as forced.</summary>
        public bool NoForced { get; init; }

        [CommandOption("--out <FILE>")]
        /// <summary>Gets the optional output MKV file path.</summary>
        public string? Output { get; init; }

        /// <summary>Validates the provided settings for muxing subtitles.</summary>
        /// <returns>A validation result indicating success or failure.</returns>
        public override ValidationResult Validate()
        {
            // Block: Validate the required input MKV file before muxing.
            var inputValidation = ValidationHelpers.ValidateFileExists(InputMkv, "Input MKV");
            if (!inputValidation.Successful)
            {
                return inputValidation;
            }

            if (Subtitles.Length == 0)
            {
                // Block: Require at least one subtitle file to mux.
                return ValidationResult.Error("At least one subtitle file is required.");
            }

            foreach (var subtitle in Subtitles)
            {
                // Block: Validate each subtitle file provided on the command line.
                var subValidation = ValidationHelpers.ValidateFileExists(subtitle, "Subtitle file");
                if (!subValidation.Successful)
                {
                    return subValidation;
                }
            }

            if (Default && NoDefault)
            {
                // Block: Prevent conflicting default flag configuration.
                return ValidationResult.Error("Use either --default or --no-default, not both.");
            }

            if (Forced && NoForced)
            {
                // Block: Prevent conflicting forced flag configuration.
                return ValidationResult.Error("Use either --forced or --no-forced, not both.");
            }

            return ValidationResult.Success();
        }
    }

    /// <summary>Initializes the command with the console used for output.</summary>
    /// <param name="console">The console used to render command output.</param>
    public MuxCommand(IAnsiConsole console, ToolResolver toolResolver) : base(console)
    {
        // Block: Delegate console handling to the base command class.
        _toolResolver = toolResolver;
    }

    protected override async Task<int> ExecuteAsyncCore(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        // Block: Determine the output file path when one is not explicitly provided.
        var output = settings.Output;
        if (string.IsNullOrWhiteSpace(output))
        {
            // Block: Construct a default output path alongside the input MKV.
            var name = Path.GetFileNameWithoutExtension(settings.InputMkv);
            output = Path.Combine(Path.GetDirectoryName(settings.InputMkv) ?? string.Empty, $"{name}.kitsub.mkv");
        }

        // Block: Resolve default and forced flags based on mutually exclusive options.
        var defaultFlag = settings.Default ? true : settings.NoDefault ? false : (bool?)null;
        var forcedFlag = settings.Forced ? true : settings.NoForced ? false : (bool?)null;

        // Block: Build subtitle descriptors with optional metadata for muxing.
        var subtitles = settings.Subtitles
            .Select(sub => new SubtitleDescriptor(sub, settings.Language, settings.Title, defaultFlag, forcedFlag))
            .ToList();

        // Block: Execute the muxing operation and report completion.
        using var tooling = ToolingFactory.CreateTooling(settings, Console, _toolResolver);
        await tooling.Service.MuxSubtitlesAsync(settings.InputMkv, subtitles, output, cancellationToken)
            .ConfigureAwait(false);
        Console.MarkupLine($"[green]Muxed subtitles into[/] {Markup.Escape(output)}");
        return 0;
    }
}
