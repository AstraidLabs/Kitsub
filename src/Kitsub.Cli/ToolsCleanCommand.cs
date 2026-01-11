// Summary: Clears the extracted tools cache directory.
using Kitsub.Tooling.Bundling;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Kitsub.Cli;

/// <summary>Deletes cached extracted tools.</summary>
public sealed class ToolsCleanCommand : CommandBase<ToolsCleanCommand.Settings>
{
    /// <summary>Defines command-line settings for cleaning tool caches.</summary>
    public sealed class Settings : ToolSettings
    {
        [CommandOption("--yes")]
        /// <summary>Gets a value indicating whether cache deletion is confirmed.</summary>
        public bool Confirm { get; init; }
    }

    /// <summary>Initializes the command with the console used for output.</summary>
    /// <param name="console">The console used to render command output.</param>
    public ToolsCleanCommand(IAnsiConsole console) : base(console)
    {
    }

    protected override Task<int> ExecuteAsyncCore(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        if (!settings.Confirm)
        {
            Console.MarkupLine("[red]Refusing to delete cache without --yes.[/]");
            return Task.FromResult(1);
        }

        var logger = ToolingFactory.CreateLogger(settings);
        var services = new ServiceCollection();
        services.AddSingleton(new ToolResolverOptions
        {
            PreferBundled = settings.PreferBundled,
            PreferPath = settings.PreferPath,
            ToolsCacheDirectory = settings.ToolsCacheDir
        });
        services.AddSingleton<ToolManifestLoader>();
        services.AddSingleton<ToolBundleManager>();
        services.AddLogging(builder => builder.AddSerilog(logger, dispose: true));

        using var provider = services.BuildServiceProvider();
        var bundleManager = provider.GetRequiredService<ToolBundleManager>();
        bundleManager.CleanCache();

        Console.MarkupLine("[green]Tools cache cleared.[/]");
        return Task.FromResult(0);
    }
}
