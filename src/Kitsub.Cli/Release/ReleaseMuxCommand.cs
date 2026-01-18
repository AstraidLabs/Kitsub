// Summary: Implements the release mux workflow for MKV files.
using Kitsub.Core;
using Kitsub.Tooling;
using Kitsub.Tooling.Provisioning;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Kitsub.Cli;

/// <summary>Executes the release mux workflow for MKV containers.</summary>
public sealed class ReleaseMuxCommand : CommandBase<ReleaseMuxSettings>
{
    private readonly ToolResolver _toolResolver;

    /// <summary>Initializes the command with the console used for output.</summary>
    /// <param name="console">The console used to render command output.</param>
    /// <param name="toolResolver">The tool resolver used to locate external tools.</param>
    /// <param name="configService">The configuration service used for defaults.</param>
    public ReleaseMuxCommand(IAnsiConsole console, ToolResolver toolResolver, AppConfigService configService) : base(console, configService)
    {
        _toolResolver = toolResolver;
    }

    protected override async Task<int> ExecuteAsyncCore(CommandContext context, ReleaseMuxSettings settings, CancellationToken cancellationToken)
    {
        var spec = LoadSpec(settings);
        var inputPath = ResolveInputPath(settings, spec);
        var outputPath = ResolveOutputPath(settings, spec, inputPath);
        var fontsDir = ResolveFontsDir(settings, spec);
        var strict = settings.Strict ?? spec?.Strict ?? false;

        ValidateMkvPath(inputPath, "Input");
        EnsureFileExists(inputPath, "Input MKV");
        ValidateMkvPath(outputPath, "Output");
        if (!string.IsNullOrWhiteSpace(fontsDir))
        {
            EnsureDirectoryExists(fontsDir, "Fonts directory");
        }

        var subtitleSpecs = ResolveSubtitleSpecs(settings, spec);
        var subtitles = BuildSubtitleDescriptors(subtitleSpecs, spec is not null);
        ValidateSubtitleFiles(subtitles);

        var outputDir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        if (File.Exists(outputPath) && !settings.Force)
        {
            throw new ValidationException($"Output file already exists. Use --force to overwrite: {outputPath}");
        }

        var warnings = new List<string>();
        var defaultCount = subtitles.Count(subtitle => subtitle.IsDefault == true);
        if (defaultCount > 1)
        {
            warnings.Add("More than one subtitle is marked as default.");
        }

        var fontsToAttach = Array.Empty<string>();
        if (!string.IsNullOrWhiteSpace(fontsDir))
        {
            fontsToAttach = MkvmergeMuxer.EnumerateFonts(fontsDir).ToArray();
            if (fontsToAttach.Length == 0)
            {
                warnings.Add("Fonts directory provided but contains no supported font files.");
            }
        }

        var allowProvisioning = !settings.NoProvision && !settings.DryRun;
        using var tooling = CreateTooling(settings, allowProvisioning);
        var mkvmergePath = ResolveExecutable("mkvmerge", tooling.Paths.Mkvmerge);
        if (mkvmergePath is null && settings.NoProvision)
        {
            Console.MarkupLine("[red]mkvmerge not found. Use --mkvmerge, configure tools.mkvmerge, or run without --no-provision to allow auto-download.[/]");
            return ExitCodes.ValidationError;
        }

        var hasAssInputs = subtitles.Any(subtitle => HasAssExtension(subtitle.FilePath));
        if (settings.DryRun)
        {
            RenderDryRunPlan(tooling, inputPath, outputPath, fontsDir, subtitles, fontsToAttach.Length > 0, hasAssInputs, settings.Force);
            return ExitCodes.Success;
        }

        if (mkvmergePath is null)
        {
            Console.MarkupLine("[red]mkvmerge was not resolved after provisioning.[/]");
            return ExitCodes.ProvisioningFailure;
        }

        var logger = tooling.GetRequiredService<ILogger<ReleaseMuxCommand>>();
        var tempFiles = new List<string>();
        string? outputCandidate = null;
        var fontsAttachedCount = 0;

        try
        {
            await tooling.Service.InspectAsync(inputPath, cancellationToken).ConfigureAwait(false);

            var muxTemp = CreateTempPath(outputPath);
            tempFiles.Add(muxTemp);
            await tooling.Service.MuxSubtitlesAsync(inputPath, subtitles, muxTemp, cancellationToken).ConfigureAwait(false);
            outputCandidate = muxTemp;

            if (!string.IsNullOrWhiteSpace(fontsDir) && fontsToAttach.Length > 0)
            {
                var fontsTemp = CreateTempPath(outputPath);
                tempFiles.Add(fontsTemp);
                await tooling.Service.AttachFontsAsync(outputCandidate, fontsDir, fontsTemp, cancellationToken).ConfigureAwait(false);
                fontsAttachedCount = fontsToAttach.Length;
                outputCandidate = fontsTemp;
            }

            if (hasAssInputs)
            {
                var fontCheck = await tooling.Service.CheckFontsAsync(outputCandidate, cancellationToken).ConfigureAwait(false);
                if (!fontCheck.HasFonts)
                {
                    warnings.Add("ASS/SSA subtitles detected but no font attachments found.");
                }
            }

            WriteOutputFile(outputCandidate, outputPath, settings.Force);
            tempFiles.Remove(outputCandidate);
        }
        finally
        {
            foreach (var tempFile in tempFiles)
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        RenderSummary(inputPath, outputPath, subtitles, fontsAttachedCount, warnings, logger);

        if (warnings.Count > 0 && strict)
        {
            Console.MarkupLine("[red]Strict mode enabled: warnings treated as errors.[/]");
            return ExitCodes.ValidationError;
        }

        return ExitCodes.Success;
    }

    private static ReleaseSpec? LoadSpec(ReleaseMuxSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.SpecPath))
        {
            return null;
        }

