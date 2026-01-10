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
        if (settings.DryRun)
        {
            var paths = ToolingFactory.BuildToolPaths(settings);
            var runner = new CliExternalToolRunner(Console, true, settings.Verbose);
            var ffmpeg = new FfmpegClient(runner, paths);
            Console.MarkupLine($"[grey]{Markup.Escape(ffmpeg.BuildConvertSubtitleCommand(settings.InputFile, settings.OutputFile).Rendered)}[/]");
            return 0;
        }

        var service = ToolingFactory.CreateService(settings, Console);
        await service.ConvertSubtitleAsync(settings.InputFile, settings.OutputFile, context.GetCancellationToken()).ConfigureAwait(false);
        Console.MarkupLine($"[green]Converted subtitles to[/] {Markup.Escape(settings.OutputFile)}");
        return 0;
    }
}
