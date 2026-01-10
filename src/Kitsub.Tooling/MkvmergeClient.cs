using System.Globalization;
using System.Text.Json;
using Kitsub.Core;

namespace Kitsub.Tooling;

public sealed class MkvmergeClient
{
    private readonly IExternalToolRunner _runner;
    private readonly ToolPaths _paths;

    public MkvmergeClient(IExternalToolRunner runner, ToolPaths paths)
    {
        _runner = runner;
        _paths = paths;
    }

    public async Task<MediaInfo> IdentifyAsync(string filePath, CancellationToken cancellationToken)
    {
        var command = BuildIdentifyCommand(filePath);
        var result = await _runner.CaptureAsync(command.Executable, command.Arguments, cancellationToken).ConfigureAwait(false);
        if (result.ExitCode != 0)
        {
            throw new ExternalToolException("mkvmerge failed", result);
        }

        return ParseMediaInfo(filePath, result.StandardOutput);
    }

    public ToolCommand BuildIdentifyCommand(string filePath)
    {
        var args = new List<string> { "-J", filePath };
        return new ToolCommand(_paths.Mkvmerge, args);
    }

    private static MediaInfo ParseMediaInfo(string filePath, string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var tracks = new List<TrackInfo>();
        if (root.TryGetProperty("tracks", out var tracksElement))
        {
            foreach (var track in tracksElement.EnumerateArray())
            {
                var typeValue = track.GetProperty("type").GetString();
                if (!TryMapTrackType(typeValue, out var type))
                {
                    continue;
                }

                var extra = new Dictionary<string, string>();
                var hasProperties = track.TryGetProperty("properties", out var properties);
                if (hasProperties)
                {
                    if (type == TrackType.Video && properties.TryGetProperty("pixel_dimensions", out var dims))
                    {
                        extra["resolution"] = dims.GetString() ?? string.Empty;
                    }

                    if (type == TrackType.Audio && properties.TryGetProperty("audio_channels", out var channels))
                    {
                        extra["channels"] = channels.GetInt32().ToString(CultureInfo.InvariantCulture);
                    }

                    if (type == TrackType.Audio && properties.TryGetProperty("sampling_frequency", out var freq))
                    {
                        extra["sampleRate"] = freq.GetInt32().ToString(CultureInfo.InvariantCulture);
                    }
                }

                var language = hasProperties && properties.TryGetProperty("language", out var languageProp) ? languageProp.GetString() : null;
                var title = hasProperties && properties.TryGetProperty("track_name", out var titleProp) ? titleProp.GetString() : null;
                var isDefault = hasProperties && properties.TryGetProperty("default_track", out var defaultProp) && defaultProp.GetBoolean();
                var isForced = hasProperties && properties.TryGetProperty("forced_track", out var forcedProp) && forcedProp.GetBoolean();

                tracks.Add(new TrackInfo
                {
                    Index = track.GetProperty("id").GetInt32(),
                    Id = track.GetProperty("id").GetInt32(),
                    Type = type,
                    Codec = track.GetProperty("codec").GetString() ?? string.Empty,
                    Language = language,
                    Title = title,
                    IsDefault = isDefault,
                    IsForced = isForced,
                    Extra = extra
                });
            }
        }

        var attachments = new List<AttachmentInfo>();
        if (root.TryGetProperty("attachments", out var attachmentsElement))
        {
            foreach (var attachment in attachmentsElement.EnumerateArray())
            {
                attachments.Add(new AttachmentInfo(
                    attachment.GetProperty("file_name").GetString() ?? string.Empty,
                    attachment.GetProperty("content_type").GetString() ?? string.Empty,
                    attachment.GetProperty("size").GetInt64()));
            }
        }

        string? container = null;
        TimeSpan? duration = null;
        long? sizeBytes = null;
        if (root.TryGetProperty("container", out var containerElement))
        {
            container = containerElement.GetProperty("type").GetString();
        }

        if (root.TryGetProperty("file_size", out var sizeElement))
        {
            sizeBytes = sizeElement.GetInt64();
        }

        if (root.TryGetProperty("duration", out var durationElement))
        {
            if (durationElement.TryGetProperty("seconds", out var secondsProp))
            {
                duration = TimeSpan.FromSeconds(secondsProp.GetDouble());
            }
        }

        return new MediaInfo
        {
            FilePath = filePath,
            Container = container,
            Duration = duration,
            SizeBytes = sizeBytes,
            Tracks = tracks,
            Attachments = attachments
        };
    }

    private static bool TryMapTrackType(string? typeValue, out TrackType type)
    {
        switch (typeValue)
        {
            case "video":
                type = TrackType.Video;
                return true;
            case "audio":
                type = TrackType.Audio;
                return true;
            case "subtitles":
                type = TrackType.Subtitle;
                return true;
            default:
                type = default;
                return false;
        }
    }
}
