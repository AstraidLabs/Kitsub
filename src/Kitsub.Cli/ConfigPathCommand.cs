// Summary: Prints resolved configuration file paths.
using Spectre.Console;
using Spectre.Console.Cli;

namespace Kitsub.Cli;

/// <summary>Displays resolved configuration file paths.</summary>
public sealed class ConfigPathCommand : ConfigCommandBase<ConfigPathCommand.Settings>
{
    public sealed class Settings : GlobalSettings
    {
    }

    public ConfigPathCommand(IAnsiConsole console, AppConfigService configService) : base(console, configService)
    {
    }

    protected override Task<int> ExecuteAsyncCore(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var paths = ConfigService.GetPaths();
        Console.MarkupLine($"[bold]Global config[/]: {Markup.Escape(paths.GlobalConfigPath)}");
        Console.MarkupLine($"[bold]ffmpeg override[/]: {Markup.Escape(paths.FfmpegOverridePath)}");
        Console.MarkupLine($"[bold]ffprobe override[/]: {Markup.Escape(paths.FfprobeOverridePath)}");
        Console.MarkupLine($"[bold]mkvmerge override[/]: {Markup.Escape(paths.MkvmergeOverridePath)}");
        Console.MarkupLine($"[bold]mkvpropedit override[/]: {Markup.Escape(paths.MkvpropeditOverridePath)}");
        return Task.FromResult(ExitCodes.Success);
    }
}
