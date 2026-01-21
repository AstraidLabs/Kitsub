// Summary: Renders CLI usage errors with tips and examples.
using Spectre.Console;

namespace Kitsub.Cli;

/// <summary>Formats error output for CLI usage problems.</summary>
public static class CliErrorRenderer
{
    /// <summary>Renders a usage error with tips and examples.</summary>
    /// <param name="console">The console for output.</param>
    /// <param name="message">The error message to display.</param>
    /// <param name="commandPath">The command path, when available.</param>
    /// <param name="includeCommandTip">Whether to include a command-specific help hint.</param>
    public static void RenderUsageError(IAnsiConsole console, string message, string? commandPath, bool includeCommandTip)
    {
        console.MarkupLine($"[red]Error:[/] {Markup.Escape(message)}");
        console.MarkupLine("[yellow]Tip:[/] kitsub --help");

        if (includeCommandTip && !string.IsNullOrWhiteSpace(commandPath))
        {
            console.MarkupLine($"[yellow]Tip:[/] kitsub {Markup.Escape(commandPath)} --help");
        }

        RenderExamples(console, ExamplesRegistry.GetExamples(commandPath));
    }

    /// <summary>Renders an unknown command error with suggestions.</summary>
    /// <param name="console">The console for output.</param>
    /// <param name="input">The command input that was not recognized.</param>
    /// <param name="suggestions">Suggested commands that closely match the input.</param>
    public static void RenderUnknownCommand(IAnsiConsole console, string input, IReadOnlyList<string> suggestions)
    {
        var safeInput = string.IsNullOrWhiteSpace(input) ? "" : Markup.Escape(input);
        console.MarkupLine($"[red]Error:[/] Unknown command: \"{safeInput}\"");
        console.MarkupLine("[yellow]Tip:[/] kitsub --help");

        if (suggestions.Count > 0)
        {
            console.MarkupLine("Did you mean:");
            foreach (var suggestion in suggestions)
            {
                console.MarkupLine($"  - {Markup.Escape(suggestion)}");
            }
        }

        var exampleCommand = suggestions.FirstOrDefault();
        RenderExamples(console, ExamplesRegistry.GetExamples(exampleCommand));
    }

    private static void RenderExamples(IAnsiConsole console, IReadOnlyList<string> examples)
    {
        if (examples.Count == 0)
        {
            return;
        }

        console.MarkupLine("[grey]Examples:[/]");
        foreach (var example in examples)
        {
            console.MarkupLine($"  - {Markup.Escape(example)}");
        }
    }
}
