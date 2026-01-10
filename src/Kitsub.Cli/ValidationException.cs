namespace Kitsub.Cli;

public sealed class ValidationException : Exception
{
    public ValidationException(string message) : base(message)
    {
    }
}
