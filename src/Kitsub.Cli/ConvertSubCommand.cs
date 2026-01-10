using Kitsub.Tooling;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Kitsub.Cli;

public sealed class ConvertSubCommand : CommandBase<ConvertSubCommand.Settings>
{
    public sealed class Settings : ToolSettings
    {
        [CommandOption("--in <FILE>")]
        public string InputFile { get; init; } = string.Empty;

        [CommandOption("--out <FILE>")]
        public string OutputFile { get; init; } = string.Empty;

        public override ValidationResult Validate()
        {
            var inputValidation = ValidationHelpers.ValidateFileExists(InputFile, "Input file");
            if (!inputValidation.Successful)
            {
                return inputValidation;
            }

            if (string.IsNullOrWhiteSpace(OutputFile))
            {
                return ValidationResult.Error("Output file is required.");
            }

            return ValidationResult.Success();
        }
    }

    public ConvertSubCommand(IAnsiConsole console) : base(console)
    {
    }

    protected override async Task<int> ExecuteAsyncCore(CommandContext context, Settings settings)
    {
        using var tooling = ToolingFactory.CreateTooling(settings, Console);
        if (settings.DryRun)
        {
            var ffmpeg = tooling.GetRequiredService<FfmpegClient>();
            Console.MarkupLine($"[grey]{Markup.Escape(ffmpeg.BuildConvertSubtitleCommand(settings.InputFile, settings.OutputFile).Rendered)}[/]");
            return 0;
        }

        await tooling.Service.ConvertSubtitleAsync(settings.InputFile, settings.OutputFile, context.GetCancellationToken())
            .ConfigureAwait(false);
        Console.MarkupLine($"[green]Converted subtitles to[/] {Markup.Escape(settings.OutputFile)}");
        return 0;
    }
}
