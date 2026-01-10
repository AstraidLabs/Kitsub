using Kitsub.Tooling;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Kitsub.Cli;

public sealed class FontsCheckCommand : CommandBase<FontsCheckCommand.Settings>
{
    public sealed class Settings : ToolSettings
    {
        [CommandOption("--in <MKV>")]
        public string InputMkv { get; init; } = string.Empty;

        public override ValidationResult Validate()
        {
            return ValidationHelpers.ValidateFileExists(InputMkv, "Input MKV");
        }
    }

    public FontsCheckCommand(IAnsiConsole console) : base(console)
    {
    }

    protected override async Task<int> ExecuteAsyncCore(CommandContext context, Settings settings)
    {
        using var tooling = ToolingFactory.CreateTooling(settings, Console);
        if (settings.DryRun)
        {
            var mkvmerge = tooling.GetRequiredService<MkvmergeClient>();
            Console.MarkupLine($"[grey]{Markup.Escape(mkvmerge.BuildIdentifyCommand(settings.InputMkv).Rendered)}[/]");
            return 0;
        }

        var service = tooling.Service;
        var result = await service.CheckFontsAsync(settings.InputMkv, context.GetCancellationToken()).ConfigureAwait(false);
        var hasFonts = result.HasFonts;
        var hasAss = result.HasAssSubtitles;

        if (hasFonts)
        {
            Console.MarkupLine("[green]Fonts attachments present.[/]");
        }
        else
        {
            Console.MarkupLine("[yellow]No fonts attachments detected.[/]");
        }

        if (!hasFonts && hasAss)
        {
            Console.MarkupLine("[red]Warning:[/] ASS subtitles detected without embedded fonts.");
        }

        return 0;
    }
}
