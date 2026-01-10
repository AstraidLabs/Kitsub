// Summary: Defines an exception that captures failures from external tool executions.
namespace Kitsub.Tooling;

/// <summary>Represents errors reported by external tool executions.</summary>
public sealed class ExternalToolException : Exception
{
    /// <summary>Initializes a new instance with the specified message and tool result.</summary>
    /// <param name="message">The error message describing the failure.</param>
    /// <param name="result">The external tool result associated with the failure.</param>
    public ExternalToolException(string message, ExternalToolResult result)
        : base(message)
    {
        // Block: Store the external tool result for diagnostic reporting.
        Result = result;
    }

    /// <summary>Gets the external tool result associated with the failure.</summary>
    public ExternalToolResult Result { get; }
}
