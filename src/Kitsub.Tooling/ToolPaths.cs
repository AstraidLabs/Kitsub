// Summary: Holds configured executable paths for external media tools.
namespace Kitsub.Tooling;

/// <summary>Represents the executable paths for the supported external tools.</summary>
/// <param name="Ffmpeg">The ffmpeg executable path or name.</param>
/// <param name="Ffprobe">The ffprobe executable path or name.</param>
/// <param name="Mkvmerge">The mkvmerge executable path or name.</param>
/// <param name="Mkvpropedit">The mkvpropedit executable path or name.</param>
public sealed record ToolPaths(
    string Ffmpeg,
    string Ffprobe,
    string Mkvmerge,
    string Mkvpropedit
)
{
    /// <summary>Gets the default tool paths using standard executable names.</summary>
    public static ToolPaths Default => new("ffmpeg", "ffprobe", "mkvmerge", "mkvpropedit");
}