        return ReleaseSpec.Load(settings.SpecPath).ResolvePaths(settings.SpecPath);
    }

    private static string ResolveInputPath(ReleaseMuxSettings settings, ReleaseSpec? spec)
    {
        var input = settings.InputMkv;
        if (string.IsNullOrWhiteSpace(input))
        {
            input = spec?.Input;
        }

        if (string.IsNullOrWhiteSpace(input))
        {
            throw new ValidationException("Input MKV is required.");
        }

        return Path.GetFullPath(input);
    }

    private static string ResolveOutputPath(ReleaseMuxSettings settings, ReleaseSpec? spec, string inputPath)
    {
        if (!string.IsNullOrWhiteSpace(settings.Output))
        {
            return Path.GetFullPath(settings.Output);
        }

        if (!string.IsNullOrWhiteSpace(settings.OutputDir))
        {
            var outDir = Path.GetFullPath(settings.OutputDir);
            var name = Path.GetFileNameWithoutExtension(inputPath);
            return Path.Combine(outDir, $"{name}.release.mkv");
        }

        if (!string.IsNullOrWhiteSpace(spec?.Output))
        {
            return Path.GetFullPath(spec.Output);
        }

        var inputDir = Path.GetDirectoryName(inputPath) ?? string.Empty;
        var inputName = Path.GetFileNameWithoutExtension(inputPath);
        return Path.Combine(inputDir, $"{inputName}.release.mkv");
    }

    private static string? ResolveFontsDir(ReleaseMuxSettings settings, ReleaseSpec? spec)
    {
        var fontsDir = settings.FontsDir;
        if (string.IsNullOrWhiteSpace(fontsDir))
        {
            fontsDir = spec?.FontsDir;
        }

        return string.IsNullOrWhiteSpace(fontsDir) ? null : Path.GetFullPath(fontsDir);
    }

    private IReadOnlyList<ReleaseSubtitleSpec> ResolveSubtitleSpecs(ReleaseMuxSettings settings, ReleaseSpec? spec)
    {
        if (spec is not null)
        {
            if (spec.Subtitles.Count == 0)
            {
                throw new ValidationException("Release spec must contain at least one subtitle.");
            }

            return spec.Subtitles;
        }

        if (string.IsNullOrWhiteSpace(settings.SubtitlePath))
        {
            throw new ValidationException("Subtitle file is required.");
        }

        var defaultFlag = settings.Default == true
            ? true
            : settings.NoDefault == true
                ? false
                : EffectiveConfig.Defaults.Mux.DefaultDefaultFlag ?? true;
        var forcedFlag = settings.Forced == true
            ? true
            : settings.NoForced == true
                ? false
                : EffectiveConfig.Defaults.Mux.DefaultForcedFlag ?? false;

        return new[]
        {
            new ReleaseSubtitleSpec
            {
                Path = Path.GetFullPath(settings.SubtitlePath),
                Lang = settings.Language,
                Title = settings.Title,
                Default = defaultFlag,
                Forced = forcedFlag
            }
        };
    }

    private IReadOnlyList<SubtitleDescriptor> BuildSubtitleDescriptors(
        IReadOnlyList<ReleaseSubtitleSpec> subtitles,
        bool fromSpec)
    {
        var defaults = EffectiveConfig.Defaults.Mux;
        var defaultLanguage = defaults.DefaultLanguage;
        var defaultTitle = defaults.DefaultTrackName;
        var defaultDefaultFlag = defaults.DefaultDefaultFlag;
        var defaultForcedFlag = defaults.DefaultForcedFlag;

        return subtitles
            .Select(sub => new SubtitleDescriptor(
                sub.Path,
                sub.Lang ?? defaultLanguage,
                sub.Title ?? defaultTitle,
                fromSpec ? (sub.Default ?? defaultDefaultFlag) : sub.Default,
                fromSpec ? (sub.Forced ?? defaultForcedFlag) : sub.Forced))
            .ToArray();
    }

    private static void ValidateSubtitleFiles(IEnumerable<SubtitleDescriptor> subtitles)
    {
        foreach (var subtitle in subtitles)
        {
            EnsureFileExists(subtitle.FilePath, "Subtitle file");
        }
    }

    private static void ValidateMkvPath(string path, string label)
    {
        if (!Path.GetExtension(path).Equals(".mkv", StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException($"{label} must be an MKV file: {path}");
        }
    }

    private static void EnsureFileExists(string path, string label)
    {
        if (!File.Exists(path))
        {
            throw new ValidationException($"{label} not found: {path}");
        }
    }

    private static void EnsureDirectoryExists(string path, string label)
    {
        if (!Directory.Exists(path))
        {
            throw new ValidationException($"{label} not found: {path}");
        }
    }

    private static string CreateTempPath(string outputPath)
    {
        var outputDir = Path.GetDirectoryName(outputPath) ?? Environment.CurrentDirectory;
        var name = Path.GetFileNameWithoutExtension(outputPath);
        var tempFile = $"{name}.{Guid.NewGuid():N}.tmp.mkv";
        return Path.Combine(outputDir, tempFile);
    }

    private static void WriteOutputFile(string tempPath, string outputPath, bool force)
    {
        if (!File.Exists(tempPath))
        {
            throw new ValidationException("Release output was not generated.");
        }

        if (File.Exists(outputPath))
        {
            if (!force)
            {
                throw new ValidationException($"Output file already exists. Use --force to overwrite: {outputPath}");
            }

            File.Replace(tempPath, outputPath, null, ignoreMetadataErrors: true);
        }
        else
        {
            File.Move(tempPath, outputPath);
        }
    }

    private void RenderDryRunPlan(
        ToolingContext tooling,
        string inputPath,
        string outputPath,
        string? fontsDir,
        IReadOnlyList<SubtitleDescriptor> subtitles,
        bool willAttachFonts,
        bool hasAssInputs,
        bool force)
    {
        var muxTemp = CreateTempPath(outputPath);
        var attachTemp = willAttachFonts ? CreateTempPath(outputPath) : null;

        Console.MarkupLine("[yellow]Plan:[/]");
        var step = 1;
        Console.MarkupLine($"[grey]  {step++}) Inspect input MKV[/]");
        Console.MarkupLine($"[grey]  {step++}) Mux subtitles into release MKV[/]");
        if (willAttachFonts)
        {
            Console.MarkupLine($"[grey]  {step++}) Attach fonts[/]");
        }

        if (hasAssInputs)
        {
            Console.MarkupLine($"[grey]  {step++}) Verify fonts[/]");
        }

        Console.MarkupLine($"[grey]  {step}) Move output to {Markup.Escape(outputPath)}[/]");

        var mkvmerge = tooling.GetRequiredService<MkvmergeClient>();
        var muxer = tooling.GetRequiredService<MkvmergeMuxer>();

        Console.MarkupLine("[yellow]Commands:[/]");
        Console.MarkupLine($"[grey]{Markup.Escape(mkvmerge.BuildIdentifyCommand(inputPath).Rendered)}[/]");
        Console.MarkupLine($"[grey]{Markup.Escape(muxer.BuildMuxSubtitlesCommand(inputPath, subtitles, muxTemp).Rendered)}[/]");

        if (willAttachFonts && attachTemp is not null && !string.IsNullOrWhiteSpace(fontsDir))
        {
            Console.MarkupLine($"[grey]{Markup.Escape(muxer.BuildAttachFontsCommand(muxTemp, fontsDir, attachTemp).Rendered)}[/]");
        }

        if (hasAssInputs)
        {
            var verifyTarget = attachTemp ?? muxTemp;
            Console.MarkupLine($"[grey]{Markup.Escape(mkvmerge.BuildIdentifyCommand(verifyTarget).Rendered)}[/]");
        }

        if (force)
        {
            Console.MarkupLine("[grey]Output will overwrite existing file due to --force.[/]");
        }
    }

    private void RenderSummary(
        string inputPath,
        string outputPath,
        IReadOnlyList<SubtitleDescriptor> subtitles,
        int fontsAttachedCount,
        IReadOnlyList<string> warnings,
        ILogger<ReleaseMuxCommand> logger)
    {
        Console.MarkupLine("[green]Release mux complete.[/]");
        Console.MarkupLine($"[grey]Input:[/] {Markup.Escape(inputPath)}");
        Console.MarkupLine($"[grey]Output:[/] {Markup.Escape(outputPath)}");
        Console.MarkupLine($"[grey]Subtitles:[/] {subtitles.Count}");

        foreach (var subtitle in subtitles)
        {
            var fileName = Path.GetFileName(subtitle.FilePath);
            var line = $"  - {fileName}";
            if (!string.IsNullOrWhiteSpace(subtitle.Language))
            {
                line += $" | lang={subtitle.Language}";
            }

            if (!string.IsNullOrWhiteSpace(subtitle.Title))
            {
                line += $" | title=\"{subtitle.Title}\"";
            }

            if (subtitle.IsDefault.HasValue)
            {
                line += $" | default={subtitle.IsDefault.Value.ToString().ToLowerInvariant()}";
            }

            if (subtitle.IsForced.HasValue)
            {
                line += $" | forced={subtitle.IsForced.Value.ToString().ToLowerInvariant()}";
            }

            Console.MarkupLine(Markup.Escape(line));
        }

        Console.MarkupLine($"[grey]Fonts attached:[/] {fontsAttachedCount}");

        if (warnings.Count > 0)
        {
            Console.MarkupLine("[yellow]Warnings:[/]");
            foreach (var warning in warnings)
            {
                Console.MarkupLine($"[yellow]- {Markup.Escape(warning)}[/]");
            }
        }

        logger.LogInformation("Release mux summary: Input={Input} Output={Output} Subtitles={SubtitleCount} FontsAttached={FontsAttached}",
            inputPath,
            outputPath,
            subtitles.Count,
            fontsAttachedCount);

        foreach (var subtitle in subtitles)
        {
            logger.LogInformation("Release subtitle: File={File} Lang={Lang} Title={Title} Default={Default} Forced={Forced}",
                subtitle.FilePath,
                subtitle.Language,
                subtitle.Title,
                subtitle.IsDefault,
                subtitle.IsForced);
        }

        foreach (var warning in warnings)
        {
            logger.LogWarning("Release mux warning: {Warning}", warning);
        }
    }

    private ToolingContext CreateTooling(ReleaseMuxSettings settings, bool allowProvisioning)
    {
        var overrides = ToolingFactory.BuildToolOverrides(settings);
        var resolverOptions = ToolingFactory.BuildResolveOptions(settings, allowProvisioning);
        var options = ToolingFactory.BuildRunOptions(settings, Console);
        var logger = ToolingFactory.CreateLogger(settings);
        var progressMode = settings.Progress ?? UiProgressMode.Auto;

        var resolved = SpectreProgressReporter.RunWithProgress(
            Console,
            progressMode,
            progress => _toolResolver.Resolve(overrides, resolverOptions, progress));

        var services = new ServiceCollection();
        services.AddSingleton(options);
        services.AddSingleton(resolved);
        services.AddSingleton<IExternalToolRunner, ExternalToolRunner>();
        services.AddSingleton<FfprobeClient>();
        services.AddSingleton<MkvmergeClient>();
        services.AddSingleton<MkvmergeMuxer>();
        services.AddSingleton<MkvpropeditClient>();
        services.AddSingleton<FfmpegClient>();
        services.AddSingleton<KitsubService>();
        services.AddLogging(builder => builder.AddSerilog(logger, dispose: true));

        var provider = services.BuildServiceProvider();
        return new ToolingContext(provider, resolved, options);
    }

    private static bool HasAssExtension(string path)
    {
        var extension = Path.GetExtension(path);
        return extension.Equals(".ass", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".ssa", StringComparison.OrdinalIgnoreCase);
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
}
