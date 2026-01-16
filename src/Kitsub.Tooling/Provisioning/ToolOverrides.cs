// Summary: Represents user-provided overrides for external tool paths.
namespace Kitsub.Tooling.Provisioning;

/// <summary>Holds override paths for external tools.</summary>
public sealed record ToolOverrides(
    string? Ffmpeg,
    string? Ffprobe,
    string? Mkvmerge,
    string? Mkvpropedit,
    string? Mediainfo
);
