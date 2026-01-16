// Summary: Configures tool resolution and provisioning behavior.
namespace Kitsub.Tooling.Provisioning;

/// <summary>Defines options used when resolving tool paths.</summary>
public sealed class ToolResolveOptions
{
    /// <summary>Gets or sets a value indicating whether provisioning is allowed.</summary>
    public bool AllowProvisioning { get; init; } = true;

    /// <summary>Gets or sets a value indicating whether bundled tools are preferred.</summary>
    public bool PreferBundled { get; init; } = true;

    /// <summary>Gets or sets a value indicating whether PATH-based resolution is preferred.</summary>
    public bool PreferPath { get; init; }

    /// <summary>Gets or sets an optional tools cache directory override.</summary>
    public string? ToolsCacheDir { get; init; }

    /// <summary>Gets or sets a value indicating whether provisioning should run in dry-run mode.</summary>
    public bool DryRun { get; init; }

    /// <summary>Gets or sets a value indicating whether verbose logging is enabled.</summary>
    public bool Verbose { get; init; }
}
