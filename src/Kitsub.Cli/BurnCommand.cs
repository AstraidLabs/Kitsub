// Summary: Implements the CLI command that burns subtitles into video output.
using Kitsub.Tooling;
using Kitsub.Tooling.Provisioning;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Kitsub.Cli;

/// <summary>Executes the burn command to render subtitles into a video file.</summary>
public sealed class BurnCommand : CommandBase<BurnCommand.Settings>
{
    private readonly ToolResolver _toolResolver;

    /// <summary>Defines command-line settings for burning subtitles.</summary>
    public sealed class Settings : ToolSettings
    {
        [CommandOption("--in <FILE>")]
        /// <summary>Gets the input media file path.</summary>
        public string InputFile { get; set; } = string.Empty;

        [CommandOption("--sub <FILE>")]
        /// <summary>Gets the subtitle file path to burn, when provided.</summary>
        public string? SubtitleFile { get; set; }

        [CommandOption("--track <SELECTOR>")]
        /// <summary>Gets the subtitle track selector when burning from a media track.</summary>
        public string? TrackSelector { get; set; }

        [CommandOption("--out <FILE>")]
        /// <summary>Gets the output media file path.</summary>
        public string OutputFile { get; set; } = string.Empty;

        [CommandOption("--fontsdir <DIR>")]
        /// <summary>Gets the optional directory containing fonts used for subtitles.</summary>
        public string? FontsDir { get; set; }

        [CommandOption("--crf <N>")]
        /// <summary>Gets the constant rate factor used for video encoding.</summary>
        public int? Crf { get; set; }

        [CommandOption("--preset <NAME>")]
        /// <summary>Gets the encoder preset used for video encoding.</summary>
        public string? Preset { get; set; }

        /// <summary>Validates the provided settings for the burn command.</summary>
        /// <returns>A validation result indicating success or failure.</returns>
        public override ValidationResult Validate()
        {
            // Block: Validate the required input file before continuing.
            var inputValidation = ValidationHelpers.ValidateFileExists(InputFile, "Input file");
            if (!inputValidation.Successful)
            {
                return inputValidation;
            }

            if (!string.IsNullOrWhiteSpace(FontsDir))
            {
                // Block: Validate the fonts directory only when it is provided.
                var dirValidation = ValidationHelpers.ValidateDirectoryExists(FontsDir, "Fonts directory");
                if (!dirValidation.Successful)
                {
                    return dirValidation;
                }
            }

            if (string.IsNullOrWhiteSpace(OutputFile))
            {
                // Block: Require an explicit output path for the burned video.
                return ValidationResult.Error("Output file is required.");
            }

            if (string.IsNullOrWhiteSpace(SubtitleFile) && string.IsNullOrWhiteSpace(TrackSelector))
            {
                // Block: Enforce that either a subtitle file or track selector is supplied.
                return ValidationResult.Error("Provide either --sub or --track.");
            }

            if (!string.IsNullOrWhiteSpace(SubtitleFile) && !string.IsNullOrWhiteSpace(TrackSelector))
            {
                // Block: Prevent ambiguous requests with both subtitle sources set.
                return ValidationResult.Error("Use either --sub or --track, not both.");
            }

            if (!string.IsNullOrWhiteSpace(SubtitleFile))
            {
                // Block: Validate the subtitle file when provided explicitly.
                var subValidation = ValidationHelpers.ValidateFileExists(SubtitleFile, "Subtitle file");
                if (!subValidation.Successful)
                {
                    return subValidation;
                }
            }

            return ValidationResult.Success();
        }
    }

    /// <summary>Initializes the command with the console used for output.</summary>
    /// <param name="console">The console used to render command output.</param>
    public BurnCommand(IAnsiConsole console, ToolResolver toolResolver, AppConfigService configService) : base(console, configService)
    {
        // Block: Delegate console handling to the base command class.
        _toolResolver = toolResolver;
    }

