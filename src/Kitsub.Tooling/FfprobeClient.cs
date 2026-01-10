using System.Globalization;
using System.Text.Json;
using Kitsub.Core;

namespace Kitsub.Tooling;

public sealed class FfprobeClient
{
    private readonly IExternalToolRunner _runner;
    private readonly ToolPaths _paths;

    public FfprobeClient(IExternalToolRunner runner, ToolPaths paths)
    {
        _runner = runner;
        _paths = paths;
    }

    public async Task<MediaInfo> ProbeAsync(string filePath, CancellationToken cancellationToken)
    {
        var command = BuildProbeCommand(filePath);
        var result = await _runner.CaptureAsync(command.Executable, command.Arguments, cancellationToken).ConfigureAwait(false);
        if (result.ExitCode != 0)
        {
            throw new ExternalToolException("ffprobe failed", result);
        }

        return ParseMediaInfo(filePath, result.StandardOutput);
    }

    public ToolCommand BuildProbeCommand(string filePath)
    {
        var args = new List<string>
        {
            "-print_format", "json",
            "-show_streams",
            "-show_format",
            filePath
        };

        return new ToolCommand(_paths.Ffprobe, args);
    }

    private static MediaInfo ParseMediaInfo(string filePath, string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var tracks = new List<TrackInfo>();
        if (root.TryGetProperty("streams", out var streams))
        {
            foreach (var stream in streams.EnumerateArray())
            {
                var codecType = stream.GetProperty("codec_type").GetString();
                if (!TryMapTrackType(codecType, out var type))
                {
                    continue;
                }

                var extra = new Dictionary<string, string>();
                if (type == TrackType.Video)
                {
                    if (stream.TryGetProperty("width", out var width) &&
                        stream.TryGetProperty("height", out var height))
                    {
                        extra["resolution"] = $"{width.GetInt32()}x{height.GetInt32()}";
                    }
                }

                if (type == TrackType.Audio)
                {
                    if (stream.TryGetProperty("channels", out var channels))
                    {
                        extra["channels"] = channels.GetInt32().ToString(CultureInfo.InvariantCulture);
                    }

                    if (stream.TryGetProperty("sample_rate", out var sampleRate))
                    {
                        extra["sampleRate"] = sampleRate.GetString() ?? string.Empty;
                    }
                }

                stream.TryGetProperty("tags", out var tagsElement);
                var language = GetTagValue(tagsElement, "language");
                var title = GetTagValue(tagsElement, "title");

                var isDefault = false;
                var isForced = false;
                if (stream.TryGetProperty("disposition", out var disposition))
                {
                    if (disposition.TryGetProperty("default", out var defaultProp))
                    {
                        isDefault = defaultProp.GetInt32() == 1;
                    }

                    if (disposition.TryGetProperty("forced", out var forcedProp))
                    {
                        isForced = forcedProp.GetInt32() == 1;
                    }
                }

                tracks.Add(new TrackInfo
                {
                    Index = stream.GetProperty("index").GetInt32(),
                    Id = stream.TryGetProperty("id", out var idProp) && idProp.TryGetInt32(out var idVal) ? idVal : null,
                    Type = type,
                    Codec = stream.GetProperty("codec_name").GetString() ?? string.Empty,
                    Language = language,
                    Title = title,
                    IsDefault = isDefault,
                    IsForced = isForced,
                    Extra = extra
                });
            }
        }

        string? container = null;
        TimeSpan? duration = null;
        long? sizeBytes = null;
        if (root.TryGetProperty("format", out var format))
        {
            if (format.TryGetProperty("format_name", out var formatName))
            {
                container = formatName.GetString();
            }

            if (format.TryGetProperty("duration", out var durationProp) &&
                double.TryParse(durationProp.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var durationSeconds))
            {
                duration = TimeSpan.FromSeconds(durationSeconds);
            }

            if (format.TryGetProperty("size", out var sizeProp) &&
                long.TryParse(sizeProp.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var sizeVal))
            {
                sizeBytes = sizeVal;
            }
        }

        return new MediaInfo
        {
            FilePath = filePath,
            Container = container,
            Duration = duration,
            SizeBytes = sizeBytes,
            Tracks = tracks,
            Attachments = Array.Empty<AttachmentInfo>()
        };
    }

    private static string? GetTagValue(JsonElement tagsElement, string key)
    {
        if (tagsElement.ValueKind == JsonValueKind.Object && tagsElement.TryGetProperty(key, out var value))
        {
            return value.GetString();
        }

        return null;
    }

    private static bool TryMapTrackType(string? codecType, out TrackType type)
    {
        switch (codecType)
        {
            case "video":
                type = TrackType.Video;
                return true;
            case "audio":
                type = TrackType.Audio;
                return true;
            case "subtitle":
                type = TrackType.Subtitle;
                return true;
            default:
                type = default;
                return false;
        }
    }
}
