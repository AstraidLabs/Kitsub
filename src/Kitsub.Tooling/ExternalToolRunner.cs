using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Kitsub.Tooling;

public sealed record ExternalToolResult(
    int ExitCode,
    string StandardOutput,
    string StandardError,
    string CommandLine
);

public sealed record ExternalToolRunOptions
{
    public bool DryRun { get; init; }
    public bool Verbose { get; init; }
    public Action<string>? CommandEcho { get; init; }
    public Action<string>? StdoutCallback { get; init; }
    public Action<string>? StderrCallback { get; init; }
}

public interface IExternalToolRunner
{
    Task<ExternalToolResult> CaptureAsync(
        string exePath,
        IReadOnlyList<string> args,
        ExternalToolRunOptions options,
        CancellationToken cancellationToken);

    Task<int> RunAsync(
        string exePath,
        IReadOnlyList<string> args,
        ExternalToolRunOptions options,
        CancellationToken cancellationToken);
}

public sealed class ExternalToolRunner : IExternalToolRunner
{
    private readonly ILogger<ExternalToolRunner> _logger;

    public ExternalToolRunner(ILogger<ExternalToolRunner> logger)
    {
        _logger = logger;
    }

    public async Task<ExternalToolResult> CaptureAsync(
        string exePath,
        IReadOnlyList<string> args,
        ExternalToolRunOptions options,
        CancellationToken cancellationToken)
    {
        return await RunProcessAsync(exePath, args, options, captureOutput: true, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<int> RunAsync(
        string exePath,
        IReadOnlyList<string> args,
        ExternalToolRunOptions options,
        CancellationToken cancellationToken)
    {
        var result = await RunProcessAsync(exePath, args, options, captureOutput: false, cancellationToken)
            .ConfigureAwait(false);
        return result.ExitCode;
    }

    public static string RenderCommandLine(string exePath, IReadOnlyList<string> args)
    {
        var builder = new StringBuilder();
        builder.Append(QuoteIfNeeded(exePath));
        foreach (var arg in args)
        {
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
        var commandLine = RenderCommandLine(exePath, args);
        _logger.LogInformation("Executing {CommandLine}", commandLine);

        options.CommandEcho?.Invoke(commandLine);

        if (options.DryRun)
        {
            _logger.LogInformation("Dry run: {CommandLine}", commandLine);
            return new ExternalToolResult(0, string.Empty, string.Empty, commandLine);
        }

        var redirectOutput = captureOutput || options.Verbose;
        var startInfo = BuildStartInfo(exePath, args, redirectOutput);
        using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = redirectOutput };

        var stdout = captureOutput ? new StringBuilder() : null;
        var stderr = captureOutput ? new StringBuilder() : null;
        TaskCompletionSource<bool>? stdoutTcs = null;
        TaskCompletionSource<bool>? stderrTcs = null;

        if (redirectOutput)
        {
            stdoutTcs = new TaskCompletionSource<bool>();
            stderrTcs = new TaskCompletionSource<bool>();

            process.OutputDataReceived += (_, eventArgs) =>
            {
                if (eventArgs.Data is null)
                {
                    stdoutTcs.TrySetResult(true);
                }
                else
                {
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
                    stderrTcs.TrySetResult(true);
                }
                else
                {
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
            throw new InvalidOperationException($"Failed to start process {exePath}.");
        }

        if (redirectOutput)
        {
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
        }

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        if (redirectOutput && stdoutTcs is not null && stderrTcs is not null)
        {
            await Task.WhenAll(stdoutTcs.Task, stderrTcs.Task).ConfigureAwait(false);
        }

        return new ExternalToolResult(
            process.ExitCode,
            stdout?.ToString() ?? string.Empty,
            stderr?.ToString() ?? string.Empty,
            commandLine);
    }

    private static ProcessStartInfo BuildStartInfo(string exePath, IReadOnlyList<string> args, bool redirectOutput)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = exePath,
            RedirectStandardOutput = redirectOutput,
            RedirectStandardError = redirectOutput,
            UseShellExecute = false
        };

        foreach (var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        return startInfo;
    }

    private static string QuoteIfNeeded(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "\"\"";
        }

        return value.Contains(' ') || value.Contains('"')
            ? $"\"{value.Replace("\"", "\\\"")}\""
            : value;
    }
}
