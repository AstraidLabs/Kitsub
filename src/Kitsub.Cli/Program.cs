// Summary: Boots the CLI application, configures services, and registers command routes.
using Kitsub.Core;
using Kitsub.Tooling.Provisioning;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Kitsub.Cli;

/// <summary>Provides the application entry point for the CLI.</summary>
public static class Program
{
    /// <summary>Builds and runs the CLI command application.</summary>
    /// <param name="args">The command-line arguments provided by the user.</param>
    /// <returns>The process exit code produced by the command execution.</returns>
    public static async Task<int> Main(string[] args)
    {
        var configService = new AppConfigService();
        AppConfig? effectiveConfig = null;
        try
        {
            effectiveConfig = configService.LoadEffectiveConfig();
        }
        catch (ConfigurationException ex)
        {
            AnsiConsole.MarkupLine($"[red]{Markup.Escape(ex.Message)}[/]");
            return ExitCodes.ValidationError;
        }

        var bootstrap = ParseBootstrapOptions(args);
        var uiConfig = effectiveConfig.Ui;
        var noBanner = bootstrap.NoBanner || (uiConfig.NoBanner ?? false);
        var noColor = bootstrap.NoColor || (uiConfig.NoColor ?? false);
        var consoleSettings = new AnsiConsoleSettings();
        if (noColor)
        {
            consoleSettings.Ansi = AnsiSupport.No;
            consoleSettings.ColorSystem = ColorSystemSupport.NoColors;
        }

        var console = AnsiConsole.Create(consoleSettings);

        if (!noBanner)
        {
            PrintBanner(console);
        }

        // Block: Configure dependency injection services used by CLI commands.
        var services = new ServiceCollection();
        services.AddSingleton<IAnsiConsole>(console);
        services.AddSingleton(configService);
        var bootstrapLogger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .CreateLogger();
        services.AddLogging(builder => builder.AddSerilog(bootstrapLogger, dispose: true));
        services.AddSingleton<ToolManifestLoader>();
        services.AddSingleton<ToolCachePaths>();
        services.AddSingleton<WindowsRidDetector>();
        services.AddSingleton<ToolBundleManager>();
        services.AddSingleton<ToolResolver>();

        // Block: Wire up the Spectre.Console command app and its routes.
        var registrar = new TypeRegistrar(services);
        var app = new CommandApp(registrar);
        app.Configure(config =>
        {
            // Block: Register top-level commands and command groups for the CLI.
            config.SetApplicationName("kitsub");
            config.AddCommand<InspectCommand>("inspect").WithDescription("Inspect media file.");
            config.AddCommand<MuxCommand>("mux").WithDescription("Mux subtitles into MKV.");
            config.AddCommand<BurnCommand>("burn").WithDescription("Burn subtitles into video.");

            config.AddBranch("fonts", fonts =>
            {
                // Block: Configure commands related to font attachment workflows.
                fonts.SetDescription("Font attachments.");
                fonts.AddCommand<FontsAttachCommand>("attach").WithDescription("Attach fonts to MKV.");
                fonts.AddCommand<FontsCheckCommand>("check").WithDescription("Check fonts in MKV.");
            });

            config.AddBranch("extract", extract =>
            {
                // Block: Configure commands that extract streams from media containers.
                extract.SetDescription("Extract media streams.");
                extract.AddCommand<ExtractAudioCommand>("audio").WithDescription("Extract audio track.");
                extract.AddCommand<ExtractSubCommand>("sub").WithDescription("Extract subtitle track.");
                extract.AddCommand<ExtractVideoCommand>("video").WithDescription("Extract video track.");
            });

            config.AddBranch("convert", convert =>
            {
                // Block: Configure commands for subtitle conversion tasks.
                convert.SetDescription("Convert subtitles.");
                convert.AddCommand<ConvertSubCommand>("sub").WithDescription("Convert subtitle file.");
            });

            config.AddBranch("tools", tools =>
            {
                // Block: Configure commands for tool status and cache management.
                tools.SetDescription("Tool provisioning and cache management.");
                tools.AddCommand<ToolsStatusCommand>("status").WithDescription("Show resolved tool paths.");
                tools.AddCommand<ToolsFetchCommand>("fetch").WithDescription("Download and cache tool binaries.");
                tools.AddCommand<ToolsCleanCommand>("clean").WithDescription("Delete extracted tool cache.");
            });

            config.AddBranch("release", release =>
            {
                // Block: Configure release workflow commands.
                release.SetDescription("Release workflows.");
                release.AddCommand<ReleaseMuxCommand>("mux").WithDescription("Release mux workflow for MKV files.");
            });

            config.AddBranch("config", configBranch =>
            {
                // Block: Configure commands that manage Kitsub configuration files.
                configBranch.SetDescription("Configuration management.");
                configBranch.AddCommand<ConfigPathCommand>("path").WithDescription("Show resolved configuration paths.");
                configBranch.AddCommand<ConfigInitCommand>("init").WithDescription("Initialize the default configuration file.");
                configBranch.AddCommand<ConfigShowCommand>("show").WithDescription("Display configuration files.");
            });

            config.AddCommand<DoctorCommand>("doctor").WithDescription("Run diagnostics and tool checks.");
        });

        // Block: Run the command app and return its exit code.
        return await app.RunAsync(args).ConfigureAwait(false);
    }

    private static BootstrapOptions ParseBootstrapOptions(string[] args)
    {
        bool noBanner = false;
        bool noColor = false;

        for (var index = 0; index < args.Length; index++)
        {
            var arg = args[index];
            if (arg.Equals("--no-banner", StringComparison.OrdinalIgnoreCase))
            {
                noBanner = true;
            }
            else if (arg.Equals("--no-color", StringComparison.OrdinalIgnoreCase))
            {
                noColor = true;
            }
        }

        return new BootstrapOptions(noBanner, noColor);
    }

    private static void PrintBanner(IAnsiConsole console)
    {
        const string banner = """
 .'.            ,;     
 ..,c,.       .oKc     
 .  ,k0xxxdooxXNx;.    
   .;kNWMMMMMMMXOx,    
  .lO0KNWMMMMMMMNc     ██ ▄█▀ ▄▄ ▄▄▄▄▄▄ ▄▄▄▄ ▄▄ ▄▄ ▄▄▄▄
.,oocd0XNWMMWKx0Nk;    ████   ██   ██  ███▄▄ ██ ██ ██▄██
..:lc:lkXNKkxod0Ol'    ██ ▀█▄ ██   ██  ▄▄██▀ ▀███▀ ██▄█▀
   .'cdxKXOxkkc..      
      ,kKXXKo.         Burn-In Toolkit.
       :0Xd,.          
        ,:.            
""";
        console.WriteLine(banner);
        console.WriteLine();
    }

    private sealed record BootstrapOptions(bool NoBanner, bool NoColor);
}
