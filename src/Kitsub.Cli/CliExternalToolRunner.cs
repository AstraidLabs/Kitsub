using Kitsub.Tooling;
using Spectre.Console;

namespace Kitsub.Cli;

public sealed class CliExternalToolRunner : IExternalToolRunner
{
    private readonly ExternalToolRunner _inner = new();
    private readonly IAnsiConsole _console;
    private readonly bool _dryRun;
    private readonly bool _verbose;

    public CliExternalToolRunner(IAnsiConsole console, bool dryRun, bool verbose)
    {
        _console = console;
        _dryRun = dryRun;
        _verbose = verbose;
    }

    public async Task<ExternalToolResult> CaptureAsync(string exePath, IReadOnlyList<string> args, CancellationToken cancellationToken)
    {
        var commandLine = CommandLineRenderer.Render(exePath, args);
        if (_dryRun)
        {
            _console.MarkupLine($"[grey]{Markup.Escape(commandLine)}[/]");
            return new ExternalToolResult(0, string.Empty, string.Empty, commandLine);
        }

        if (_verbose)
        {
            _console.MarkupLine($"[grey]{Markup.Escape(commandLine)}[/]");
        }

        var result = await _inner.CaptureAsync(exePath, args, cancellationToken).ConfigureAwait(false);
        if (_verbose)
        {
            WriteOutput(result.StandardOutput, "stdout");
            WriteOutput(result.StandardError, "stderr");
        }

        return result;
    }

    public async Task<int> RunAsync(string exePath, IReadOnlyList<string> args, CancellationToken cancellationToken)
    {
        var commandLine = CommandLineRenderer.Render(exePath, args);
        if (_dryRun)
        {
            _console.MarkupLine($"[grey]{Markup.Escape(commandLine)}[/]");
            return 0;
        }

        if (_verbose)
        {
            _console.MarkupLine($"[grey]{Markup.Escape(commandLine)}[/]");
        }

        return await _inner.RunAsync(exePath, args, cancellationToken).ConfigureAwait(false);
    }

    private void WriteOutput(string value, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        _console.MarkupLine($"[grey]{Markup.Escape(label)}:[/]");
        foreach (var line in value.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries))
        {
            _console.MarkupLine($"[grey]{Markup.Escape(line)}[/]");
        }
    }
}