    protected override async Task<int> ExecuteAsyncCore(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        ApplyDefaults(settings);
        ValidateDefaults(settings);

        // Block: Create tooling services scoped to this command execution.
        using var tooling = ToolingFactory.CreateTooling(settings, Console, _toolResolver);
        if (settings.DryRun)
        {
            // Block: Render the external tool commands without executing them.
            await RenderDryRunAsync(tooling, settings).ConfigureAwait(false);
            return 0;
        }

        if (!string.IsNullOrWhiteSpace(settings.SubtitleFile))
        {
            // Block: Burn subtitles from a provided subtitle file.
            await tooling.Service.BurnSubtitlesAsync(
                settings.InputFile,
                settings.SubtitleFile,
                settings.OutputFile,
                settings.FontsDir,
                settings.Crf!.Value,
                settings.Preset!,
                cancellationToken).ConfigureAwait(false);
            Console.MarkupLine($"[green]Burned subtitles into[/] {Markup.Escape(settings.OutputFile)}");
            return 0;
        }

        // Block: Extract a subtitle track to a temp file before burning.
        var tempFile = await tooling.Service.ExtractSubtitleToTempAsync(
            settings.InputFile,
            settings.TrackSelector!,
            cancellationToken).ConfigureAwait(false);

        try
        {
            // Block: Burn subtitles using the extracted temporary subtitle file.
            await tooling.Service.BurnSubtitlesAsync(
                settings.InputFile,
                tempFile,
                settings.OutputFile,
                settings.FontsDir,
                settings.Crf!.Value,
                settings.Preset!,
                cancellationToken).ConfigureAwait(false);
            Console.MarkupLine($"[green]Burned subtitles into[/] {Markup.Escape(settings.OutputFile)}");
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                // Block: Ensure the temporary subtitle file is removed after burning.
                File.Delete(tempFile);
            }
        }

        return 0;
    }

    private Task RenderDryRunAsync(ToolingContext tooling, Settings settings)
    {
        // Block: Build command lines without executing to show intended operations.
        var ffmpeg = tooling.GetRequiredService<FfmpegClient>();

        if (!string.IsNullOrWhiteSpace(settings.SubtitleFile))
        {
            // Block: Render the burn command using the provided subtitle file.
            Console.MarkupLine($"[grey]{Markup.Escape(ffmpeg.BuildBurnSubtitlesCommand(settings.InputFile, settings.SubtitleFile, settings.OutputFile, settings.FontsDir, settings.Crf!.Value, settings.Preset!).Rendered)}[/]");
            return Task.CompletedTask;
        }

        if (!int.TryParse(settings.TrackSelector, out var subtitleIndex))
        {
            // Block: Reject non-numeric track selectors for dry-run extraction.
            throw new ValidationException("Dry-run for --track requires a numeric index selector.");
        }

        // Block: Render extraction and burn commands using a temporary subtitle output.
        var tempFile = Path.Combine(Path.GetTempPath(), $"kitsub_dryrun_{Guid.NewGuid():N}.ass");
        Console.MarkupLine($"[grey]{Markup.Escape(ffmpeg.BuildExtractSubtitleCommand(settings.InputFile, subtitleIndex, tempFile).Rendered)}[/]");
        Console.MarkupLine($"[grey]{Markup.Escape(ffmpeg.BuildBurnSubtitlesCommand(settings.InputFile, tempFile, settings.OutputFile, settings.FontsDir, settings.Crf!.Value, settings.Preset!).Rendered)}[/]");
        return Task.CompletedTask;
    }

    private void ApplyDefaults(Settings settings)
    {
        var defaults = EffectiveConfig.Defaults.Burn;
        settings.Crf ??= defaults.Crf;
        settings.Preset ??= defaults.Preset;
        settings.FontsDir ??= defaults.FontsDir;

        settings.Crf ??= 18;
        settings.Preset ??= "medium";
    }

    private static void ValidateDefaults(Settings settings)
    {
        if (settings.Crf is < 0 or > 51)
        {
            throw new ValidationException("CRF must be between 0 and 51.");
        }

        if (!string.IsNullOrWhiteSpace(settings.FontsDir))
        {
            var dirValidation = ValidationHelpers.ValidateDirectoryExists(settings.FontsDir, "Fonts directory");
            if (!dirValidation.Successful)
            {
                throw new ValidationException(dirValidation.Message ?? "Fonts directory does not exist.");
            }
        }
    }
}
