// Summary: Coordinates startup tool provisioning prompts and update checks.
using Kitsub.Tooling.Provisioning;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Kitsub.Tooling;

/// <summary>Options that control tool startup prompts.</summary>
public sealed record ToolsStartupOptions(
    bool StartupPromptEnabled,
    bool AutoUpdate,
    bool UpdatePromptOnStartup,
    int CheckIntervalHours,
    bool ForceUpdateCheck,
    bool NoProvision,
    bool NoStartupPrompt,
    bool IsHelpInvocation);

/// <summary>Runs interactive tool provisioning prompts at startup.</summary>
public sealed class ToolsStartupCoordinator
{
    private const string HashManifestName = "tool-hashes.json";
    private readonly ToolResolver _toolResolver;
    private readonly ToolBundleManager _bundleManager;
    private readonly ToolCachePaths _cachePaths;
    private readonly WindowsRidDetector _ridDetector;
    private readonly StartupStateStore _stateStore;
    private readonly ILogger<ToolsStartupCoordinator> _logger;

    public ToolsStartupCoordinator(
        ToolResolver toolResolver,
        ToolBundleManager bundleManager,
        ToolCachePaths cachePaths,
        WindowsRidDetector ridDetector,
        StartupStateStore stateStore,
        ILogger<ToolsStartupCoordinator> logger)
    {
        _toolResolver = toolResolver;
        _bundleManager = bundleManager;
        _cachePaths = cachePaths;
        _ridDetector = ridDetector;
        _stateStore = stateStore;
        _logger = logger;
    }

