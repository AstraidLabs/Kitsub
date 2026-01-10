using Kitsub.Tooling;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Kitsub.Cli;

public abstract class CommandBase<TSettings> : AsyncCommand<TSettings> where TSettings : ToolSettings
{
    private readonly IAnsiConsole _console;

    protected CommandBase(IAnsiConsole console)
    {
        _console = console;
    }

    public sealed override async Task<int> ExecuteAsync(CommandContext context, TSettings settings)
    {
        try
        {
            ToolingFactory.ValidateLogging(settings);
            return await ExecuteAsyncCore(context, settings).ConfigureAwait(false);
        }
        catch (ValidationException ex)
        {
            _console.MarkupLine($"[red]{Markup.Escape(ex.Message)}[/]");
            return 1;
        }
        catch (ExternalToolException ex)
        {
            _console.MarkupLine($"[red]{Markup.Escape(ex.Message)}[/]");
            _console.MarkupLine($"[grey]{Markup.Escape(ex.Result.CommandLine)}[/]");
            if (settings.Verbose)
            {
                _console.MarkupLine($"[grey]{Markup.Escape(ex.Result.StandardError)}[/]");
            }

            return 2;
        }
        catch (Exception ex)
        {
            _console.MarkupLine($"[red]{Markup.Escape(ex.Message)}[/]");
            return 2;
        }
    }

    protected IAnsiConsole Console => _console;

    protected abstract Task<int> ExecuteAsyncCore(CommandContext context, TSettings settings);
}
