// Summary: Provides mkvmerge command execution and JSON parsing for MKV inspection.
using System.Globalization;
using System.Text.Json;
using Kitsub.Core;
using Microsoft.Extensions.Logging;

namespace Kitsub.Tooling;

/// <summary>Executes mkvmerge and maps identify results into media metadata.</summary>
public sealed class MkvmergeClient
{
    private readonly IExternalToolRunner _runner;
    private readonly Bundling.ToolPathsResolved _paths;
    private readonly ExternalToolRunOptions _options;
    private readonly ILogger<MkvmergeClient> _logger;

    /// <summary>Initializes a new instance with required dependencies.</summary>
    /// <param name="runner">The external tool runner used to execute commands.</param>
    /// <param name="paths">The configured tool paths.</param>
    /// <param name="options">The run options applied to tool execution.</param>
    /// <param name="logger">The logger used for diagnostic messages.</param>
    public MkvmergeClient(
        IExternalToolRunner runner,
        Bundling.ToolPathsResolved paths,
        ExternalToolRunOptions options,
        ILogger<MkvmergeClient> logger)
    {
        // Block: Store dependencies needed to build and run mkvmerge commands.
        _runner = runner;
        _paths = paths;
        _options = options;
        _logger = logger;
    }

    /// <summary>Runs mkvmerge to identify tracks and attachments for a file.</summary>
    /// <param name="filePath">The MKV file path to inspect.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The parsed media metadata.</returns>
    /// <exception cref="ExternalToolException">Thrown when mkvmerge reports a failure.</exception>
    /// <exception cref="JsonException">Thrown when mkvmerge output cannot be parsed.</exception>
    public async Task<MediaInfo> IdentifyAsync(string filePath, CancellationToken cancellationToken)
    {
        // Block: Build and execute the mkvmerge identify command.
        var command = BuildIdentifyCommand(filePath);
        var result = await _runner.CaptureAsync(command.Executable, command.Arguments, _options, cancellationToken)
            .ConfigureAwait(false);
        if (result.ExitCode != 0)
        {
            // Block: Translate non-zero exit codes into a tool-specific exception.
            throw new ExternalToolException("mkvmerge failed", result);
        }

        // Block: Log successful identify and parse the JSON payload.
        _logger.LogDebug("Parsed mkvmerge JSON for {FilePath}", filePath);
        return ParseMediaInfo(filePath, result.StandardOutput);
    }

    /// <summary>Builds the mkvmerge identify command for a file.</summary>
    /// <param name="filePath">The MKV file path to inspect.</param>
    /// <returns>The constructed tool command.</returns>
    public ToolCommand BuildIdentifyCommand(string filePath)
    {
        // Block: Assemble mkvmerge arguments for JSON output.
        var args = new List<string> { "-J", filePath };
        return new ToolCommand(_paths.Mkvmerge.Path, args);
    }

    private static MediaInfo ParseMediaInfo(string filePath, string json)
    {
        // Block: Parse the mkvmerge JSON and initialize the result containers.
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var tracks = new List<TrackInfo>();
        if (root.TryGetProperty("tracks", out var tracksElement))
        {
            // Block: Iterate through tracks and map them into track metadata.
            foreach (var track in tracksElement.EnumerateArray())
            {
                var typeValue = track.GetProperty("type").GetString();
                if (!TryMapTrackType(typeValue, out var type))
                {
                    // Block: Skip tracks that cannot be mapped to a known track type.
                    continue;
                }

                var extra = new Dictionary<string, string>();
                var hasProperties = track.TryGetProperty("properties", out var properties);
                if (hasProperties)
                {
                    // Block: Capture type-specific properties when present.
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

                // Block: Extract track metadata from the properties collection.
                var language = hasProperties && properties.TryGetProperty("language", out var languageProp) ? languageProp.GetString() : null;
                var title = hasProperties && properties.TryGetProperty("track_name", out var titleProp) ? titleProp.GetString() : null;
                var isDefault = hasProperties && properties.TryGetProperty("default_track", out var defaultProp) && defaultProp.GetBoolean();
                var isForced = hasProperties && properties.TryGetProperty("forced_track", out var forcedProp) && forcedProp.GetBoolean();

                // Block: Assemble the track info record from parsed metadata.
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
            // Block: Map attachment metadata when attachments are present.
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
            // Block: Capture container type information when available.
            container = containerElement.GetProperty("type").GetString();
        }

        if (root.TryGetProperty("file_size", out var sizeElement))
        {
            // Block: Capture file size in bytes when available.
            sizeBytes = sizeElement.GetInt64();
        }

        if (root.TryGetProperty("duration", out var durationElement))
        {
            // Block: Capture duration when the duration node is available.
            if (durationElement.TryGetProperty("seconds", out var secondsProp))
            {
                duration = TimeSpan.FromSeconds(secondsProp.GetDouble());
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
            Attachments = attachments
        };
    }

    private static bool TryMapTrackType(string? typeValue, out TrackType type)
    {
        // Block: Map mkvmerge track type strings to application track types.
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
