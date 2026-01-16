// Summary: Reports resolved tool paths and bundle status.
using Kitsub.Tooling.Provisioning;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Kitsub.Cli;

/// <summary>Prints current tool resolution status.</summary>
public sealed class ToolsStatusCommand : CommandBase<ToolsStatusCommand.Settings>
{
    private readonly ToolResolver _toolResolver;

    /// <summary>Defines command-line settings for tool status.</summary>
    public sealed class Settings : ToolSettings
    {
    }

    /// <summary>Initializes the command with the console used for output.</summary>
    /// <param name="console">The console used to render command output.</param>
    public ToolsStatusCommand(IAnsiConsole console, ToolResolver toolResolver, AppConfigService configService) : base(console, configService)
    {
        _toolResolver = toolResolver;
    }

    protected override Task<int> ExecuteAsyncCore(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        using var tooling = ToolingFactory.CreateTooling(settings, Console, _toolResolver);
        var paths = tooling.Paths;

        Console.MarkupLine($"[bold]RID[/]: {Markup.Escape(paths.RuntimeRid)}");
        Console.MarkupLine($"[bold]Toolset version[/]: {Markup.Escape(paths.ToolsetVersion)}");

        var table = new Table().RoundedBorder();
        table.AddColumn("Tool");
        table.AddColumn("Path");
        table.AddColumn("Source");
        table.AddColumn("Exists");

        AddRow(table, "ffmpeg", paths.Ffmpeg);
        AddRow(table, "ffprobe", paths.Ffprobe);
        AddRow(table, "mkvmerge", paths.Mkvmerge);
        AddRow(table, "mkvpropedit", paths.Mkvpropedit);
        AddRow(table, "mediainfo", paths.Mediainfo);

        Console.Write(table);
        return Task.FromResult(0);
    }

    private static void AddRow(Table table, string toolName, ToolPathResolution resolution)
    {
        var exists = Path.IsPathRooted(resolution.Path) && File.Exists(resolution.Path) ? "Yes" : "No";
        table.AddRow(toolName, resolution.Path, resolution.Source.ToString(), exists);
    }
}
