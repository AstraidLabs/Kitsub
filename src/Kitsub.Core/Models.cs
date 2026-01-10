// Summary: Defines core media metadata models and helper methods used across the application.
namespace Kitsub.Core;

/// <summary>Represents the category of a media track.</summary>
public enum TrackType
{
    /// <summary>Represents a video track.</summary>
    Video,
    /// <summary>Represents an audio track.</summary>
    Audio,
    /// <summary>Represents a subtitle track.</summary>
    Subtitle
}

/// <summary>Represents an attachment embedded in a media container.</summary>
/// <param name="FileName">The attachment file name.</param>
/// <param name="MimeType">The attachment MIME type.</param>
/// <param name="SizeBytes">The attachment size in bytes.</param>
public sealed record AttachmentInfo(
    string FileName,
    string MimeType,
    long SizeBytes
);

/// <summary>Represents metadata for a media track.</summary>
public sealed record TrackInfo
{
    /// <summary>Gets the zero-based track index.</summary>
    public int Index { get; init; }
    /// <summary>Gets the optional container-specific track identifier.</summary>
    public int? Id { get; init; }
    /// <summary>Gets the track type.</summary>
    public TrackType Type { get; init; }
    /// <summary>Gets the codec name for the track.</summary>
    public string Codec { get; init; } = string.Empty;
    /// <summary>Gets the language tag for the track, if available.</summary>
    public string? Language { get; init; }
    /// <summary>Gets the track title, if available.</summary>
    public string? Title { get; init; }
    /// <summary>Gets a value indicating whether the track is marked as default.</summary>
    public bool IsDefault { get; init; }
    /// <summary>Gets a value indicating whether the track is marked as forced.</summary>
    public bool IsForced { get; init; }
    /// <summary>Gets additional metadata for the track.</summary>
    public IReadOnlyDictionary<string, string> Extra { get; init; } = new Dictionary<string, string>();
}

/// <summary>Represents metadata for a media file and its tracks.</summary>
public sealed record MediaInfo
{
    /// <summary>Gets the file path for the media file.</summary>
    public string FilePath { get; init; } = string.Empty;
    /// <summary>Gets the container format name, if available.</summary>
    public string? Container { get; init; }
    /// <summary>Gets the media duration, if available.</summary>
    public TimeSpan? Duration { get; init; }
    /// <summary>Gets the media file size in bytes, if available.</summary>
    public long? SizeBytes { get; init; }
    /// <summary>Gets the tracks contained in the media file.</summary>
    public IReadOnlyList<TrackInfo> Tracks { get; init; } = Array.Empty<TrackInfo>();
    /// <summary>Gets the attachments contained in the media file.</summary>
    public IReadOnlyList<AttachmentInfo> Attachments { get; init; } = Array.Empty<AttachmentInfo>();
}

/// <summary>Describes a subtitle file and its muxing metadata.</summary>
/// <param name="FilePath">The subtitle file path.</param>
/// <param name="Language">The optional language tag for the subtitle track.</param>
/// <param name="Title">The optional title for the subtitle track.</param>
/// <param name="IsDefault">The optional default flag for the subtitle track.</param>
/// <param name="IsForced">The optional forced flag for the subtitle track.</param>
public sealed record SubtitleDescriptor(
    string FilePath,
    string? Language,
    string? Title,
    bool? IsDefault,
    bool? IsForced
);

/// <summary>Provides helpers for selecting tracks from media metadata.</summary>
public static class TrackSelection
{
    /// <summary>Selects a track by index, identifier, language, or title.</summary>
    /// <param name="info">The media information containing track metadata.</param>
    /// <param name="type">The track type to select.</param>
    /// <param name="selector">The selector value for index, language, or title matching.</param>
    /// <returns>The matched track, or <c>null</c> when no match is found.</returns>
    public static TrackInfo? SelectTrack(MediaInfo info, TrackType type, string selector)
    {
        if (int.TryParse(selector, out var index))
        {
            // Block: Match by numeric track index or ID when the selector is numeric.
            return info.Tracks.FirstOrDefault(track =>
                track.Type == type && (track.Index == index || track.Id == index));
        }

        // Block: Match by language or title when the selector is text-based.
        return info.Tracks
            .Where(track => track.Type == type)
            .FirstOrDefault(track =>
                (!string.IsNullOrWhiteSpace(track.Language) &&
                 string.Equals(track.Language, selector, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(track.Title) &&
                 track.Title.Contains(selector, StringComparison.OrdinalIgnoreCase)));
    }
}
