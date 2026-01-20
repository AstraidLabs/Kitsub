// Summary: Implements the CLI command that inspects media files and renders track metadata.
using Kitsub.Core;
using Kitsub.Tooling;
using Kitsub.Tooling.Provisioning;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Kitsub.Cli;

/// <summary>Executes media inspection and renders track metadata to the console.</summary>
public sealed class InspectCommand : CommandBase<InspectCommand.Settings>
{
    private readonly ToolResolver _toolResolver;
    private readonly ToolBundleManager _bundleManager;
    private readonly ToolCachePaths _cachePaths;
    private readonly WindowsRidDetector _ridDetector;

    /// <summary>Defines command-line settings for media inspection.</summary>
    public sealed class Settings : ToolSettings
    {
        [CommandArgument(0, "<TARGET>")]
        /// <summary>Gets the inspection target or mode.</summary>
        public string Target { get; init; } = string.Empty;

        [CommandArgument(1, "[FILE]")]
        /// <summary>Gets the path to the media file when using a specialized inspection mode.</summary>
        public string? FilePath { get; init; }

        /// <summary>Validates the provided settings for media inspection.</summary>
        /// <returns>A validation result indicating success or failure.</returns>
        public override ValidationResult Validate()
        {
            // Block: Validate the inspection target and any required file paths.
            if (string.IsNullOrWhiteSpace(Target))
            {
                return ValidationResult.Error("Inspection target is required.");
            }

            if (Target.Equals("mediainfo", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(FilePath))
                {
                    return ValidationResult.Error("MediaInfo inspection requires a file path.");
                }

                return ValidationHelpers.ValidateFileExists(FilePath, "Input file");
            }

            if (!string.IsNullOrWhiteSpace(FilePath))
            {
                return ValidationResult.Error("Unexpected extra argument. Use `kitsub inspect mediainfo <file>` for MediaInfo mode.");
            }

            return ValidationHelpers.ValidateFileExists(Target, "Input file");
        }
    }

    /// <summary>Initializes the command with the console used for output.</summary>
    /// <param name="console">The console used to render command output.</param>
    public InspectCommand(
        IAnsiConsole console,
        ToolResolver toolResolver,
        ToolBundleManager bundleManager,
        ToolCachePaths cachePaths,
        WindowsRidDetector ridDetector,
        AppConfigService configService) : base(console, configService, toolResolver, bundleManager, ridDetector)
    {
        // Block: Delegate console handling to the base command class.
        _toolResolver = toolResolver;
        _bundleManager = bundleManager;
        _cachePaths = cachePaths;
        _ridDetector = ridDetector;
    }

    protected override async Task<int> ExecuteAsyncCore(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        if (settings.Target.Equals("mediainfo", StringComparison.OrdinalIgnoreCase))
        {
            return await ExecuteMediaInfoAsync(settings, cancellationToken).ConfigureAwait(false);
        }

        var filePath = settings.Target;

        // Block: Create tooling services scoped to this command execution.
        using var tooling = ToolingFactory.CreateTooling(settings, Console, _toolResolver);
        if (settings.DryRun)
        {
            if (Path.GetExtension(filePath).Equals(".mkv", StringComparison.OrdinalIgnoreCase))
            {
                // Block: Render the MKV identify command without executing it.
                var mkvmerge = tooling.GetRequiredService<MkvmergeClient>();
                Console.MarkupLine($"[grey]{Markup.Escape(mkvmerge.BuildIdentifyCommand(filePath).Rendered)}[/]");
            }
            else
            {
                // Block: Render the ffprobe command without executing it.
                var ffprobe = tooling.GetRequiredService<FfprobeClient>();
                Console.MarkupLine($"[grey]{Markup.Escape(ffprobe.BuildProbeCommand(filePath).Rendered)}[/]");
            }

            return 0;
        }

        // Block: Inspect the media file and gather metadata for rendering.
        var service = tooling.Service;
        var result = await service.InspectAsync(filePath, cancellationToken).ConfigureAwait(false);
        var info = result.Info;
        var isMkv = result.IsMkv;

        // Block: Render track information for the inspected media file.
        RenderTracks(info);
        if (isMkv && info.Attachments.Count > 0)
        {
            // Block: Render attachments only for MKV files that contain attachments.
            RenderAttachments(info.Attachments);
        }

        return 0;
    }

