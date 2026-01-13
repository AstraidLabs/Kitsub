// Summary: Handles command exceptions and maps them to exit codes.
using Kitsub.Core;
using Kitsub.Tooling;
using Spectre.Console;

namespace Kitsub.Cli;

/// <summary>Maps known exceptions to exit codes and renders error output.</summary>
public static class CommandErrorHandler
{
    private const int TailLineCount = 12;

    public static int Handle(Exception ex, IAnsiConsole console, bool verbose)
    {
        switch (ex)
        {
            case ValidationException or ConfigurationException:
                console.MarkupLine($"[red]{Markup.Escape(ex.Message)}[/]");
                return ExitCodes.ValidationError;
            case IntegrityException:
                console.MarkupLine($"[red]{Markup.Escape(ex.Message)}[/]");
                return ExitCodes.IntegrityFailure;
            case ProvisioningException:
                console.MarkupLine($"[red]{Markup.Escape(ex.Message)}[/]");
                return ExitCodes.ProvisioningFailure;
            case ExternalToolException toolException:
                RenderExternalToolError(toolException, console, verbose);
                return ExitCodes.ExternalToolFailure;
            default:
                console.MarkupLine($"[red]{Markup.Escape(ex.Message)}[/]");
                return ExitCodes.UnexpectedError;
        }
    }

    private static void RenderExternalToolError(ExternalToolException ex, IAnsiConsole console, bool verbose)
    {
        console.MarkupLine($"[red]{Markup.Escape(ex.Message)}[/]");
        console.MarkupLine($"[grey]{Markup.Escape(ex.Result.CommandLine)}[/]");

        var stderr = ex.Result.StandardError;
        if (!string.IsNullOrWhiteSpace(stderr))
        {
            var tail = TailLines(stderr, TailLineCount);
            console.MarkupLine("[yellow]Last stderr lines:[/]");
            console.MarkupLine($"[grey]{Markup.Escape(tail)}[/]");
        }

        if (verbose && string.IsNullOrWhiteSpace(stderr))
        {
            console.MarkupLine("[grey]No stderr output was captured.[/]");
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
