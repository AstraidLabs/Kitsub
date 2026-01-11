// Summary: Defines the JSON manifest schema for external tool provisioning.
using System.Text.Json.Serialization;

namespace Kitsub.Tooling.Provisioning;

/// <summary>Represents the root manifest describing tool sources per RID.</summary>
public sealed class ToolsManifest
{
    /// <summary>Gets the toolset version used for cache invalidation.</summary>
    [JsonPropertyName("toolsetVersion")]
    public string ToolsetVersion { get; init; } = string.Empty;

    /// <summary>Gets the per-RID tool definitions.</summary>
    [JsonPropertyName("rids")]
    public Dictionary<string, ToolsManifestRid> Rids { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>Represents tool definitions for a specific runtime identifier.</summary>
public sealed class ToolsManifestRid
{
    [JsonPropertyName("ffmpeg")]
    public ToolArchiveDefinition? Ffmpeg { get; init; }

    [JsonPropertyName("mkvtoolnix")]
    public ToolArchiveDefinition? Mkvtoolnix { get; init; }
}

/// <summary>Describes a downloadable tool archive and its extraction map.</summary>
public sealed class ToolArchiveDefinition
{
    [JsonPropertyName("version")]
    public string? Version { get; init; }

    [JsonPropertyName("archiveUrl")]
    public string ArchiveUrl { get; init; } = string.Empty;

    [JsonPropertyName("sha256Url")]
    public string Sha256Url { get; init; } = string.Empty;

    [JsonPropertyName("sha256Entry")]
    public string? Sha256Entry { get; init; }

    [JsonPropertyName("archiveType")]
    public string ArchiveType { get; init; } = string.Empty;

    [JsonPropertyName("extractMap")]
    public Dictionary<string, string> ExtractMap { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}
