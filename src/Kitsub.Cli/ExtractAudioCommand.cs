// Summary: Implements the CLI command that extracts audio tracks from media files.
using Kitsub.Tooling;
using Kitsub.Tooling.Provisioning;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Kitsub.Cli;

/// <summary>Executes audio extraction using external tooling.</summary>
public sealed class ExtractAudioCommand : CommandBase<ExtractAudioCommand.Settings>
{
    private readonly ToolResolver _toolResolver;

    /// <summary>Defines command-line settings for audio extraction.</summary>
    public sealed class Settings : ToolSettings
    {
        [CommandOption("--in <FILE>")]
        /// <summary>Gets the input media file path.</summary>
        public string InputFile { get; init; } = string.Empty;

        [CommandOption("--track <SELECTOR>")]
        /// <summary>Gets the audio track selector to extract.</summary>
        public string TrackSelector { get; init; } = string.Empty;

        [CommandOption("--out <FILE>")]
        /// <summary>Gets the output audio file path.</summary>
        public string OutputFile { get; init; } = string.Empty;

        /// <summary>Validates the provided settings for audio extraction.</summary>
        /// <returns>A validation result indicating success or failure.</returns>
        public override ValidationResult Validate()
        {
            // Block: Validate the required input file before extraction.
            var inputValidation = ValidationHelpers.ValidateFileExists(InputFile, "Input file");
            if (!inputValidation.Successful)
            {
                return inputValidation;
            }

            if (string.IsNullOrWhiteSpace(TrackSelector))
            {
                // Block: Require a track selector to identify the audio stream.
                return ValidationResult.Error("Track selector is required.");
            }

            if (string.IsNullOrWhiteSpace(OutputFile))
            {
                // Block: Require a destination file for extracted audio.
                return ValidationResult.Error("Output file is required.");
            }

            return ValidationResult.Success();
        }
    }

    /// <summary>Initializes the command with the console used for output.</summary>
    /// <param name="console">The console used to render command output.</param>
    public ExtractAudioCommand(IAnsiConsole console, ToolResolver toolResolver) : base(console)
    {
        // Block: Delegate console handling to the base command class.
        _toolResolver = toolResolver;
    }

    protected override async Task<int> ExecuteAsyncCore(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        // Block: Create tooling services scoped to this command execution.
        using var tooling = ToolingFactory.CreateTooling(settings, Console, _toolResolver);
        if (settings.DryRun)
        {
            if (!int.TryParse(settings.TrackSelector, out var index))
            {
                // Block: Reject non-numeric track selectors for dry-run rendering.
                throw new ValidationException("Dry-run for --track requires a numeric index selector.");
            }

            // Block: Render the extraction command without executing it.
            var ffmpeg = tooling.GetRequiredService<FfmpegClient>();
            Console.MarkupLine($"[grey]{Markup.Escape(ffmpeg.BuildExtractAudioCommand(settings.InputFile, index, settings.OutputFile).Rendered)}[/]");
            return 0;
        }

        // Block: Execute the audio extraction and report completion.
        await tooling.Service.ExtractAudioAsync(
            settings.InputFile,
            settings.TrackSelector,
            settings.OutputFile,
            cancellationToken).ConfigureAwait(false);
        Console.MarkupLine($"[green]Extracted audio to[/] {Markup.Escape(settings.OutputFile)}");
        return 0;
    }
}
