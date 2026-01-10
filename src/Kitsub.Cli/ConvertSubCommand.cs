// Summary: Implements the CLI command that converts subtitle files to a new format.
using Kitsub.Tooling;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Kitsub.Cli;

/// <summary>Executes subtitle conversion using external tooling.</summary>
public sealed class ConvertSubCommand : CommandBase<ConvertSubCommand.Settings>
{
    /// <summary>Defines command-line settings for subtitle conversion.</summary>
    public sealed class Settings : ToolSettings
    {
        [CommandOption("--in <FILE>")]
        /// <summary>Gets the input subtitle file path.</summary>
        public string InputFile { get; init; } = string.Empty;

        [CommandOption("--out <FILE>")]
        /// <summary>Gets the output subtitle file path.</summary>
        public string OutputFile { get; init; } = string.Empty;

        /// <summary>Validates the provided settings for subtitle conversion.</summary>
        /// <returns>A validation result indicating success or failure.</returns>
        public override ValidationResult Validate()
        {
            // Block: Validate the required input file before conversion.
            var inputValidation = ValidationHelpers.ValidateFileExists(InputFile, "Input file");
            if (!inputValidation.Successful)
            {
                return inputValidation;
            }

            if (string.IsNullOrWhiteSpace(OutputFile))
            {
                // Block: Require a target output file path for conversion results.
                return ValidationResult.Error("Output file is required.");
            }

            return ValidationResult.Success();
        }
    }

    /// <summary>Initializes the command with the console used for output.</summary>
    /// <param name="console">The console used to render command output.</param>
    public ConvertSubCommand(IAnsiConsole console) : base(console)
    {
        // Block: Delegate console handling to the base command class.
    }

    protected override async Task<int> ExecuteAsyncCore(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        // Block: Create tooling services scoped to this command execution.
        using var tooling = ToolingFactory.CreateTooling(settings, Console);
        if (settings.DryRun)
        {
            // Block: Render the conversion command without executing it.
            var ffmpeg = tooling.GetRequiredService<FfmpegClient>();
            Console.MarkupLine($"[grey]{Markup.Escape(ffmpeg.BuildConvertSubtitleCommand(settings.InputFile, settings.OutputFile).Rendered)}[/]");
            return 0;
        }

        // Block: Execute the subtitle conversion and report completion.
        await tooling.Service.ConvertSubtitleAsync(settings.InputFile, settings.OutputFile, cancellationToken)
            .ConfigureAwait(false);
        Console.MarkupLine($"[green]Converted subtitles to[/] {Markup.Escape(settings.OutputFile)}");
        return 0;
    }
}
