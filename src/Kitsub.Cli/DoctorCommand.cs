// Summary: Implements the diagnostic doctor command for Kitsub.
using Kitsub.Tooling;
using Kitsub.Tooling.Provisioning;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Kitsub.Cli;

/// <summary>Runs diagnostics to verify configuration and tooling health.</summary>
public sealed class DoctorCommand : CommandBase<DoctorCommand.Settings>
{
    private readonly ToolResolver _toolResolver;

    /// <summary>Defines command-line settings for the doctor command.</summary>
    public sealed class Settings : ToolSettings
    {
    }

    public DoctorCommand(IAnsiConsole console, ToolResolver toolResolver, AppConfigService configService) : base(console, configService)
    {
        _toolResolver = toolResolver;
    }

    protected override async Task<int> ExecuteAsyncCore(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var configStatus = ConfigService.GetGlobalStatus();

        Console.MarkupLine("[bold]Kitsub Doctor[/]");
        Console.MarkupLine(string.Empty);
        RenderConfigStatus(configStatus);

        using var tooling = ToolingFactory.CreateTooling(settings, Console, _toolResolver);
        RenderToolStatus(tooling.Paths);

        var requiredMissing = false;
        var nextSteps = new List<string>();

        var ffmpegPath = ResolveExecutable("ffmpeg", tooling.Paths.Ffmpeg);
        if (ffmpegPath is null)
        {
            Console.MarkupLine("[yellow]ffmpeg not found; skipping version check.[/]");
            requiredMissing = true;
            nextSteps.Add("Install ffmpeg or configure tools.ffmpeg in kitsub.json.");
        }
        else
        {
            await RunVersionCheckAsync("ffmpeg", ffmpegPath, new[] { "-version" }, tooling, settings, cancellationToken)
                .ConfigureAwait(false);
        }

        var mkvmergePath = ResolveExecutable("mkvmerge", tooling.Paths.Mkvmerge);
        if (mkvmergePath is null)
        {
            Console.MarkupLine("[yellow]mkvmerge not found; skipping version check.[/]");
            requiredMissing = true;
            nextSteps.Add("Install mkvmerge or configure tools.mkvmerge in kitsub.json.");
        }
        else
        {
            await RunVersionCheckAsync("mkvmerge", mkvmergePath, new[] { "-V" }, tooling, settings, cancellationToken)
                .ConfigureAwait(false);
        }

        if (!configStatus.Found)
        {
            nextSteps.Add("Run `kitsub config init` to create a baseline configuration file.");
        }
        else if (!configStatus.Valid)
        {
            nextSteps.Add("Fix the global config or restore from the .bak file.");
        }

        if (requiredMissing)
        {
            nextSteps.Add("Run `kitsub tools fetch` to provision bundled tools into the cache.");
        }

        Console.MarkupLine(string.Empty);
        Console.MarkupLine("[bold]Checklist Summary[/]");
        Console.MarkupLine($"[bold]Config[/]: {(configStatus.Found ? (configStatus.Valid ? "Valid" : "Invalid") : "Missing")}");
        Console.MarkupLine($"[bold]Required tools[/]: {(requiredMissing ? "Missing" : "OK")}");

        if (nextSteps.Count > 0)
        {
            Console.MarkupLine(string.Empty);
            Console.MarkupLine("[bold]Next steps[/]");
            foreach (var step in nextSteps.Distinct())
            {
                Console.MarkupLine($"- {Markup.Escape(step)}");
            }
        }

        return requiredMissing || !configStatus.Valid ? ExitCodes.ValidationError : ExitCodes.Success;
    }

    private void RenderConfigStatus(ConfigStatus status)
    {
        if (!status.Found)
        {
            Console.MarkupLine($"[yellow]Config[/]: Missing ({Markup.Escape(status.Path)})");
            return;
        }

        if (status.Valid)
        {
            Console.MarkupLine($"[green]Config[/]: Found and valid ({Markup.Escape(status.Path)})");
        }
        else
        {
            Console.MarkupLine($"[red]Config[/]: Invalid ({Markup.Escape(status.Path)})");
            if (!string.IsNullOrWhiteSpace(status.Error))
            {
                Console.MarkupLine($"[grey]{Markup.Escape(status.Error)}[/]");
            }
        }
    }

    private void RenderToolStatus(ToolResolution paths)
    {
        Console.MarkupLine(string.Empty);
        Console.MarkupLine($"[bold]RID[/]: {Markup.Escape(paths.RuntimeRid)}");
        Console.MarkupLine($"[bold]Toolset version[/]: {Markup.Escape(paths.ToolsetVersion)}");

        var table = new Table().RoundedBorder();
        table.AddColumn("Tool");
        table.AddColumn("Path");
        table.AddColumn("Source");
        table.AddColumn("Exists");

        AddRow(table, "ffmpeg", paths.Ffmpeg);
        AddRow(table, "ffprobe", paths.Ffprobe);
        AddRow(table, "mkvmerge", paths.Mkvmerge);
        AddRow(table, "mkvpropedit", paths.Mkvpropedit);
        AddRow(table, "mediainfo", paths.Mediainfo);

        Console.Write(table);
        Console.MarkupLine(string.Empty);
    }

    private static void AddRow(Table table, string toolName, ToolPathResolution resolution)
    {
        var exists = Path.IsPathRooted(resolution.Path) && File.Exists(resolution.Path) ? "Yes" : "No";
        table.AddRow(toolName, resolution.Path, resolution.Source.ToString(), exists);
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

    private async Task RunVersionCheckAsync(
        string toolName,
        string executable,
        IReadOnlyList<string> args,
        ToolingContext tooling,
        ToolSettings settings,
        CancellationToken cancellationToken)
    {
        var runner = tooling.GetRequiredService<IExternalToolRunner>();
        var runOptions = tooling.RunOptions with { DryRun = false };
        ExternalToolResult result;
        try
        {
            result = await runner.CaptureAsync(executable, args, runOptions, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Console.MarkupLine($"[yellow]{toolName}[/]: Warning ({Markup.Escape(ex.Message)})");
            return;
        }

        if (result.ExitCode == 0)
        {
            Console.MarkupLine($"[green]{toolName}[/]: OK");
            return;
        }

        Console.MarkupLine($"[yellow]{toolName}[/]: Warning (exit code {result.ExitCode})");
        if (!string.IsNullOrWhiteSpace(result.StandardError))
        {
            var tail = TailLines(result.StandardError, 8);
            Console.MarkupLine($"[grey]{Markup.Escape(tail)}[/]");
        }
        else if (settings.Verbose)
        {
            Console.MarkupLine("[grey]No stderr output was captured.[/]");
        }
    }

    private static string TailLines(string input, int count)
    {
        var lines = input.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length <= count)
        {
            return string.Join(Environment.NewLine, lines);
        }

        return string.Join(Environment.NewLine, lines[^count..]);
    }
}
