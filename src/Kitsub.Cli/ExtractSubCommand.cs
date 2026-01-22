// Summary: Implements the CLI command that extracts subtitle tracks from media files.
using Kitsub.Core;
using Kitsub.Tooling;
using Kitsub.Tooling.Provisioning;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Kitsub.Cli;

/// <summary>Executes subtitle extraction using external tooling.</summary>
public sealed class ExtractSubCommand : CommandBase<ExtractSubCommand.Settings>
{
    private readonly ToolResolver _toolResolver;

    /// <summary>Defines command-line settings for subtitle extraction.</summary>
    public sealed class Settings : ToolSettings
    {
        [CommandOption("--in <FILE>")]
        /// <summary>Gets the input media file path.</summary>
        public string InputFile { get; init; } = string.Empty;

        [CommandOption("--track <SELECTOR>")]
        /// <summary>Gets the subtitle track selector to extract.</summary>
        public string TrackSelector { get; init; } = string.Empty;

        [CommandOption("--out <FILE>")]
        /// <summary>Gets the output subtitle file path.</summary>
        public string OutputFile { get; init; } = string.Empty;

        [CommandOption("--force")]
        /// <summary>Gets a value indicating whether existing output files should be overwritten.</summary>
        public bool Force { get; init; }

        /// <summary>Validates the provided settings for subtitle extraction.</summary>
        /// <returns>A validation result indicating success or failure.</returns>
        public override ValidationResult Validate()
        {
            if (string.IsNullOrWhiteSpace(InputFile))
            {
                return ValidationResult.Error("Missing required option: --in. Fix: provide --in <file>.");
            }

            // Block: Validate the required input file before extraction.
            var inputValidation = ValidationHelpers.ValidateFileExists(InputFile, "Input file");
            if (!inputValidation.Successful)
            {
                return inputValidation;
            }

            if (string.IsNullOrWhiteSpace(TrackSelector))
            {
                // Block: Require a track selector to identify the subtitle stream.
                return ValidationResult.Error("Missing required option: --track. Fix: provide --track <selector>.");
            }

            if (string.IsNullOrWhiteSpace(OutputFile))
            {
                // Block: Require a destination file for extracted subtitles.
                return ValidationResult.Error("Missing required option: --out. Fix: provide --out <file>.");
            }

            var selectorValidation = ValidationHelpers.ValidateTrackSelectorSyntax(TrackSelector);
            if (!selectorValidation.Successful)
            {
                return selectorValidation;
            }

            return ValidationResult.Success();
        }
    }

    /// <summary>Initializes the command with the console used for output.</summary>
    /// <param name="console">The console used to render command output.</param>
    public ExtractSubCommand(
        IAnsiConsole console,
        ToolResolver toolResolver,
        ToolBundleManager bundleManager,
        WindowsRidDetector ridDetector,
        AppConfigService configService) : base(console, configService, toolResolver, bundleManager, ridDetector)
    {
        // Block: Delegate console handling to the base command class.
        _toolResolver = toolResolver;
    }

    protected override async Task<int> ExecuteAsyncCore(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        // Block: Create tooling services scoped to this command execution.
        using var tooling = ToolingFactory.CreateTooling(settings, Console, _toolResolver);
        ValidationHelpers.EnsureOutputPath(
            settings.OutputFile,
            "Output file",
            allowCreateDirectory: true,
            allowOverwrite: settings.Force,
            inputPath: settings.InputFile,
            createDirectory: !settings.DryRun);

        if (settings.DryRun)
        {
            if (!int.TryParse(settings.TrackSelector, out var index))
            {
                // Block: Reject non-numeric track selectors for dry-run rendering.
                throw new ValidationException("Invalid value for --track: must be numeric when using --dry-run. Fix: use a numeric track index.");
            }

            // Block: Render the extraction command without executing it.
            var ffmpeg = tooling.GetRequiredService<FfmpegClient>();
            Console.MarkupLine($"[grey]{Markup.Escape(ffmpeg.BuildExtractSubtitleCommand(settings.InputFile, index, settings.OutputFile).Rendered)}[/]");
            return 0;
        }

        var info = await tooling.GetRequiredService<FfprobeClient>()
            .ProbeAsync(settings.InputFile, cancellationToken).ConfigureAwait(false);
        var selection = ValidationHelpers.ResolveTrackSelection(
            info,
            TrackType.Subtitle,
            settings.TrackSelector,
            rejectBitmapSubtitles: true,
            settings.InputFile);
        if (!string.IsNullOrWhiteSpace(selection.Warning))
        {
            Console.MarkupLine($"[yellow]{Markup.Escape(selection.Warning)}[/]");
        }

        // Block: Execute the subtitle extraction and report completion.
        try
        {
            await tooling.Service.ExtractSubtitleAsync(
                settings.InputFile,
                settings.TrackSelector,
                settings.OutputFile,
                cancellationToken).ConfigureAwait(false);
        }
        catch (InvalidOperationException ex)
        {
            throw new ValidationException($"Failed to resolve subtitle track. Fix: run `kitsub inspect {settings.InputFile}` to list tracks. ({ex.Message})", ex);
        }
        Console.MarkupLine($"[green]Extracted subtitles to[/] {Markup.Escape(settings.OutputFile)}");
        return 0;
    }

    protected override ToolRequirement GetToolRequirement(Settings settings)
    {
        return ToolRequirement.For(ToolKind.Ffprobe, ToolKind.Ffmpeg);
    }
}
