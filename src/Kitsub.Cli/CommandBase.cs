// Summary: Provides a reusable base for CLI commands with shared error handling and console output.
using Kitsub.Tooling;
using Kitsub.Tooling.Provisioning;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Kitsub.Cli;

/// <summary>Defines a base CLI command with shared validation and error handling logic.</summary>
/// <typeparam name="TSettings">The settings type for the command.</typeparam>
public abstract class CommandBase<TSettings> : AsyncCommand<TSettings> where TSettings : ToolSettings
{
    private readonly IAnsiConsole _console;
    private readonly AppConfigService _configService;
    private readonly ToolResolver _toolResolver;
    private readonly ToolBundleManager _bundleManager;
    private readonly WindowsRidDetector _ridDetector;

    protected CommandBase(
        IAnsiConsole console,
        AppConfigService configService,
        ToolResolver toolResolver,
        ToolBundleManager bundleManager,
        WindowsRidDetector ridDetector)
    {
        // Block: Capture the console abstraction used for user-facing output.
        _console = console;
        _configService = configService;
        _toolResolver = toolResolver;
        _bundleManager = bundleManager;
        _ridDetector = ridDetector;
    }

    /// <summary>Runs the command with standardized validation and exception handling.</summary>
    /// <param name="context">The CLI command context.</param>
    /// <param name="settings">The parsed settings for the command.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The process exit code for the command.</returns>
    /// <exception cref="ExternalToolException">Thrown when external tooling fails during execution.</exception>
    public sealed override async Task<int> ExecuteAsync(CommandContext context, TSettings settings, CancellationToken cancellationToken)
    {
        // Block: Wrap execution to enforce consistent validation and error reporting.
        try
        {
            // Block: Validate logging settings before invoking the core command logic.
            EffectiveConfig = _configService.LoadEffectiveConfig();
            ToolSettingsApplier.Apply(settings, EffectiveConfig);
            ToolingFactory.ValidateLogging(settings);
            var preflight = await EnsureRequiredToolsAsync(context, settings, cancellationToken).ConfigureAwait(false);
            if (preflight.HasValue)
            {
                return preflight.Value;
            }

            return await ExecuteAsyncCore(context, settings, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Block: Handle failures using standardized error handling and exit codes.
            return CommandErrorHandler.Handle(ex, _console, settings.Verbose);
        }
    }

    protected IAnsiConsole Console => _console;

    protected AppConfig EffectiveConfig { get; private set; } = AppConfigDefaults.CreateDefaults();

    protected AppConfigService ConfigService => _configService;

    protected virtual ToolRequirement GetToolRequirement(TSettings settings) => ToolRequirement.None;

    protected abstract Task<int> ExecuteAsyncCore(CommandContext context, TSettings settings, CancellationToken cancellationToken);

    private async Task<int?> EnsureRequiredToolsAsync(CommandContext context, TSettings settings, CancellationToken cancellationToken)
    {
        var requirement = GetToolRequirement(settings);
        if (requirement.RequiredTools.Count == 0)
        {
            return null;
        }

        var missing = GetMissingTools(settings, requirement);
        if (missing.Count == 0)
        {
            return null;
        }

        Console.MarkupLine($"[red]Command unavailable. Missing required tools: {Markup.Escape(string.Join(", ", missing.Select(FormatToolName)))}.[/]");

        if (settings.NoProvision)
        {
            RenderProvisioningInstructions();
            return ExitCodes.ValidationError;
        }

        if (settings.AssumeYes)
        {
            return await ProvisionRequiredToolsAsync(settings, requirement, cancellationToken).ConfigureAwait(false);
        }

        if (!IsInteractive() || EffectiveConfig.Tools.CommandPromptOnMissing == false)
        {
            RenderProvisioningInstructions();
            return ExitCodes.ValidationError;
        }

        var prompt = AnsiConsole.Confirm("Download required tools now?", defaultValue: false);
        if (!prompt)
        {
            RenderProvisioningInstructions();
            return ExitCodes.ValidationError;
        }

        return await ProvisionRequiredToolsAsync(settings, requirement, cancellationToken).ConfigureAwait(false);

    }

    private async Task<int?> ProvisionRequiredToolsAsync(
        TSettings settings,
        ToolRequirement requirement,
        CancellationToken cancellationToken)
    {

        if (!_ridDetector.IsWindows)
        {
            Console.MarkupLine("[red]Tool provisioning is only available on Windows. Configure tool paths or install tools on PATH.[/]");
            return ExitCodes.ValidationError;
        }

        var rid = _ridDetector.GetRuntimeRid();
        if (!_bundleManager.Manifest.Rids.ContainsKey(rid))
        {
            Console.MarkupLine("[red]Tool provisioning is not available for this platform.[/]");
            return ExitCodes.ValidationError;
        }

        var resolveOptions = ToolingFactory.BuildResolveOptions(settings, allowProvisioning: true);
        var progressMode = settings.Progress ?? UiProgressMode.Auto;
        var provisioned = await SpectreProgressReporter.RunWithProgressAsync(
            Console,
            progressMode,
            progress => _bundleManager.EnsureCachedToolsetAsync(rid, resolveOptions, cancellationToken, force: false, progress))
            .ConfigureAwait(false);

        if (provisioned is null)
        {
            Console.MarkupLine("[red]Failed to provision tools. Check logs for details.[/]");
            return ExitCodes.ProvisioningFailure;
        }

        var missing = GetMissingTools(settings, requirement);
        if (missing.Count > 0)
        {
            Console.MarkupLine("[red]Tool provisioning completed but required tools are still missing.[/]");
            RenderProvisioningInstructions();
            return ExitCodes.ProvisioningFailure;
        }

        return null;
    }

    private IReadOnlyList<ToolKind> GetMissingTools(TSettings settings, ToolRequirement requirement)
    {
        var overrides = ToolingFactory.BuildToolOverrides(settings);
        var resolveOptions = ToolingFactory.BuildResolveOptions(settings, allowProvisioning: false);
        var resolved = _toolResolver.Resolve(overrides, resolveOptions);
        var missing = new List<ToolKind>();

        foreach (var tool in requirement.RequiredTools)
        {
            var resolution = ResolveToolPath(resolved, tool);
            if (ResolveExecutable(GetToolName(tool), resolution) is null)
            {
                missing.Add(tool);
            }
        }

        return missing;
    }

    private static ToolPathResolution ResolveToolPath(ToolResolution resolution, ToolKind tool)
    {
        return tool switch
        {
            ToolKind.Ffmpeg => resolution.Ffmpeg,
            ToolKind.Ffprobe => resolution.Ffprobe,
            ToolKind.Mkvmerge => resolution.Mkvmerge,
            ToolKind.Mkvpropedit => resolution.Mkvpropedit,
            ToolKind.Mediainfo => resolution.Mediainfo,
            _ => throw new InvalidOperationException($"Unknown tool requirement: {tool}")
        };
    }

    private static string GetToolName(ToolKind tool)
    {
        return tool switch
        {
            ToolKind.Ffmpeg => "ffmpeg",
            ToolKind.Ffprobe => "ffprobe",
            ToolKind.Mkvmerge => "mkvmerge",
            ToolKind.Mkvpropedit => "mkvpropedit",
            ToolKind.Mediainfo => "mediainfo",
            _ => "tool"
        };
    }

    private static string FormatToolName(ToolKind tool) => GetToolName(tool);

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

    private static bool IsInteractive()
    {
        if (Console.IsInputRedirected || Console.IsOutputRedirected)
        {
            return false;
        }

        return Environment.UserInteractive;
    }

    private void RenderProvisioningInstructions()
    {
        Console.MarkupLine("Install required tools or run [bold]kitsub tools fetch[/] to provision bundled tools.");
        Console.MarkupLine("You can also configure explicit tool paths in kitsub.json.");
    }
}

public enum ToolKind
{
    Ffmpeg,
    Ffprobe,
    Mkvmerge,
    Mkvpropedit,
    Mediainfo
}

public sealed record ToolRequirement(IReadOnlyList<ToolKind> RequiredTools)
{
    public static ToolRequirement None { get; } = new(Array.Empty<ToolKind>());

    public static ToolRequirement For(params ToolKind[] tools)
    {
        return tools.Length == 0 ? None : new ToolRequirement(tools);
    }
}
