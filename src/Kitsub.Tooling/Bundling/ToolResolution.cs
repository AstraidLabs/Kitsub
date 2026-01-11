// Summary: Defines resolved tool paths and their origin sources.
namespace Kitsub.Tooling.Bundling;

/// <summary>Identifies where a tool executable path was resolved from.</summary>
public enum ToolPathSource
{
    Override,
    Bundled,
    Extracted,
    Path
}

/// <summary>Represents a resolved tool path with its source.</summary>
/// <param name="Path">The resolved executable path.</param>
/// <param name="Source">The source of the path.</param>
public sealed record ToolPathResolution(string Path, ToolPathSource Source);

/// <summary>Holds resolved tool paths along with toolset metadata.</summary>
/// <param name="RuntimeRid">The runtime identifier used for resolution.</param>
/// <param name="ToolsetVersion">The toolset version from the manifest.</param>
/// <param name="Ffmpeg">The resolved ffmpeg path.</param>
/// <param name="Ffprobe">The resolved ffprobe path.</param>
/// <param name="Mkvmerge">The resolved mkvmerge path.</param>
/// <param name="Mkvpropedit">The resolved mkvpropedit path.</param>
public sealed record ToolPathsResolved(
    string RuntimeRid,
    string ToolsetVersion,
    ToolPathResolution Ffmpeg,
    ToolPathResolution Ffprobe,
    ToolPathResolution Mkvmerge,
    ToolPathResolution Mkvpropedit
);
