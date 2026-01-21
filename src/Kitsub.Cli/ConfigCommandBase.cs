// Summary: Provides a base class for configuration commands with shared error handling.
using Spectre.Console;
using Spectre.Console.Cli;

namespace Kitsub.Cli;

/// <summary>Defines a base CLI command for configuration operations.</summary>
/// <typeparam name="TSettings">The settings type for the command.</typeparam>
public abstract class ConfigCommandBase<TSettings> : AsyncCommand<TSettings> where TSettings : CommandSettings
{
    private readonly IAnsiConsole _console;
    private readonly AppConfigService _configService;

    protected ConfigCommandBase(IAnsiConsole console, AppConfigService configService)
    {
        _console = console;
        _configService = configService;
    }

    public sealed override async Task<int> ExecuteAsync(CommandContext context, TSettings settings, CancellationToken cancellationToken)
    {
        try
        {
            return await ExecuteAsyncCore(context, settings, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return CommandErrorHandler.Handle(ex, _console, verbose: false, context.GetCommandPath());
        }
    }

    protected IAnsiConsole Console => _console;

    protected AppConfigService ConfigService => _configService;

    protected abstract Task<int> ExecuteAsyncCore(CommandContext context, TSettings settings, CancellationToken cancellationToken);
}
