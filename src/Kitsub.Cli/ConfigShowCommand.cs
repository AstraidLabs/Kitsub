// Summary: Displays Kitsub configuration content.
using System.Text.Json;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Kitsub.Cli;

/// <summary>Shows configuration files for Kitsub.</summary>
public sealed class ConfigShowCommand : ConfigCommandBase<ConfigShowCommand.Settings>
{
    public sealed class Settings : GlobalSettings
    {
        [CommandOption("--effective")]
        public bool Effective { get; set; }
    }

    public ConfigShowCommand(IAnsiConsole console, AppConfigService configService) : base(console, configService)
    {
    }

    protected override Task<int> ExecuteAsyncCore(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        if (settings.Effective)
        {
            var effective = ConfigService.LoadEffectiveConfig();
            var json = JsonSerializer.Serialize(effective, AppConfigLoader.GetJsonOptions());
            Console.WriteLine(json);
            return Task.FromResult(ExitCodes.Success);
        }

        var loaded = ConfigService.LoadGlobalConfig();
        if (!loaded.Found)
        {
            Console.MarkupLine($"[yellow]No global config found at[/] {Markup.Escape(loaded.Path)}");
            return Task.FromResult(ExitCodes.Success);
        }

        var output = JsonSerializer.Serialize(loaded.Config, AppConfigLoader.GetJsonOptions());
        Console.WriteLine(output);
        return Task.FromResult(ExitCodes.Success);
    }
}
