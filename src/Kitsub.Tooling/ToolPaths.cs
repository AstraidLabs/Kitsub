namespace Kitsub.Tooling;

public sealed record ToolPaths(
    string Ffmpeg,
    string Ffprobe,
    string Mkvmerge,
    string Mkvpropedit
)
{
    public static ToolPaths Default => new("ffmpeg", "ffprobe", "mkvmerge", "mkvpropedit");
}
