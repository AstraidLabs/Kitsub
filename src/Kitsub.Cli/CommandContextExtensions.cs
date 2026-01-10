// Summary: Provides helper extensions for accessing Spectre.Console command context data.
using Spectre.Console.Cli;

namespace Kitsub.Cli;

/// <summary>Provides extension methods for working with command contexts.</summary>
public static class CommandContextExtensions
{
    /// <summary>Gets a cancellation token from the command context when available.</summary>
    /// <param name="context">The command context to inspect.</param>
    /// <returns>The context cancellation token, or <see cref="CancellationToken.None"/> if unavailable.</returns>
    public static CancellationToken GetCancellationToken(this CommandContext context)
    {
        // Block: Guard against null context to avoid reflection on an invalid instance.
        ArgumentNullException.ThrowIfNull(context);

        // Block: Probe for an optional CancellationToken property to support Spectre versions that expose it.
        var property = context.GetType().GetProperty("CancellationToken");
        if (property?.PropertyType == typeof(CancellationToken) &&
            property.GetValue(context) is CancellationToken cancellationToken)
        {
            // Block: Return the discovered cancellation token when the property matches expectations.
            return cancellationToken;
        }

        // Block: Fall back to a non-cancelable token when the context does not expose one.
        return CancellationToken.None;
    }
}
