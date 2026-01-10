using Spectre.Console.Cli;

namespace Kitsub.Cli;

public static class CommandContextExtensions
{
    public static CancellationToken GetCancellationToken(this CommandContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var property = context.GetType().GetProperty("CancellationToken");
        if (property?.PropertyType == typeof(CancellationToken) &&
            property.GetValue(context) is CancellationToken cancellationToken)
        {
            return cancellationToken;
        }

        return CancellationToken.None;
    }
}
