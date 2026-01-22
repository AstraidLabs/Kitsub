// Summary: Implements the CLI command that extracts video streams from media files.
using Kitsub.Tooling;
using Kitsub.Tooling.Provisioning;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Kitsub.Cli;

/// <summary>Executes video extraction using external tooling.</summary>
public sealed class ExtractVideoCommand : CommandBase<ExtractVideoCommand.Settings>
{
    private readonly ToolResolver _toolResolver;

    /// <summary>Defines command-line settings for video extraction.</summary>
    public sealed class Settings : ToolSettings
    {
        [CommandOption("--in <FILE>")]
        /// <summary>Gets the input media file path.</summary>
        public string InputFile { get; init; } = string.Empty;

        [CommandOption("--out <FILE>")]
        /// <summary>Gets the output video file path.</summary>
        public string OutputFile { get; init; } = string.Empty;

        [CommandOption("--force")]
        /// <summary>Gets a value indicating whether existing output files should be overwritten.</summary>
        public bool Force { get; init; }

        /// <summary>Validates the provided settings for video extraction.</summary>
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

            if (string.IsNullOrWhiteSpace(OutputFile))
            {
                // Block: Require a destination file for extracted video.
                return ValidationResult.Error("Missing required option: --out. Fix: provide --out <file>.");
            }

            return ValidationResult.Success();
        }
    }

    /// <summary>Initializes the command with the console used for output.</summary>
    /// <param name="console">The console used to render command output.</param>
    public ExtractVideoCommand(
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
            // Block: Render the extraction command without executing it.
            var ffmpeg = tooling.GetRequiredService<FfmpegClient>();
            Console.MarkupLine($"[grey]{Markup.Escape(ffmpeg.BuildExtractVideoCommand(settings.InputFile, settings.OutputFile).Rendered)}[/]");
            return 0;
        }

        // Block: Execute the video extraction and report completion.
        await tooling.Service.ExtractVideoAsync(settings.InputFile, settings.OutputFile, cancellationToken)
            .ConfigureAwait(false);
        Console.MarkupLine($"[green]Extracted video to[/] {Markup.Escape(settings.OutputFile)}");
        return 0;
    }

    protected override ToolRequirement GetToolRequirement(Settings settings)
    {
        return ToolRequirement.For(ToolKind.Ffmpeg);
    }
}
