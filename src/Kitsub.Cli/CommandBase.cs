// Summary: Provides a reusable base for CLI commands with shared error handling and console output.
using Kitsub.Tooling;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Kitsub.Cli;

/// <summary>Defines a base CLI command with shared validation and error handling logic.</summary>
/// <typeparam name="TSettings">The settings type for the command.</typeparam>
public abstract class CommandBase<TSettings> : AsyncCommand<TSettings> where TSettings : ToolSettings
{
    private readonly IAnsiConsole _console;

    protected CommandBase(IAnsiConsole console)
    {
        // Block: Capture the console abstraction used for user-facing output.
        _console = console;
    }

    /// <summary>Runs the command with standardized validation and exception handling.</summary>
    /// <param name="context">The CLI command context.</param>
    /// <param name="settings">The parsed settings for the command.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The process exit code for the command.</returns>
    /// <exception cref="ExternalToolException">Thrown when external tooling fails during execution.</exception>
    public sealed override async Task<int> ExecuteAsync(CommandContext context, TSettings settings, CancellationToken cancellationToken)
    {
        // Block: Wrap execution to enforce consistent validation and error reporting.
        try
        {
            // Block: Validate logging settings before invoking the core command logic.
            ToolingFactory.ValidateLogging(settings);
            return await ExecuteAsyncCore(context, settings, cancellationToken).ConfigureAwait(false);
        }
        catch (ValidationException ex)
        {
            // Block: Surface user-facing validation errors and return a failure code.
            _console.MarkupLine($"[red]{Markup.Escape(ex.Message)}[/]");
            return 1;
        }
        catch (ExternalToolException ex)
        {
            // Block: Report external tool failures with optional stderr details for diagnostics.
            _console.MarkupLine($"[red]{Markup.Escape(ex.Message)}[/]");
            _console.MarkupLine($"[grey]{Markup.Escape(ex.Result.CommandLine)}[/]");
            if (settings.Verbose)
            {
                // Block: Emit external tool stderr output when verbose logging is enabled.
                _console.MarkupLine($"[grey]{Markup.Escape(ex.Result.StandardError)}[/]");
            }

            return 2;
        }
        catch (Exception ex)
        {
            // Block: Handle unexpected failures with a general error message.
            _console.MarkupLine($"[red]{Markup.Escape(ex.Message)}[/]");
            return 2;
        }
    }

    protected IAnsiConsole Console => _console;

    protected abstract Task<int> ExecuteAsyncCore(CommandContext context, TSettings settings, CancellationToken cancellationToken);
}
