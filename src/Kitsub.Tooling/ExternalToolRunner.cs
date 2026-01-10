// Summary: Executes external tool processes and captures output for CLI operations.
using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Kitsub.Tooling;

/// <summary>Represents the result of running an external tool.</summary>
/// <param name="ExitCode">The process exit code.</param>
/// <param name="StandardOutput">The captured standard output.</param>
/// <param name="StandardError">The captured standard error.</param>
/// <param name="CommandLine">The rendered command line executed.</param>
public sealed record ExternalToolResult(
    int ExitCode,
    string StandardOutput,
    string StandardError,
    string CommandLine
);

/// <summary>Defines options for running external tools.</summary>
public sealed record ExternalToolRunOptions
{
    /// <summary>Gets a value indicating whether execution should be skipped.</summary>
    public bool DryRun { get; init; }
    /// <summary>Gets a value indicating whether verbose logging is enabled.</summary>
    public bool Verbose { get; init; }
    /// <summary>Gets the callback used to echo command lines.</summary>
    public Action<string>? CommandEcho { get; init; }
    /// <summary>Gets the callback used to handle standard output lines.</summary>
    public Action<string>? StdoutCallback { get; init; }
    /// <summary>Gets the callback used to handle standard error lines.</summary>
    public Action<string>? StderrCallback { get; init; }
}

/// <summary>Runs external tools and optionally captures their output.</summary>
public interface IExternalToolRunner
{
    /// <summary>Runs an external tool and captures standard output and error.</summary>
    /// <param name="exePath">The tool executable path or name.</param>
    /// <param name="args">The tool arguments.</param>
    /// <param name="options">The run options controlling execution.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result containing captured output and exit code.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the process cannot be started.</exception>
    Task<ExternalToolResult> CaptureAsync(
        string exePath,
        IReadOnlyList<string> args,
        ExternalToolRunOptions options,
        CancellationToken cancellationToken);

    /// <summary>Runs an external tool and returns the exit code.</summary>
    /// <param name="exePath">The tool executable path or name.</param>
    /// <param name="args">The tool arguments.</param>
    /// <param name="options">The run options controlling execution.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The exit code from the tool execution.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the process cannot be started.</exception>
    Task<int> RunAsync(
        string exePath,
        IReadOnlyList<string> args,
        ExternalToolRunOptions options,
        CancellationToken cancellationToken);
}

/// <summary>Executes external tools and captures output when requested.</summary>
public sealed class ExternalToolRunner : IExternalToolRunner
{
    private readonly ILogger<ExternalToolRunner> _logger;

