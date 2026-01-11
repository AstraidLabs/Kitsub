// Summary: Defines the manifest schema describing bundled tool binaries.
using System.Text.Json.Serialization;

namespace Kitsub.Tooling.Bundling;

/// <summary>Represents the manifest describing bundled tools and versions.</summary>
public sealed class ToolManifest
{
    /// <summary>Gets or sets the toolset version used for cache invalidation.</summary>
    [JsonPropertyName("toolsetVersion")]
    public string ToolsetVersion { get; init; } = "unknown";

    /// <summary>Gets or sets the per-RID tool definitions.</summary>
    [JsonPropertyName("rids")]
    public Dictionary<string, ToolManifestRid> Rids { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>Represents tool paths for a specific runtime identifier.</summary>
public sealed class ToolManifestRid
{
    [JsonPropertyName("ffmpeg")]
    public ToolManifestTool? Ffmpeg { get; init; }

    [JsonPropertyName("ffprobe")]
    public ToolManifestTool? Ffprobe { get; init; }

    [JsonPropertyName("mkvmerge")]
    public ToolManifestTool? Mkvmerge { get; init; }

    [JsonPropertyName("mkvpropedit")]
    public ToolManifestTool? Mkvpropedit { get; init; }
}

/// <summary>Describes a tool entry in the manifest.</summary>
public sealed class ToolManifestTool
{
    /// <summary>Gets or sets the relative path to the tool executable.</summary>
    [JsonPropertyName("path")]
    public string Path { get; init; } = string.Empty;

    /// <summary>Gets or sets the optional SHA256 hash for verification.</summary>
    [JsonPropertyName("sha256")]
    public string? Sha256 { get; init; }
}
