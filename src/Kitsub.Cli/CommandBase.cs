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
    private readonly AppConfigService _configService;

    protected CommandBase(IAnsiConsole console, AppConfigService configService)
    {
        // Block: Capture the console abstraction used for user-facing output.
        _console = console;
        _configService = configService;
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
            EffectiveConfig = _configService.LoadEffectiveConfig();
            ToolSettingsApplier.Apply(settings, EffectiveConfig);
            ToolingFactory.ValidateLogging(settings);
            return await ExecuteAsyncCore(context, settings, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Block: Handle failures using standardized error handling and exit codes.
            return CommandErrorHandler.Handle(ex, _console, settings.Verbose);
        }
    }

    protected IAnsiConsole Console => _console;

    protected AppConfig EffectiveConfig { get; private set; } = AppConfigDefaults.CreateDefaults();

    protected AppConfigService ConfigService => _configService;

    protected abstract Task<int> ExecuteAsyncCore(CommandContext context, TSettings settings, CancellationToken cancellationToken);
}
