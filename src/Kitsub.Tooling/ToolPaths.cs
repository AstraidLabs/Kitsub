// Summary: Holds resolved executable paths for external media tools.
namespace Kitsub.Tooling;

/// <summary>Represents the executable paths for the supported external tools.</summary>
/// <param name="Ffmpeg">The resolved ffmpeg executable path.</param>
/// <param name="Ffprobe">The resolved ffprobe executable path.</param>
/// <param name="Mkvmerge">The resolved mkvmerge executable path.</param>
/// <param name="Mkvpropedit">The resolved mkvpropedit executable path.</param>
/// <param name="Mediainfo">The resolved mediainfo executable path.</param>
public sealed record ToolPaths(
    string Ffmpeg,
    string Ffprobe,
    string Mkvmerge,
    string Mkvpropedit,
    string Mediainfo
);
