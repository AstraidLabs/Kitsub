// Summary: Implements the CLI command that inspects MKV files for font attachments.
using Kitsub.Tooling;
using Kitsub.Tooling.Provisioning;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Kitsub.Cli;

/// <summary>Executes font attachment checks for MKV files.</summary>
public sealed class FontsCheckCommand : CommandBase<FontsCheckCommand.Settings>
{
    private readonly ToolResolver _toolResolver;

    /// <summary>Defines command-line settings for checking font attachments.</summary>
    public sealed class Settings : ToolSettings
    {
        [CommandOption("--in <MKV>")]
        /// <summary>Gets the input MKV file path.</summary>
        public string InputMkv { get; init; } = string.Empty;

        /// <summary>Validates the provided settings for font checks.</summary>
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

            // Block: Validate the required input MKV file before inspection.
            return ValidationHelpers.ValidateFileExists(InputMkv, "Input MKV");
        }
    }

    /// <summary>Initializes the command with the console used for output.</summary>
    /// <param name="console">The console used to render command output.</param>
    public FontsCheckCommand(
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
        if (settings.DryRun)
        {
            // Block: Render the identify command without executing it.
            var mkvmerge = tooling.GetRequiredService<MkvmergeClient>();
            Console.MarkupLine($"[grey]{Markup.Escape(mkvmerge.BuildIdentifyCommand(settings.InputMkv).Rendered)}[/]");
            return 0;
        }

        // Block: Query the service for font and subtitle metadata.
        var service = tooling.Service;
        var result = await service.CheckFontsAsync(settings.InputMkv, cancellationToken).ConfigureAwait(false);
        var hasFonts = result.HasFonts;
        var hasAss = result.HasAssSubtitles;

        if (hasFonts)
        {
            // Block: Report that font attachments are present.
            Console.MarkupLine("[green]Fonts attachments present.[/]");
        }
        else
        {
            // Block: Report that no font attachments were detected.
            Console.MarkupLine("[yellow]No fonts attachments detected.[/]");
        }

        if (!hasFonts && hasAss)
        {
            // Block: Warn when ASS subtitles are present without embedded fonts.
            Console.MarkupLine("[red]Warning:[/] ASS subtitles detected without embedded fonts. Fix: attach fonts or provide an external fonts directory.");
        }

        return 0;
    }

    protected override ToolRequirement GetToolRequirement(Settings settings)
    {
        return ToolRequirement.For(ToolKind.Mkvmerge);
    }
}
