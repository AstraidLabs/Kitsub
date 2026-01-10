// Summary: Builds tooling contexts, logging, and run options used by CLI commands.
using Kitsub.Tooling;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Spectre.Console;

namespace Kitsub.Cli;

/// <summary>Creates tooling services, logging, and configuration for command execution.</summary>
public static class ToolingFactory
{
    private const string OutputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}";

    /// <summary>Builds the tool path configuration from command settings.</summary>
    /// <param name="settings">The settings containing external tool paths.</param>
    /// <returns>A <see cref="ToolPaths"/> instance populated with tool locations.</returns>
    public static ToolPaths BuildToolPaths(ToolSettings settings)
    {
        // Block: Translate CLI settings into a ToolPaths value for dependency injection.
        return new ToolPaths(
            settings.FfmpegPath,
            settings.FfprobePath,
            settings.MkvmergePath,
            settings.MkvpropeditPath);
    }

    /// <summary>Validates logging settings and throws when configuration is invalid.</summary>
    /// <param name="settings">The settings containing logging options.</param>
    /// <exception cref="ValidationException">Thrown when logging settings are invalid.</exception>
    public static void ValidateLogging(ToolSettings settings)
    {
        // Block: Validate the log level string before proceeding.
        _ = ParseLogLevel(settings.LogLevel);

        if (!settings.NoLog && string.IsNullOrWhiteSpace(settings.LogFile))
        {
            // Block: Enforce a log file path when logging is enabled.
            throw new ValidationException("Log file path is required unless --no-log is set.");
        }
    }

    /// <summary>Creates a tooling context with registered services and run options.</summary>
    /// <param name="settings">The settings that configure tooling and logging.</param>
    /// <param name="console">The console used for optional command echo output.</param>
    /// <returns>A fully configured <see cref="ToolingContext"/> instance.</returns>
    public static ToolingContext CreateTooling(ToolSettings settings, IAnsiConsole console)
    {
        // Block: Build tool paths, run options, and logging configuration.
        var paths = BuildToolPaths(settings);
        var options = BuildRunOptions(settings, console);
        var logger = CreateLogger(settings);

        // Block: Register tooling services and logging into a DI container.
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

        // Block: Build the provider and package it into a tooling context.
        var provider = services.BuildServiceProvider();
        return new ToolingContext(provider, paths, options);
    }

    private static ExternalToolRunOptions BuildRunOptions(ToolSettings settings, IAnsiConsole console)
    {
        // Block: Initialize optional callbacks used to echo tool commands and output.
        Action<string>? commandEcho = null;
        Action<string>? stdoutCallback = null;
        Action<string>? stderrCallback = null;

        if (settings.DryRun || settings.Verbose)
        {
            // Block: Enable command echoing when dry-run or verbose output is requested.
            commandEcho = line => console.MarkupLine($"[grey]{Markup.Escape(line)}[/]");
        }

        if (settings.Verbose)
        {
            // Block: Emit stdout and stderr lines to the console in verbose mode.
            stdoutCallback = line =>
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    // Block: Show non-empty stdout lines with neutral styling.
                    console.MarkupLine($"[grey]{Markup.Escape(line)}[/]");
                }
            };

            stderrCallback = line =>
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    // Block: Show non-empty stderr lines with warning styling.
                    console.MarkupLine($"[yellow]{Markup.Escape(line)}[/]");
                }
            };
        }

        // Block: Package the run options into a configuration object for tooling calls.
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
        // Block: Start with a base logger configuration and parsed log level.
        var level = ParseLogLevel(settings.LogLevel);
        var loggerConfiguration = new LoggerConfiguration()
            .MinimumLevel.Is(level)
            .Enrich.FromLogContext();

        if (settings.NoLog)
        {
            // Block: Send logs to the console when logging to file is disabled.
            loggerConfiguration = loggerConfiguration.WriteTo.Console(outputTemplate: OutputTemplate);
        }
        else
        {
            // Block: Prepare the log directory and configure file-based logging.
            var directory = Path.GetDirectoryName(settings.LogFile);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                // Block: Ensure the log directory exists before writing log files.
                Directory.CreateDirectory(directory);
            }

            loggerConfiguration = loggerConfiguration.WriteTo.File(
                settings.LogFile,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                outputTemplate: OutputTemplate);
        }

        // Block: Finalize and return the configured logger instance.
        return loggerConfiguration.CreateLogger();
    }

    private static LogEventLevel ParseLogLevel(string? value)
    {
        // Block: Map the configured log level text into a Serilog log level.
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
