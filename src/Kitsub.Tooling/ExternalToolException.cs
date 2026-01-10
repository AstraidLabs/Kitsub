namespace Kitsub.Tooling;

public sealed class ExternalToolException : Exception
{
    public ExternalToolException(string message, ExternalToolResult result)
        : base(message)
    {
        Result = result;
    }

    public ExternalToolResult Result { get; }
}
