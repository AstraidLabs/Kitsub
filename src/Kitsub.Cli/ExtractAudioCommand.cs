using Kitsub.Tooling;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Kitsub.Cli;

public sealed class ExtractAudioCommand : CommandBase<ExtractAudioCommand.Settings>
{
    public sealed class Settings : ToolSettings
    {
        [CommandOption("--in <FILE>")]
        public string InputFile { get; init; } = string.Empty;

        [CommandOption("--track <SELECTOR>")]
        public string TrackSelector { get; init; } = string.Empty;

        [CommandOption("--out <FILE>")]
        public string OutputFile { get; init; } = string.Empty;

        public override ValidationResult Validate()
        {
            var inputValidation = ValidationHelpers.ValidateFileExists(InputFile, "Input file");
            if (!inputValidation.Successful)
            {
                return inputValidation;
            }

            if (string.IsNullOrWhiteSpace(TrackSelector))
            {
                return ValidationResult.Error("Track selector is required.");
            }

            if (string.IsNullOrWhiteSpace(OutputFile))
            {
                return ValidationResult.Error("Output file is required.");
            }

            return ValidationResult.Success();
        }
    }

    public ExtractAudioCommand(IAnsiConsole console) : base(console)
    {
    }

    protected override async Task<int> ExecuteAsyncCore(CommandContext context, Settings settings)
    {
        using var tooling = ToolingFactory.CreateTooling(settings, Console);
        if (settings.DryRun)
        {
            if (!int.TryParse(settings.TrackSelector, out var index))
            {
                throw new ValidationException("Dry-run for --track requires a numeric index selector.");
            }

            var ffmpeg = tooling.GetRequiredService<FfmpegClient>();
            Console.MarkupLine($"[grey]{Markup.Escape(ffmpeg.BuildExtractAudioCommand(settings.InputFile, index, settings.OutputFile).Rendered)}[/]");
            return 0;
        }

        await tooling.Service.ExtractAudioAsync(
            settings.InputFile,
            settings.TrackSelector,
            settings.OutputFile,
            context.GetCancellationToken()).ConfigureAwait(false);
        Console.MarkupLine($"[green]Extracted audio to[/] {Markup.Escape(settings.OutputFile)}");
        return 0;
    }
}
