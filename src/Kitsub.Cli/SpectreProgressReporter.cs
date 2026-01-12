// Summary: Bridges provisioning progress updates to Spectre.Console progress rendering.
using Kitsub.Tooling.Provisioning;
using Spectre.Console;

namespace Kitsub.Cli;

/// <summary>Reports provisioning progress through Spectre.Console.</summary>
public sealed class SpectreProgressReporter : IProgress<ToolProvisionProgress>
{
    private readonly ProgressContext _context;
    private readonly Dictionary<(string ToolName, ToolProvisionProgress.Stage Stage), ProgressTask> _tasks = new();
    private readonly object _lock = new();

    public SpectreProgressReporter(ProgressContext context)
    {
        _context = context;
    }

    public void Report(ToolProvisionProgress value)
    {
        lock (_lock)
        {
            var task = GetOrCreateTask(value);
            switch (value.ProvisionStage)
            {
                case ToolProvisionProgress.Stage.Download:
                    UpdateDownloadTask(task, value);
                    break;
                case ToolProvisionProgress.Stage.Extract:
                    UpdateExtractTask(task, value);
                    break;
            }
        }
    }

    public static bool CanRender(IAnsiConsole console)
    {
        return !console.Profile.Out.IsRedirected && console.Profile.Capabilities.Ansi;
    }

    public static T RunWithProgress<T>(IAnsiConsole console, Func<IProgress<ToolProvisionProgress>, T> action)
    {
        if (!CanRender(console))
        {
            return action(new NullProgressReporter());
        }

        T result = default!;
        console.Progress()
            .Columns(CreateColumns())
            .Start(context =>
            {
                var reporter = new SpectreProgressReporter(context);
                result = action(reporter);
            });

        return result;
    }

    public static async Task<T> RunWithProgressAsync<T>(IAnsiConsole console, Func<IProgress<ToolProvisionProgress>, Task<T>> action)
    {
        if (!CanRender(console))
        {
            return await action(new NullProgressReporter()).ConfigureAwait(false);
        }

        T result = default!;
        await console.Progress()
            .Columns(CreateColumns())
            .StartAsync(async context =>
            {
                var reporter = new SpectreProgressReporter(context);
                result = await action(reporter).ConfigureAwait(false);
            }).ConfigureAwait(false);

        return result;
    }

    private ProgressTask GetOrCreateTask(ToolProvisionProgress progress)
    {
        var key = (progress.ToolName, progress.ProvisionStage);
        if (_tasks.TryGetValue(key, out var existing))
        {
            return existing;
        }

        var description = progress.ProvisionStage switch
        {
            ToolProvisionProgress.Stage.Download => $"Downloading {progress.ToolName}",
            ToolProvisionProgress.Stage.Extract => $"Extracting {progress.ToolName}",
            _ => progress.ToolName
        };

        var task = _context.AddTask(description, new ProgressTaskSettings { AutoStart = true });
        _tasks[key] = task;
        return task;
    }

    private static void UpdateDownloadTask(ProgressTask task, ToolProvisionProgress progress)
    {
        if (progress.TotalBytes.HasValue)
        {
            task.IsIndeterminate = false;
            task.MaxValue = progress.TotalBytes.Value;
            task.Value = Math.Min(progress.CurrentBytes, task.MaxValue);
        }
        else
        {
            task.IsIndeterminate = true;
        }
    }

    private static void UpdateExtractTask(ProgressTask task, ToolProvisionProgress progress)
    {
        if (progress.FilesTotal.HasValue)
        {
            task.IsIndeterminate = false;
            task.MaxValue = progress.FilesTotal.Value;
            task.Value = Math.Min(progress.FilesDone, task.MaxValue);
        }
        else
        {
            task.IsIndeterminate = true;
        }
    }

    private static ProgressColumn[] CreateColumns()
    {
        return new ProgressColumn[]
        {
            new TaskDescriptionColumn(),
            new ProgressBarColumn(),
            new PercentageColumn(),
            new RemainingTimeColumn(),
            new SpinnerColumn()
        };
    }

    private sealed class NullProgressReporter : IProgress<ToolProvisionProgress>
    {
        public void Report(ToolProvisionProgress value)
        {
        }
    }
}