    protected override ToolRequirement GetToolRequirement(Settings settings)
    {
        if (settings.Target.Equals("mediainfo", StringComparison.OrdinalIgnoreCase))
        {
            return ToolRequirement.None;
        }

        var filePath = settings.Target;
        return Path.GetExtension(filePath).Equals(".mkv", StringComparison.OrdinalIgnoreCase)
            ? ToolRequirement.For(ToolKind.Mkvmerge)
            : ToolRequirement.For(ToolKind.Ffprobe);
    }

    private async Task<int> ExecuteMediaInfoAsync(Settings settings, CancellationToken cancellationToken)
    {
        var filePath = settings.FilePath!;
        var overrides = ToolingFactory.BuildToolOverrides(settings);
        var progressMode = settings.Progress ?? UiProgressMode.Auto;
        var resolveOptions = ToolingFactory.BuildResolveOptions(settings, allowProvisioning: false);

        var resolved = SpectreProgressReporter.RunWithProgress(
            Console,
            progressMode,
            progress => _toolResolver.Resolve(overrides, resolveOptions, progress));

        var mediainfoPath = ResolveExecutable("mediainfo", resolved.Mediainfo);
        if (mediainfoPath is null)
        {
            if (settings.DryRun)
            {
                RenderDryRunProvisioning(settings);
                return ExitCodes.Success;
            }

            if (settings.NoProvision)
            {
                Console.MarkupLine("[red]MediaInfo not found. Use --mediainfo or configure tools.mediainfo, or run without --no-provision to auto-download.[/]");
                return ExitCodes.ValidationError;
            }

            if (!settings.AssumeYes)
            {
                if (!IsInteractive())
                {
                    Console.MarkupLine("[red]MediaInfo not found. Use --mediainfo or configure tools.mediainfo, or rerun with --assume-yes to auto-download.[/]");
                    return ExitCodes.ValidationError;
                }

                var download = AnsiConsole.Confirm("Download MediaInfo now?", defaultValue: false);
                if (!download)
                {
                    Console.MarkupLine("[red]MediaInfo not found. Use --mediainfo or configure tools.mediainfo, or rerun with --assume-yes to auto-download.[/]");
                    return ExitCodes.ValidationError;
                }
            }

            var provisionOptions = ToolingFactory.BuildResolveOptions(settings, allowProvisioning: true);
            var provisionResult = await SpectreProgressReporter.RunWithProgressAsync(
                Console,
                progressMode,
                progress => _bundleManager.EnsureCachedToolsetAsync(
                    _ridDetector.GetRuntimeRid(),
                    provisionOptions,
                    cancellationToken,
                    force: false,
                    progress)).ConfigureAwait(false);

            if (provisionResult is null)
            {
                Console.MarkupLine("[red]Failed to provision MediaInfo. Check logs for details.[/]");
                return ExitCodes.ProvisioningFailure;
            }

            resolved = _toolResolver.Resolve(overrides, resolveOptions);
            mediainfoPath = ResolveExecutable("mediainfo", resolved.Mediainfo);
            if (mediainfoPath is null)
            {
                Console.MarkupLine("[red]MediaInfo provisioning completed but executable was not found.[/]");
                return ExitCodes.ProvisioningFailure;
            }
        }

        var reportPath = BuildMediaInfoReportPath();
        var args = new[] { "--Output=JSON", filePath };
        var runOptions = ToolingFactory.BuildRunOptions(settings, Console);
        var logger = ToolingFactory.CreateLogger(settings);

        if (settings.DryRun)
        {
            Console.MarkupLine($"[grey]{Markup.Escape(ExternalToolRunner.RenderCommandLine(mediainfoPath, args))}[/]");
            Console.MarkupLine($"[grey]Report: {Markup.Escape(reportPath)}[/]");
            return ExitCodes.Success;
        }

        var services = new ServiceCollection();
        services.AddSingleton(runOptions);
        services.AddSingleton<IExternalToolRunner, ExternalToolRunner>();
        services.AddLogging(builder => builder.AddSerilog(logger, dispose: true));
        using var provider = services.BuildServiceProvider();

        var runner = provider.GetRequiredService<IExternalToolRunner>();
        var result = await runner.CaptureAsync(mediainfoPath, args, runOptions, cancellationToken).ConfigureAwait(false);
        if (result.ExitCode != 0)
        {
            throw new ExternalToolException("mediainfo failed.", result);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
        await File.WriteAllTextAsync(reportPath, result.StandardOutput, cancellationToken).ConfigureAwait(false);

        Console.MarkupLine($"[green]MediaInfo report saved:[/] {Markup.Escape(reportPath)}");
        return ExitCodes.Success;
    }

    private string BuildMediaInfoReportPath()
    {
        var timestamp = DateTimeOffset.Now;
        var reportRoot = Path.Combine(Environment.CurrentDirectory, "reports", "mediainfo", timestamp.ToString("yyyyMMdd"));
        var fileName = $"mediainfo_{timestamp:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}.json";
        return Path.Combine(reportRoot, fileName);
    }

    private void RenderDryRunProvisioning(Settings settings)
    {
        var rid = _ridDetector.GetRuntimeRid();
        var manifest = _bundleManager.Manifest;
        if (!manifest.Rids.TryGetValue(rid, out var ridEntry) || ridEntry.Mediainfo is null)
        {
            Console.MarkupLine("[yellow]MediaInfo provisioning is not available for this platform.[/]");
            return;
        }

        var toolsetRoot = _cachePaths.GetToolsetRoot(rid, manifest.ToolsetVersion, settings.ToolsCacheDir);
        var expectedPath = Path.Combine(toolsetRoot, "mediainfo");

        Console.MarkupLine("[yellow]MediaInfo not found.[/]");
        Console.MarkupLine("[grey]Dry-run: provisioning is disabled.[/]");
        Console.MarkupLine($"[grey]Would download:[/] {Markup.Escape(ridEntry.Mediainfo.ArchiveUrl)}");
        Console.MarkupLine($"[grey]Would extract to:[/] {Markup.Escape(expectedPath)}");
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

    private static bool IsInteractive()
    {
        if (System.Console.IsInputRedirected || System.Console.IsOutputRedirected)
        {
            return false;
        }

        return Environment.UserInteractive;
    }

    private void RenderTracks(MediaInfo info)
    {
        // Block: Build the table layout used to render track metadata.
        var table = new Table().RoundedBorder();
        table.AddColumn("Type");
        table.AddColumn("Index/Id");
        table.AddColumn("Codec");
        table.AddColumn("Language");
        table.AddColumn("Title");
        table.AddColumn("Default");
        table.AddColumn("Forced");
        table.AddColumn("Extra");

        foreach (var track in info.Tracks)
        {
            // Block: Add a row for each track with formatted metadata.
            table.AddRow(
                track.Type.ToString(),
                track.Id?.ToString() ?? track.Index.ToString(),
                track.Codec,
                track.Language ?? "-",
                track.Title ?? "-",
                track.IsDefault ? "yes" : "no",
                track.IsForced ? "yes" : "no",
                FormatExtra(track));
        }

        // Block: Render the populated track table to the console.
        Console.Write(table);
    }

    private void RenderAttachments(IReadOnlyList<AttachmentInfo> attachments)
    {
        // Block: Build the table layout used to render attachment metadata.
        var table = new Table().RoundedBorder();
        table.AddColumn("File");
        table.AddColumn("Mime");
        table.AddColumn("Size");

        foreach (var attachment in attachments)
        {
            // Block: Add a row for each attachment with its key properties.
            table.AddRow(attachment.FileName, attachment.MimeType, attachment.SizeBytes.ToString());
        }

        // Block: Render a heading and the populated attachment table.
        Console.MarkupLine("\n[bold]Attachments[/]");
        Console.Write(table);
    }

    private static string FormatExtra(TrackInfo track)
    {
        if (track.Extra.Count == 0)
        {
            // Block: Return a placeholder when no extra metadata exists.
            return "-";
        }

        // Block: Format extra key-value metadata into a comma-separated string.
        return string.Join(", ", track.Extra.Select(pair => $"{pair.Key}={pair.Value}"));
    }
}