    public async Task RunAsync(
        IAnsiConsole console,
        ToolOverrides overrides,
        ToolResolveOptions resolveOptions,
        ToolsStartupOptions options,
        CancellationToken cancellationToken)
    {
        if (options.IsHelpInvocation || options.NoStartupPrompt || !options.StartupPromptEnabled)
        {
            return;
        }

        if (options.NoProvision || !IsInteractive(console))
        {
            return;
        }

        if (!_ridDetector.IsWindows)
        {
            return;
        }

        var rid = _ridDetector.GetRuntimeRid();
        if (!_bundleManager.Manifest.Rids.ContainsKey(rid))
        {
            return;
        }

        var checkOptions = CloneResolveOptions(resolveOptions, allowProvisioning: false);
        var resolution = _toolResolver.Resolve(overrides, checkOptions);
        var baselineMissing = ResolveExecutable("mkvmerge", resolution.Mkvmerge) is null ||
                              ResolveExecutable("ffprobe", resolution.Ffprobe) is null;

        if (baselineMissing)
        {
            console.MarkupLine("[yellow]Required tools not found (mkvmerge, ffprobe).[/]");
            var download = AnsiConsole.Confirm("Download required tools now?", defaultValue: false);
            if (!download)
            {
                return;
            }

            var provisioned = await ProvisionToolsAsync(console, rid, resolveOptions, cancellationToken).ConfigureAwait(false);
            if (!provisioned)
            {
                return;
            }
        }

        if (!options.AutoUpdate || !options.UpdatePromptOnStartup)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var state = _stateStore.Load();
        var intervalHours = options.CheckIntervalHours < 1 ? 24 : options.CheckIntervalHours;
        var interval = TimeSpan.FromHours(intervalHours);
        if (!options.ForceUpdateCheck &&
            state.LastStartupCheckUtc.HasValue &&
            now - state.LastStartupCheckUtc.Value < interval)
        {
            return;
        }

        state = state with { LastStartupCheckUtc = now };
        var installedVersion = TryGetInstalledToolsetVersion(rid, resolveOptions.ToolsCacheDir);
        state = state with { LastInstalledToolsetVersionSeen = installedVersion };
        _stateStore.Save(state);

        if (string.IsNullOrWhiteSpace(installedVersion))
        {
            return;
        }

        var manifestVersion = _bundleManager.Manifest.ToolsetVersion;
        if (string.Equals(installedVersion, manifestVersion, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var updatePrompt = $"Tool updates available ({installedVersion} → {manifestVersion}). Download now?";
        if (!AnsiConsole.Confirm(updatePrompt, defaultValue: false))
        {
            return;
        }

        var updated = await ProvisionToolsAsync(console, rid, resolveOptions, cancellationToken).ConfigureAwait(false);
        if (updated)
        {
            _stateStore.Save(state with { LastInstalledToolsetVersionSeen = manifestVersion });
        }
    }

    private async Task<bool> ProvisionToolsAsync(
        IAnsiConsole console,
        string rid,
        ToolResolveOptions resolveOptions,
        CancellationToken cancellationToken)
    {
        try
        {
            var provisionOptions = CloneResolveOptions(resolveOptions, allowProvisioning: true);
            ToolBundleResult? result = null;
            console.Status()
                .Start("Provisioning tools...", _ =>
                {
                    result = _bundleManager
                        .EnsureCachedToolsetAsync(rid, provisionOptions, cancellationToken, force: false, progress: null)
                        .GetAwaiter()
                        .GetResult();
                });

            if (result is null)
            {
                console.MarkupLine("[red]Failed to provision tools. Check logs for details.[/]");
                return false;
            }

            console.MarkupLine("[green]Tools provisioned.[/]");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to provision tools at startup.");
            console.MarkupLine("[red]Failed to provision tools. Check logs for details.[/]");
            return false;
        }
    }

    private string? TryGetInstalledToolsetVersion(string rid, string? toolsCacheDir)
    {
        try
        {
            var cacheRoot = _cachePaths.GetCacheRoot(toolsCacheDir);
            var ridRoot = Path.Combine(cacheRoot, rid);
            if (!Directory.Exists(ridRoot))
            {
                return null;
            }

            var versionDirs = Directory.GetDirectories(ridRoot);
            var candidates = new List<string>();
            foreach (var dir in versionDirs)
            {
                var hashPath = Path.Combine(dir, HashManifestName);
                if (File.Exists(hashPath))
                {
                    candidates.Add(dir);
                }
            }

            if (candidates.Count != 1)
            {
                return null;
            }

            return new DirectoryInfo(candidates[0]).Name;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to detect installed toolset version.");
            return null;
        }
    }

    private static ToolResolveOptions CloneResolveOptions(ToolResolveOptions options, bool allowProvisioning)
    {
        return new ToolResolveOptions
        {
            AllowProvisioning = allowProvisioning,
            PreferBundled = options.PreferBundled,
            PreferPath = options.PreferPath,
            ToolsCacheDir = options.ToolsCacheDir,
            DryRun = options.DryRun,
            Verbose = options.Verbose
        };
    }

    private static string? ResolveExecutable(string toolName, ToolPathResolution resolution)
    {
        if (resolution.Source != ToolSource.Path)
        {
            return File.Exists(resolution.Path) ? resolution.Path : null;
        }

        return FindOnPath(toolName);
    }

    private static string? FindOnPath(string toolName)
    {
        var pathValue = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(pathValue))
        {
            return null;
        }

        var candidates = new[] { toolName, $"{toolName}.exe" };
        foreach (var path in pathValue.Split(Path.PathSeparator))
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                continue;
            }

            var trimmed = path.Trim().Trim('"');
            foreach (var candidate in candidates)
            {
                var fullPath = Path.Combine(trimmed, candidate);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }
        }

        return null;
    }

    private static bool IsInteractive(IAnsiConsole console)
    {
        var isCi = Environment.GetEnvironmentVariable("CI");
        if (!string.IsNullOrWhiteSpace(isCi) &&
            !string.Equals(isCi, "false", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(isCi, "0", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (Console.IsInputRedirected || Console.IsOutputRedirected || Console.IsErrorRedirected)
        {
            return false;
        }

        return console.Profile.Out.IsTerminal;
    }
}
