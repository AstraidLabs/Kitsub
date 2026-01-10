using System.Diagnostics;
using System.Text;

namespace Kitsub.Tooling;

public sealed record ExternalToolResult(
    int ExitCode,
    string StandardOutput,
    string StandardError,
    string CommandLine
);

public interface IExternalToolRunner
{
    Task<ExternalToolResult> CaptureAsync(string exePath, IReadOnlyList<string> args, CancellationToken cancellationToken);
    Task<int> RunAsync(string exePath, IReadOnlyList<string> args, CancellationToken cancellationToken);
}

public sealed class ExternalToolRunner : IExternalToolRunner
{
    public async Task<ExternalToolResult> CaptureAsync(string exePath, IReadOnlyList<string> args, CancellationToken cancellationToken)
    {
        var startInfo = BuildStartInfo(exePath, args);
        using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };

        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        var stdoutTcs = new TaskCompletionSource<bool>();
        var stderrTcs = new TaskCompletionSource<bool>();

        process.OutputDataReceived += (_, eventArgs) =>
        {
            if (eventArgs.Data is null)
            {
                stdoutTcs.TrySetResult(true);
            }
            else
            {
                stdout.AppendLine(eventArgs.Data);
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
                stderr.AppendLine(eventArgs.Data);
            }
        };

        if (!process.Start())
        {
            throw new InvalidOperationException($"Failed to start process {exePath}.");
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        await Task.WhenAll(stdoutTcs.Task, stderrTcs.Task).ConfigureAwait(false);

        return new ExternalToolResult(
            process.ExitCode,
            stdout.ToString(),
            stderr.ToString(),
            CommandLineRenderer.Render(exePath, args));
    }

    public async Task<int> RunAsync(string exePath, IReadOnlyList<string> args, CancellationToken cancellationToken)
    {
        var startInfo = BuildStartInfo(exePath, args);
        using var process = new Process { StartInfo = startInfo };
        if (!process.Start())
        {
            throw new InvalidOperationException($"Failed to start process {exePath}.");
        }

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        return process.ExitCode;
    }

    private static ProcessStartInfo BuildStartInfo(string exePath, IReadOnlyList<string> args)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = exePath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        foreach (var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        return startInfo;
    }
}

public static class CommandLineRenderer
{
    public static string Render(string exePath, IReadOnlyList<string> args)
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
