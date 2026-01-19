// Summary: Clears the extracted tools cache directory.
using Kitsub.Tooling.Provisioning;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Kitsub.Cli;

/// <summary>Deletes cached extracted tools.</summary>
public sealed class ToolsCleanCommand : CommandBase<ToolsCleanCommand.Settings>
{
    private readonly ToolBundleManager _bundleManager;
    private readonly WindowsRidDetector _ridDetector;

    /// <summary>Defines command-line settings for cleaning tool caches.</summary>
    public sealed class Settings : ToolSettings
    {
        [CommandOption("--yes")]
        /// <summary>Gets a value indicating whether cache deletion is confirmed.</summary>
        public bool Confirm { get; init; }
    }

    /// <summary>Initializes the command with the console used for output.</summary>
    /// <param name="console">The console used to render command output.</param>
    public ToolsCleanCommand(
        IAnsiConsole console,
        ToolResolver toolResolver,
        ToolBundleManager bundleManager,
        WindowsRidDetector ridDetector,
        AppConfigService configService) : base(console, configService, toolResolver, bundleManager, ridDetector)
    {
        _bundleManager = bundleManager;
        _ridDetector = ridDetector;
    }

    protected override Task<int> ExecuteAsyncCore(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        if (!settings.Confirm)
        {
            Console.MarkupLine("[red]Refusing to delete cache without --yes.[/]");
            return Task.FromResult(1);
        }

        _bundleManager.CleanCache(_ridDetector.GetRuntimeRid(), settings.ToolsCacheDir);

        Console.MarkupLine("[green]Tools cache cleared.[/]");
        return Task.FromResult(0);
    }
}
