// Summary: Implements the CLI command that attaches font files to MKV containers.
using Kitsub.Tooling;
using Kitsub.Tooling.Provisioning;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Kitsub.Cli;

/// <summary>Executes font attachment for MKV files.</summary>
public sealed class FontsAttachCommand : CommandBase<FontsAttachCommand.Settings>
{
    private readonly ToolResolver _toolResolver;

    /// <summary>Defines command-line settings for attaching fonts to MKV files.</summary>
    public sealed class Settings : ToolSettings
    {
        [CommandOption("--in <MKV>")]
        /// <summary>Gets the input MKV file path.</summary>
        public string InputMkv { get; init; } = string.Empty;

        [CommandOption("--dir <DIR>")]
        /// <summary>Gets the directory containing fonts to attach.</summary>
        public string FontsDir { get; init; } = string.Empty;

        [CommandOption("--out <FILE>")]
        /// <summary>Gets the optional output MKV file path.</summary>
        public string? Output { get; init; }

        [CommandOption("--force")]
        /// <summary>Gets a value indicating whether existing output files should be overwritten.</summary>
        public bool Force { get; init; }

        /// <summary>Validates the provided settings for font attachment.</summary>
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

            // Block: Validate the required input MKV file before proceeding.
            var inputValidation = ValidationHelpers.ValidateFileExists(InputMkv, "Input MKV");
            if (!inputValidation.Successful)
            {
                return inputValidation;
            }

            if (string.IsNullOrWhiteSpace(FontsDir))
            {
                return ValidationResult.Error("Missing required option: --dir. Fix: provide --dir <folder>.");
            }

            // Block: Validate the fonts directory and ensure it exists.
            var dirValidation = ValidationHelpers.ValidateDirectoryExists(FontsDir, "Fonts directory");
            if (!dirValidation.Successful)
            {
                return dirValidation;
            }

            if (MkvmergeMuxer.EnumerateFonts(FontsDir).Count == 0)
            {
                // Block: Fail validation when no font files are present to attach.
                return ValidationResult.Error("No font files found in the directory. Fix: add supported font files or choose another folder.");
            }

            if (!string.IsNullOrWhiteSpace(Output))
            {
                var outputValidation = ValidationHelpers.ValidateFileExtension(Output, ".mkv", "Output");
                if (!outputValidation.Successful)
                {
                    return outputValidation;
                }
            }

            return ValidationResult.Success();
        }
    }

    /// <summary>Initializes the command with the console used for output.</summary>
    /// <param name="console">The console used to render command output.</param>
    public FontsAttachCommand(
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
        // Block: Determine the output file path when one is not explicitly provided.
        var output = settings.Output;
        if (string.IsNullOrWhiteSpace(output))
        {
            // Block: Construct a default output path alongside the input MKV.
            var name = Path.GetFileNameWithoutExtension(settings.InputMkv);
            output = Path.Combine(Path.GetDirectoryName(settings.InputMkv) ?? string.Empty, $"{name}.fonts.mkv");
        }

        ValidationHelpers.EnsureOutputPath(
            output,
            "Output file",
            allowCreateDirectory: true,
            allowOverwrite: settings.Force,
            inputPath: settings.InputMkv,
            createDirectory: !settings.DryRun);

        // Block: Execute font attachment using tooling services.
        using var tooling = ToolingFactory.CreateTooling(settings, Console, _toolResolver);
        await tooling.Service.AttachFontsAsync(settings.InputMkv, settings.FontsDir, output, cancellationToken)
            .ConfigureAwait(false);
        Console.MarkupLine($"[green]Attached fonts into[/] {Markup.Escape(output)}");
        return 0;
    }

    protected override ToolRequirement GetToolRequirement(Settings settings)
    {
        return ToolRequirement.For(ToolKind.Mkvmerge);
    }
}
