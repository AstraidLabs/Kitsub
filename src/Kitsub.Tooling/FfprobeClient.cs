// Summary: Provides ffprobe command execution and JSON parsing for media inspection.
using System.Globalization;
using System.Text.Json;
using Kitsub.Core;
using Kitsub.Tooling.Provisioning;
using Microsoft.Extensions.Logging;

namespace Kitsub.Tooling;

/// <summary>Executes ffprobe and maps probe results into media metadata.</summary>
public sealed class FfprobeClient
{
    private readonly IExternalToolRunner _runner;
    private readonly ToolResolution _paths;
    private readonly ExternalToolRunOptions _options;
    private readonly ILogger<FfprobeClient> _logger;

    /// <summary>Initializes a new instance with required dependencies.</summary>
    /// <param name="runner">The external tool runner used to execute commands.</param>
    /// <param name="paths">The configured tool paths.</param>
    /// <param name="options">The run options applied to tool execution.</param>
    /// <param name="logger">The logger used for diagnostic messages.</param>
    public FfprobeClient(
        IExternalToolRunner runner,
        ToolResolution paths,
        ExternalToolRunOptions options,
        ILogger<FfprobeClient> logger)
    {
        // Block: Store dependencies needed to build and run ffprobe commands.
        _runner = runner;
        _paths = paths;
        _options = options;
        _logger = logger;
    }

    /// <summary>Runs ffprobe for a file and parses the resulting media metadata.</summary>
    /// <param name="filePath">The media file path to probe.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The parsed media metadata.</returns>
    /// <exception cref="ExternalToolException">Thrown when ffprobe reports a failure.</exception>
    /// <exception cref="JsonException">Thrown when ffprobe output cannot be parsed.</exception>
    public async Task<MediaInfo> ProbeAsync(string filePath, CancellationToken cancellationToken)
    {
        // Block: Build and execute the ffprobe command for the target file.
        var command = BuildProbeCommand(filePath);
        var result = await _runner.CaptureAsync(command.Executable, command.Arguments, _options, cancellationToken)
            .ConfigureAwait(false);
        if (result.ExitCode != 0)
        {
            // Block: Translate non-zero exit codes into a tool-specific exception.
            throw new ExternalToolException("ffprobe failed", result);
        }

        // Block: Log successful probe and parse the JSON payload.
        _logger.LogDebug("Parsed ffprobe JSON for {FilePath}", filePath);
        return ParseMediaInfo(filePath, result.StandardOutput);
    }

    /// <summary>Builds the ffprobe command for inspecting a media file.</summary>
    /// <param name="filePath">The media file path to probe.</param>
    /// <returns>The constructed tool command.</returns>
    public ToolCommand BuildProbeCommand(string filePath)
    {
        // Block: Assemble ffprobe arguments for JSON output.
        var args = new List<string>
        {
            "-print_format", "json",
            "-show_streams",
            "-show_format",
            filePath
        };

        return new ToolCommand(_paths.Ffprobe.Path, args);
    }

    private static MediaInfo ParseMediaInfo(string filePath, string json)
    {
        // Block: Parse the ffprobe JSON and initialize the result containers.
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var tracks = new List<TrackInfo>();
        if (root.TryGetProperty("streams", out var streams))
        {
            // Block: Iterate through streams and map them into track metadata.
            foreach (var stream in streams.EnumerateArray())
            {
                var codecType = stream.GetProperty("codec_type").GetString();
                if (!TryMapTrackType(codecType, out var type))
                {
                    // Block: Skip streams that cannot be mapped to a known track type.
                    continue;
                }

                var extra = new Dictionary<string, string>();
                if (type == TrackType.Video)
                {
                    // Block: Capture video-specific metadata such as resolution.
                    if (stream.TryGetProperty("width", out var width) &&
                        stream.TryGetProperty("height", out var height))
                    {
                        extra["resolution"] = $"{width.GetInt32()}x{height.GetInt32()}";
                    }
                }

                if (type == TrackType.Audio)
                {
                    // Block: Capture audio-specific metadata such as channels and sample rate.
                    if (stream.TryGetProperty("channels", out var channels))
                    {
                        extra["channels"] = channels.GetInt32().ToString(CultureInfo.InvariantCulture);
                    }

                    if (stream.TryGetProperty("sample_rate", out var sampleRate))
                    {
                        extra["sampleRate"] = sampleRate.GetString() ?? string.Empty;
                    }
                }

                // Block: Extract optional language and title tags from stream metadata.
                stream.TryGetProperty("tags", out var tagsElement);
                var language = GetTagValue(tagsElement, "language");
                var title = GetTagValue(tagsElement, "title");

                var isDefault = false;
                var isForced = false;
                if (stream.TryGetProperty("disposition", out var disposition))
                {
                    // Block: Read default and forced disposition flags when present.
                    if (disposition.TryGetProperty("default", out var defaultProp))
                    {
                        isDefault = defaultProp.GetInt32() == 1;
                    }

                    if (disposition.TryGetProperty("forced", out var forcedProp))
                    {
                        isForced = forcedProp.GetInt32() == 1;
                    }
                }

                // Block: Assemble the track info record from parsed stream metadata.
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
            // Block: Map container-level metadata when format info is available.
            if (format.TryGetProperty("format_name", out var formatName))
            {
                container = formatName.GetString();
            }

            if (format.TryGetProperty("duration", out var durationProp) &&
                double.TryParse(durationProp.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var durationSeconds))
            {
                // Block: Parse the duration seconds into a TimeSpan.
                duration = TimeSpan.FromSeconds(durationSeconds);
            }

            if (format.TryGetProperty("size", out var sizeProp) &&
                long.TryParse(sizeProp.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var sizeVal))
            {
                // Block: Parse the size into a byte count.
                sizeBytes = sizeVal;
            }
        }

        // Block: Build the media info record with parsed metadata.
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
            // Block: Return the tag value when present on the stream.
            return value.GetString();
        }

        // Block: Return null when the tag is not available.
        return null;
    }

    private static bool TryMapTrackType(string? codecType, out TrackType type)
    {
        // Block: Map ffprobe codec types to application track types.
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
