// Summary: Defines shared exception types used for configuration and integrity errors.
namespace Kitsub.Core;

/// <summary>Represents errors related to configuration loading or validation.</summary>
public sealed class ConfigurationException : Exception
{
    public ConfigurationException(string message) : base(message)
    {
    }

    public ConfigurationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>Represents integrity failures such as hash mismatches or unsafe extraction paths.</summary>
public sealed class IntegrityException : Exception
{
    public IntegrityException(string message) : base(message)
    {
    }

    public IntegrityException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>Represents provisioning failures during tool download or extraction.</summary>
public sealed class ProvisioningException : Exception
{
    public ProvisioningException(string message) : base(message)
    {
    }

    public ProvisioningException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
