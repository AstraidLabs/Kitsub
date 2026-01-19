// Summary: Creates a default Kitsub configuration file.
using System.Text.Json;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Kitsub.Cli;

/// <summary>Initializes the global configuration file.</summary>
public sealed class ConfigInitCommand : ConfigCommandBase<ConfigInitCommand.Settings>
{
    public sealed class Settings : GlobalSettings
    {
        [CommandOption("--force")]
        public bool Force { get; set; }
    }

    public ConfigInitCommand(IAnsiConsole console, AppConfigService configService) : base(console, configService)
    {
    }

    protected override Task<int> ExecuteAsyncCore(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var paths = ConfigService.GetPaths();
        var target = paths.GlobalConfigPath;
        if (File.Exists(target) && !settings.Force)
        {
            Console.MarkupLine($"[yellow]Config already exists:[/] {Markup.Escape(target)}");
            Console.MarkupLine("Use --force to overwrite the existing file.");
            return Task.FromResult(ExitCodes.Success);
        }

        var sample = AppConfigDefaults.CreateDefaults();
        var json = JsonSerializer.Serialize(sample, AppConfigLoader.GetJsonOptions());
        ConfigWriter.WriteAtomic(target, json);

        Console.MarkupLine($"[green]Wrote config:[/] {Markup.Escape(target)}");
        return Task.FromResult(ExitCodes.Success);
    }
}
