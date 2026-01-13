// Summary: Downloads and provisions tool binaries into the cache.
using Kitsub.Tooling.Provisioning;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Kitsub.Cli;

/// <summary>Forces provisioning of tool binaries into the cache.</summary>
public sealed class ToolsFetchCommand : CommandBase<ToolsFetchCommand.Settings>
{
    private readonly ToolBundleManager _bundleManager;
    private readonly WindowsRidDetector _ridDetector;

    /// <summary>Defines command-line settings for fetching tools.</summary>
    public sealed class Settings : ToolSettings
    {
    }

    /// <summary>Initializes the command with the console used for output.</summary>
    /// <param name="console">The console used to render command output.</param>
    public ToolsFetchCommand(
        IAnsiConsole console,
        ToolBundleManager bundleManager,
        WindowsRidDetector ridDetector,
        AppConfigService configService) : base(console, configService)
    {
        _bundleManager = bundleManager;
        _ridDetector = ridDetector;
    }

    protected override async Task<int> ExecuteAsyncCore(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var options = ToolingFactory.BuildResolveOptions(settings);
        var progressMode = settings.Progress ?? UiProgressMode.Auto;
        var result = await SpectreProgressReporter.RunWithProgressAsync(
            Console,
            progressMode,
            progress => _bundleManager.EnsureCachedToolsetAsync(
                _ridDetector.GetRuntimeRid(),
                options,
                cancellationToken,
                force: true,
                progress)).ConfigureAwait(false);

        if (result is null)
        {
            Console.MarkupLine("[red]Failed to provision tools. Check logs for details.[/]");
            return ExitCodes.ProvisioningFailure;
        }

        Console.MarkupLine("[green]Tools provisioned in cache.[/]");
        return ExitCodes.Success;
    }
}
