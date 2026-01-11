// Summary: Represents resolved tool paths and their sources.
namespace Kitsub.Tooling.Provisioning;

/// <summary>Identifies the origin of a resolved tool path.</summary>
public enum ToolSource
{
    Override,
    Bundled,
    Cache,
    Path
}

/// <summary>Represents a resolved tool path with its source.</summary>
public sealed record ToolPathResolution(string Path, ToolSource Source);

/// <summary>Contains resolved tool paths alongside the toolset version.</summary>
public sealed record ToolResolution(
    string RuntimeRid,
    string ToolsetVersion,
    ToolPathResolution Ffmpeg,
    ToolPathResolution Ffprobe,
    ToolPathResolution Mkvmerge,
    ToolPathResolution Mkvpropedit
);
