namespace Kitsub.Core;

public enum TrackType
{
    Video,
    Audio,
    Subtitle
}

public sealed record AttachmentInfo(
    string FileName,
    string MimeType,
    long SizeBytes
);

public sealed record TrackInfo
{
    public int Index { get; init; }
    public int? Id { get; init; }
    public TrackType Type { get; init; }
    public string Codec { get; init; } = string.Empty;
    public string? Language { get; init; }
    public string? Title { get; init; }
    public bool IsDefault { get; init; }
    public bool IsForced { get; init; }
    public IReadOnlyDictionary<string, string> Extra { get; init; } = new Dictionary<string, string>();
}

public sealed record MediaInfo
{
    public string FilePath { get; init; } = string.Empty;
    public string? Container { get; init; }
    public TimeSpan? Duration { get; init; }
    public long? SizeBytes { get; init; }
    public IReadOnlyList<TrackInfo> Tracks { get; init; } = Array.Empty<TrackInfo>();
    public IReadOnlyList<AttachmentInfo> Attachments { get; init; } = Array.Empty<AttachmentInfo>();
}

public sealed record SubtitleDescriptor(
    string FilePath,
    string? Language,
    string? Title,
    bool? IsDefault,
    bool? IsForced
);

public static class TrackSelection
{
    public static TrackInfo? SelectTrack(MediaInfo info, TrackType type, string selector)
    {
        if (int.TryParse(selector, out var index))
        {
            return info.Tracks.FirstOrDefault(track =>
                track.Type == type && (track.Index == index || track.Id == index));
        }

        return info.Tracks
            .Where(track => track.Type == type)
            .FirstOrDefault(track =>
                (!string.IsNullOrWhiteSpace(track.Language) &&
                 string.Equals(track.Language, selector, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(track.Title) &&
                 track.Title.Contains(selector, StringComparison.OrdinalIgnoreCase)));
    }
}
