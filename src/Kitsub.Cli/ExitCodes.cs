// Summary: Defines process exit codes used by the CLI.
namespace Kitsub.Cli;

/// <summary>Defines stable exit codes for Kitsub.</summary>
public static class ExitCodes
{
    public const int Success = 0;
    public const int ValidationError = 1;
    public const int ExternalToolFailure = 2;
    public const int ProvisioningFailure = 3;
    public const int IntegrityFailure = 4;
    public const int UnexpectedError = 5;
}