    /// <summary>Initializes the runner with the provided logger.</summary>
    /// <param name="logger">The logger used to record tool execution details.</param>
    public ExternalToolRunner(ILogger<ExternalToolRunner> logger)
    {
        // Block: Store the logger used for diagnostics and telemetry.
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ExternalToolResult> CaptureAsync(
        string exePath,
        IReadOnlyList<string> args,
        ExternalToolRunOptions options,
        CancellationToken cancellationToken)
    {
        // Block: Run the process while capturing stdout and stderr.
        return await RunProcessAsync(exePath, args, options, captureOutput: true, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<int> RunAsync(
        string exePath,
        IReadOnlyList<string> args,
        ExternalToolRunOptions options,
        CancellationToken cancellationToken)
    {
        // Block: Run the process without capturing output and return the exit code.
        var result = await RunProcessAsync(exePath, args, options, captureOutput: false, cancellationToken)
            .ConfigureAwait(false);
        return result.ExitCode;
    }

    /// <summary>Renders a command line from an executable and argument list.</summary>
    /// <param name="exePath">The tool executable path or name.</param>
    /// <param name="args">The tool arguments.</param>
    /// <returns>The rendered command-line string.</returns>
    public static string RenderCommandLine(string exePath, IReadOnlyList<string> args)
    {
        // Block: Build a shell-safe command line for logging and display.
        var builder = new StringBuilder();
        builder.Append(QuoteIfNeeded(exePath));
        foreach (var arg in args)
        {
            // Block: Append each argument with quoting when needed.
            builder.Append(' ');
            builder.Append(QuoteIfNeeded(arg));
        }

        return builder.ToString();
    }

    private async Task<ExternalToolResult> RunProcessAsync(
        string exePath,
        IReadOnlyList<string> args,
        ExternalToolRunOptions options,
        bool captureOutput,
        CancellationToken cancellationToken)
    {
        // Block: Render the command line and emit an execution log entry.
        var commandLine = RenderCommandLine(exePath, args);
        _logger.LogInformation("Executing {CommandLine}", commandLine);

        // Block: Echo the command line to any configured output handlers.
        options.CommandEcho?.Invoke(commandLine);

        if (options.DryRun)
        {
            // Block: Short-circuit execution when running in dry-run mode.
            _logger.LogInformation("Dry run: {CommandLine}", commandLine);
            return new ExternalToolResult(0, string.Empty, string.Empty, commandLine);
        }

        // Block: Configure process execution and output redirection settings.
        var redirectOutput = captureOutput || options.Verbose;
        var startInfo = BuildStartInfo(exePath, args, redirectOutput);
        using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = redirectOutput };

        // Block: Allocate output buffers and completion sources when capturing output.
        var stdout = captureOutput ? new StringBuilder() : null;
        var stderr = captureOutput ? new StringBuilder() : null;
        TaskCompletionSource<bool>? stdoutTcs = null;
        TaskCompletionSource<bool>? stderrTcs = null;

        if (redirectOutput)
        {
            // Block: Configure async event handlers to capture process output.
            stdoutTcs = new TaskCompletionSource<bool>();
            stderrTcs = new TaskCompletionSource<bool>();

            process.OutputDataReceived += (_, eventArgs) =>
            {
                if (eventArgs.Data is null)
                {
                    // Block: Signal completion when the stdout stream closes.
                    stdoutTcs.TrySetResult(true);
                }
                else
                {
                    // Block: Buffer stdout and forward lines when verbose output is enabled.
                    stdout?.AppendLine(eventArgs.Data);
                    if (options.Verbose)
                    {
                        _logger.LogDebug("{Line}", eventArgs.Data);
                        options.StdoutCallback?.Invoke(eventArgs.Data);
                    }
                }
            };

            process.ErrorDataReceived += (_, eventArgs) =>
            {
                if (eventArgs.Data is null)
                {
                    // Block: Signal completion when the stderr stream closes.
                    stderrTcs.TrySetResult(true);
                }
                else
                {
                    // Block: Buffer stderr and forward lines when verbose output is enabled.
                    stderr?.AppendLine(eventArgs.Data);
                    if (options.Verbose)
                    {
                        _logger.LogTrace("{Line}", eventArgs.Data);
                        options.StderrCallback?.Invoke(eventArgs.Data);
                    }
                }
            };
        }

        if (!process.Start())
        {
            // Block: Fail fast when the process cannot be started.
            throw new InvalidOperationException($"Failed to start process {exePath}.");
        }

        if (redirectOutput)
        {
            // Block: Begin asynchronous reads of stdout and stderr streams.
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
        }

        // Block: Await process completion and output drain.
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        if (redirectOutput && stdoutTcs is not null && stderrTcs is not null)
        {
            // Block: Ensure both output streams finish before returning results.
            await Task.WhenAll(stdoutTcs.Task, stderrTcs.Task).ConfigureAwait(false);
        }

        // Block: Package the process exit and captured output into a result.
        return new ExternalToolResult(
            process.ExitCode,
            stdout?.ToString() ?? string.Empty,
            stderr?.ToString() ?? string.Empty,
            commandLine);
    }

    private static ProcessStartInfo BuildStartInfo(string exePath, IReadOnlyList<string> args, bool redirectOutput)
    {
        // Block: Initialize process start info with redirection settings.
        var startInfo = new ProcessStartInfo
        {
            FileName = exePath,
            RedirectStandardOutput = redirectOutput,
            RedirectStandardError = redirectOutput,
            UseShellExecute = false
        };

        foreach (var arg in args)
        {
            // Block: Add each argument to the process start info.
            startInfo.ArgumentList.Add(arg);
        }

        return startInfo;
    }

    private static string QuoteIfNeeded(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            // Block: Use empty quotes to represent empty argument values.
            return "\"\"";
        }

        // Block: Quote arguments that contain spaces or quotes.
        return value.Contains(' ') || value.Contains('"')
            ? $"\"{value.Replace("\"", "\\\"")}\""
            : value;
    }
}
