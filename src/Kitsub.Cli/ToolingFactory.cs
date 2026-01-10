using Kitsub.Tooling;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Spectre.Console;

namespace Kitsub.Cli;

public static class ToolingFactory
{
    private const string OutputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}";

    public static ToolPaths BuildToolPaths(ToolSettings settings)
    {
        return new ToolPaths(
            settings.FfmpegPath,
            settings.FfprobePath,
            settings.MkvmergePath,
            settings.MkvpropeditPath);
    }

    public static void ValidateLogging(ToolSettings settings)
    {
        _ = ParseLogLevel(settings.LogLevel);

        if (!settings.NoLog && string.IsNullOrWhiteSpace(settings.LogFile))
        {
            throw new ValidationException("Log file path is required unless --no-log is set.");
        }
    }

    public static ToolingContext CreateTooling(ToolSettings settings, IAnsiConsole console)
    {
        var paths = BuildToolPaths(settings);
        var options = BuildRunOptions(settings, console);
        var logger = CreateLogger(settings);

        var services = new ServiceCollection();
        services.AddSingleton(paths);
        services.AddSingleton(options);
        services.AddSingleton<IExternalToolRunner, ExternalToolRunner>();
        services.AddSingleton<FfprobeClient>();
        services.AddSingleton<MkvmergeClient>();
        services.AddSingleton<MkvmergeMuxer>();
        services.AddSingleton<MkvpropeditClient>();
        services.AddSingleton<FfmpegClient>();
        services.AddSingleton<KitsubService>();
        services.AddLogging(builder => builder.AddSerilog(logger, dispose: true));

        var provider = services.BuildServiceProvider();
        return new ToolingContext(provider, paths, options);
    }

    private static ExternalToolRunOptions BuildRunOptions(ToolSettings settings, IAnsiConsole console)
    {
        Action<string>? commandEcho = null;
        Action<string>? stdoutCallback = null;
        Action<string>? stderrCallback = null;

        if (settings.DryRun || settings.Verbose)
        {
            commandEcho = line => console.MarkupLine($"[grey]{Markup.Escape(line)}[/]");
        }

        if (settings.Verbose)
        {
            stdoutCallback = line =>
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    console.MarkupLine($"[grey]{Markup.Escape(line)}[/]");
                }
            };

            stderrCallback = line =>
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    console.MarkupLine($"[yellow]{Markup.Escape(line)}[/]");
                }
            };
        }

        return new ExternalToolRunOptions
        {
            DryRun = settings.DryRun,
            Verbose = settings.Verbose,
            CommandEcho = commandEcho,
            StdoutCallback = stdoutCallback,
            StderrCallback = stderrCallback
        };
    }

    private static Serilog.ILogger CreateLogger(ToolSettings settings)
    {
        var level = ParseLogLevel(settings.LogLevel);
        var loggerConfiguration = new LoggerConfiguration()
            .MinimumLevel.Is(level)
            .Enrich.FromLogContext();

        if (settings.NoLog)
        {
            loggerConfiguration = loggerConfiguration.WriteTo.Console(outputTemplate: OutputTemplate);
        }
        else
        {
            var directory = Path.GetDirectoryName(settings.LogFile);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            loggerConfiguration = loggerConfiguration.WriteTo.File(
                settings.LogFile,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                outputTemplate: OutputTemplate);
        }

        return loggerConfiguration.CreateLogger();
    }

    private static LogEventLevel ParseLogLevel(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "trace" => LogEventLevel.Verbose,
            "debug" => LogEventLevel.Debug,
            "info" => LogEventLevel.Information,
            "warn" => LogEventLevel.Warning,
            "error" => LogEventLevel.Error,
            null or "" => LogEventLevel.Information,
            _ => throw new ValidationException($"Unknown log level: {value}")
        };
    }
}
