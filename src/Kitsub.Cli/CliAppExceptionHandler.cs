// Summary: Handles top-level CLI parsing exceptions with guidance.
using Spectre.Console;

namespace Kitsub.Cli;

/// <summary>Provides centralized exception handling for command parsing errors.</summary>
public static class CliAppExceptionHandler
{
    /// <summary>Handles application-level exceptions during command parsing.</summary>
    /// <param name="exception">The exception to handle.</param>
    /// <param name="console">The console for output.</param>
    /// <param name="args">The raw command-line arguments.</param>
    /// <returns>The exit code for the failure.</returns>
    public static int Handle(Exception exception, IAnsiConsole console, string[] args)
    {
        if (IsUnknownCommand(exception))
        {
            var input = CommandInventory.GetUserCommandInput(args) ?? string.Empty;
            var suggestions = CommandInventory.Suggest(input);
            CliErrorRenderer.RenderUnknownCommand(console, input, suggestions);
            return ExitCodes.ValidationError;
        }

        if (IsUsageError(exception))
        {
            var commandPath = CommandInventory.ResolveKnownCommandPath(args);
            CliErrorRenderer.RenderUsageError(console, exception.Message, commandPath, includeCommandTip: !string.IsNullOrWhiteSpace(commandPath));
            return ExitCodes.ValidationError;
        }

        console.MarkupLine($"[red]{Markup.Escape(exception.Message)}[/]");
        return ExitCodes.UnexpectedError;
    }

    private static bool IsUnknownCommand(Exception exception)
    {
        return string.Equals(exception.GetType().Name, "CommandNotFoundException", StringComparison.Ordinal);
    }

    private static bool IsUsageError(Exception exception)
    {
        return string.Equals(exception.GetType().Name, "CommandParseException", StringComparison.Ordinal) ||
               string.Equals(exception.GetType().Name, "CommandRuntimeException", StringComparison.Ordinal) ||
               string.Equals(exception.GetType().Name, "CommandSettingsException", StringComparison.Ordinal);
    }
}
