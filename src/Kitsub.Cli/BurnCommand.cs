using Kitsub.Tooling;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Kitsub.Cli;

public sealed class BurnCommand : CommandBase<BurnCommand.Settings>
{
    public sealed class Settings : ToolSettings
    {
        [CommandOption("--in <FILE>")]
        public string InputFile { get; init; } = string.Empty;

        [CommandOption("--sub <FILE>")]
        public string? SubtitleFile { get; init; }

        [CommandOption("--track <SELECTOR>")]
        public string? TrackSelector { get; init; }

        [CommandOption("--out <FILE>")]
        public string OutputFile { get; init; } = string.Empty;

        [CommandOption("--fontsdir <DIR>")]
        public string? FontsDir { get; init; }

        [CommandOption("--crf <N>")]
        public int Crf { get; init; } = 18;

        [CommandOption("--preset <NAME>")]
        public string Preset { get; init; } = "medium";

        public override ValidationResult Validate()
        {
            var inputValidation = ValidationHelpers.ValidateFileExists(InputFile, "Input file");
            if (!inputValidation.Successful)
            {
                return inputValidation;
            }

            if (!string.IsNullOrWhiteSpace(FontsDir))
            {
                var dirValidation = ValidationHelpers.ValidateDirectoryExists(FontsDir, "Fonts directory");
                if (!dirValidation.Successful)
                {
                    return dirValidation;
                }
            }

            if (string.IsNullOrWhiteSpace(OutputFile))
            {
                return ValidationResult.Error("Output file is required.");
            }

            if (string.IsNullOrWhiteSpace(SubtitleFile) && string.IsNullOrWhiteSpace(TrackSelector))
            {
                return ValidationResult.Error("Provide either --sub or --track.");
            }

            if (!string.IsNullOrWhiteSpace(SubtitleFile) && !string.IsNullOrWhiteSpace(TrackSelector))
            {
                return ValidationResult.Error("Use either --sub or --track, not both.");
            }

            if (!string.IsNullOrWhiteSpace(SubtitleFile))
            {
                var subValidation = ValidationHelpers.ValidateFileExists(SubtitleFile, "Subtitle file");
                if (!subValidation.Successful)
                {
                    return subValidation;
                }
            }

            return ValidationResult.Success();
        }
    }

    public BurnCommand(IAnsiConsole console) : base(console)
    {
    }

    protected override async Task<int> ExecuteAsyncCore(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        using var tooling = ToolingFactory.CreateTooling(settings, Console);
        if (settings.DryRun)
        {
            await RenderDryRunAsync(tooling, settings).ConfigureAwait(false);
            return 0;
        }

        if (!string.IsNullOrWhiteSpace(settings.SubtitleFile))
        {
            await tooling.Service.BurnSubtitlesAsync(
                settings.InputFile,
                settings.SubtitleFile,
                settings.OutputFile,
                settings.FontsDir,
                settings.Crf,
                settings.Preset,
                cancellationToken).ConfigureAwait(false);
            Console.MarkupLine($"[green]Burned subtitles into[/] {Markup.Escape(settings.OutputFile)}");
            return 0;
        }

        var tempFile = await tooling.Service.ExtractSubtitleToTempAsync(
            settings.InputFile,
            settings.TrackSelector!,
            cancellationToken).ConfigureAwait(false);

        try
        {
            await tooling.Service.BurnSubtitlesAsync(
                settings.InputFile,
                tempFile,
                settings.OutputFile,
                settings.FontsDir,
                settings.Crf,
                settings.Preset,
                cancellationToken).ConfigureAwait(false);
            Console.MarkupLine($"[green]Burned subtitles into[/] {Markup.Escape(settings.OutputFile)}");
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }

        return 0;
    }

    private Task RenderDryRunAsync(ToolingContext tooling, Settings settings)
    {
        var ffmpeg = tooling.GetRequiredService<FfmpegClient>();

        if (!string.IsNullOrWhiteSpace(settings.SubtitleFile))
        {
            Console.MarkupLine($"[grey]{Markup.Escape(ffmpeg.BuildBurnSubtitlesCommand(settings.InputFile, settings.SubtitleFile, settings.OutputFile, settings.FontsDir, settings.Crf, settings.Preset).Rendered)}[/]");
            return Task.CompletedTask;
        }

        if (!int.TryParse(settings.TrackSelector, out var subtitleIndex))
        {
            throw new ValidationException("Dry-run for --track requires a numeric index selector.");
        }

        var tempFile = Path.Combine(Path.GetTempPath(), $"kitsub_dryrun_{Guid.NewGuid():N}.ass");
        Console.MarkupLine($"[grey]{Markup.Escape(ffmpeg.BuildExtractSubtitleCommand(settings.InputFile, subtitleIndex, tempFile).Rendered)}[/]");
        Console.MarkupLine($"[grey]{Markup.Escape(ffmpeg.BuildBurnSubtitlesCommand(settings.InputFile, tempFile, settings.OutputFile, settings.FontsDir, settings.Crf, settings.Preset).Rendered)}[/]");
        return Task.CompletedTask;
    }
}
