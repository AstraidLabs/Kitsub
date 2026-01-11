// Summary: Defines options for tool resolution preferences.
namespace Kitsub.Tooling.Bundling;

/// <summary>Represents settings that influence tool discovery order and cache location.</summary>
public sealed record ToolResolverOptions
{
    /// <summary>Gets or sets a value indicating whether bundled tools are preferred.</summary>
    public bool PreferBundled { get; init; } = true;

    /// <summary>Gets or sets a value indicating whether PATH should be preferred.</summary>
    public bool PreferPath { get; init; }

    /// <summary>Gets or sets an optional override for the tools cache directory.</summary>
    public string? ToolsCacheDirectory { get; init; }
}
