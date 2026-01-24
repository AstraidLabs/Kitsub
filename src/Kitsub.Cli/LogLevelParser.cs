// Summary: Parses log level values for logging configuration.
using Serilog.Events;

namespace Kitsub.Cli;

/// <summary>Provides helper methods for parsing log level strings.</summary>
public static class LogLevelParser
{
    public static LogEventLevel Parse(string? value)
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
