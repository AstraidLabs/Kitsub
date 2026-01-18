// Summary: Defines a specialized exception used to signal CLI validation failures.
namespace Kitsub.Cli;

/// <summary>Represents errors that occur when validating command inputs.</summary>
public sealed class ValidationException : Exception
{
    /// <summary>Initializes a new instance with the specified validation message.</summary>
    /// <param name="message">The validation error message.</param>
    public ValidationException(string message) : base(message)
    {
        // Block: Initialize the exception with a validation-specific message.
    }

    /// <summary>Initializes a new instance with the specified message and inner exception.</summary>
    /// <param name="message">The validation error message.</param>
    /// <param name="innerException">The inner exception that caused the validation failure.</param>
    public ValidationException(string message, Exception innerException) : base(message, innerException)
    {
        // Block: Initialize the exception with a validation-specific message and inner exception.
    }
}
