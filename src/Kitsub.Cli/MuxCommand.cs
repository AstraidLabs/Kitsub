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
        public string InputMkv { get; set; } = string.Empty;

        [CommandOption("--sub <FILE>")]
        /// <summary>Gets the subtitle files to mux.</summary>
        public string[] Subtitles { get; set; } = Array.Empty<string>();

        [CommandOption("--lang <ISO>")]
        /// <summary>Gets the optional language tag for subtitle tracks.</summary>
        public string? Language { get; set; }

        [CommandOption("--title <NAME>")]
        /// <summary>Gets the optional title for subtitle tracks.</summary>
        public string? Title { get; set; }

        [CommandOption("--default")]
        /// <summary>Gets a value indicating whether tracks should be marked as default.</summary>
        public bool? Default { get; set; }

        [CommandOption("--no-default")]
        /// <summary>Gets a value indicating whether tracks should not be marked as default.</summary>
        public bool? NoDefault { get; set; }

        [CommandOption("--forced")]
        /// <summary>Gets a value indicating whether tracks should be marked as forced.</summary>
        public bool? Forced { get; set; }

        [CommandOption("--no-forced")]
        /// <summary>Gets a value indicating whether tracks should not be marked as forced.</summary>
        public bool? NoForced { get; set; }

        [CommandOption("--out <FILE>")]
        /// <summary>Gets the optional output MKV file path.</summary>
        public string? Output { get; set; }

        [CommandOption("--force")]
        /// <summary>Gets a value indicating whether existing output files should be overwritten.</summary>
        public bool Force { get; set; }

        /// <summary>Validates the provided settings for muxing subtitles.</summary>
        /// <returns>A validation result indicating success or failure.</returns>
        public override ValidationResult Validate()
        {
            if (string.IsNullOrWhiteSpace(InputMkv))
            {
                return ValidationResult.Error("Missing required option: --in. Fix: provide --in <file>.");
            }

            var extensionValidation = ValidationHelpers.ValidateFileExtension(InputMkv, ".mkv", "Input");
            if (!extensionValidation.Successful)
            {
                return extensionValidation;
            }

            // Block: Validate the required input MKV file before muxing.
            var inputValidation = ValidationHelpers.ValidateFileExists(InputMkv, "Input MKV");
            if (!inputValidation.Successful)
            {
                return inputValidation;
            }

            if (Subtitles.Length == 0)
            {
                // Block: Require at least one subtitle file to mux.
                return ValidationResult.Error("Missing required option: --sub. Fix: provide at least one subtitle file.");
            }

            foreach (var subtitle in Subtitles)
            {
                // Block: Validate each subtitle file provided on the command line.
                var subValidation = ValidationHelpers.ValidateFileExists(subtitle, "Subtitle file");
                if (!subValidation.Successful)
                {
                    return subValidation;
                }

                var subtitleFormatValidation = ValidationHelpers.ValidateSubtitleFile(subtitle, "Subtitle file");
                if (!subtitleFormatValidation.Successful)
                {
                    return subtitleFormatValidation;
                }
            }

            if (Default == true && NoDefault == true)
            {
                // Block: Prevent conflicting default flag configuration.
                return ValidationResult.Error("Use either --default or --no-default, not both. Fix: remove one of the options.");
            }

            if (Forced == true && NoForced == true)
            {
                // Block: Prevent conflicting forced flag configuration.
                return ValidationResult.Error("Use either --forced or --no-forced, not both. Fix: remove one of the options.");
            }

            if (!string.IsNullOrWhiteSpace(Output))
            {
                var outputValidation = ValidationHelpers.ValidateFileExtension(Output, ".mkv", "Output");
                if (!outputValidation.Successful)
                {
                    return outputValidation;
                }
            }

            var languageValidation = ValidationHelpers.ValidateLanguageTag(Language, "Subtitle language");
            if (!languageValidation.Successful)
            {
                return languageValidation;
            }

            var titleValidation = ValidationHelpers.ValidateTitle(Title, "Subtitle title");
            if (!titleValidation.Successful)
            {
                return titleValidation;
            }

            return ValidationResult.Success();
        }
    }

    /// <summary>Initializes the command with the console used for output.</summary>
    /// <param name="console">The console used to render command output.</param>
    public MuxCommand(
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
        ApplyDefaults(settings);

        // Block: Determine the output file path when one is not explicitly provided.
        var output = settings.Output;
        if (string.IsNullOrWhiteSpace(output))
        {
            // Block: Construct a default output path alongside the input MKV.
            var name = Path.GetFileNameWithoutExtension(settings.InputMkv);
            output = Path.Combine(Path.GetDirectoryName(settings.InputMkv) ?? string.Empty, $"{name}.kitsub.mkv");
        }

        ValidationHelpers.EnsureOutputPath(
            output,
            "Output file",
            allowCreateDirectory: true,
            allowOverwrite: settings.Force,
            inputPath: settings.InputMkv,
            createDirectory: !settings.DryRun);

        // Block: Resolve default and forced flags based on mutually exclusive options.
        var defaultFlag = settings.Default == true
            ? true
            : settings.NoDefault == true
                ? false
                : EffectiveConfig.Defaults.Mux.DefaultDefaultFlag;
        var forcedFlag = settings.Forced == true
            ? true
            : settings.NoForced == true
                ? false
                : EffectiveConfig.Defaults.Mux.DefaultForcedFlag;

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

    protected override ToolRequirement GetToolRequirement(Settings settings)
    {
        return ToolRequirement.For(ToolKind.Mkvmerge);
    }

    private void ApplyDefaults(Settings settings)
    {
        var defaults = EffectiveConfig.Defaults.Mux;
        settings.Language ??= defaults.DefaultLanguage;
        settings.Title ??= defaults.DefaultTrackName;
    }
}
