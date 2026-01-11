using Xunit;

namespace Kitsub.Tests.Integration;

public sealed class IntegrationTestAttribute : FactAttribute
{
    public IntegrationTestAttribute()
    {
        var enabled = Environment.GetEnvironmentVariable("KITSUB_RUN_INTEGRATION");
        if (!string.Equals(enabled, "1", StringComparison.OrdinalIgnoreCase))
        {
            Skip = "Integration tests are disabled. Set KITSUB_RUN_INTEGRATION=1 to enable.";
        }
    }
}
