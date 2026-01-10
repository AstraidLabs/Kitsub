using Kitsub.Core;

namespace Kitsub.Tooling;

public sealed class KitsubService
{
    private readonly FfprobeClient _ffprobe;
    private readonly MkvmergeClient _mkvmerge;
    private readonly MkvmergeMuxer _mkvmergeMuxer;
    private readonly FfmpegClient _ffmpeg;

    public KitsubService(FfprobeClient ffprobe, MkvmergeClient mkvmerge, MkvmergeMuxer mkvmergeMuxer, FfmpegClient ffmpeg)
    {
        _ffprobe = ffprobe;
        _mkvmerge = mkvmerge;
        _mkvmergeMuxer = mkvmergeMuxer;
        _ffmpeg = ffmpeg;
    }

    public async Task<(MediaInfo Info, bool IsMkv)> InspectAsync(string filePath, CancellationToken cancellationToken)
    {
        if (Path.GetExtension(filePath).Equals(".mkv", StringComparison.OrdinalIgnoreCase))
        {
            return (await _mkvmerge.IdentifyAsync(filePath, cancellationToken).ConfigureAwait(false), true);
        }

        return (await _ffprobe.ProbeAsync(filePath, cancellationToken).ConfigureAwait(false), false);
    }

    public Task MuxSubtitlesAsync(
        string inputMkv,
        IReadOnlyList<SubtitleDescriptor> subtitles,
        string outputMkv,
        CancellationToken cancellationToken)
    {
        return _mkvmergeMuxer.MuxSubtitlesAsync(inputMkv, subtitles, outputMkv, cancellationToken);
    }

    public Task AttachFontsAsync(string inputMkv, string fontsDir, string outputMkv, CancellationToken cancellationToken)
    {
        return _mkvmergeMuxer.AttachFontsAsync(inputMkv, fontsDir, outputMkv, cancellationToken);
    }

    public async Task<(bool HasFonts, bool HasAssSubtitles, MediaInfo Info)> CheckFontsAsync(
        string inputMkv,
        CancellationToken cancellationToken)
    {
        var info = await _mkvmerge.IdentifyAsync(inputMkv, cancellationToken).ConfigureAwait(false);
        var hasFonts = info.Attachments.Any(attachment =>
            attachment.FileName.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase) ||
            attachment.FileName.EndsWith(".otf", StringComparison.OrdinalIgnoreCase) ||
            attachment.FileName.EndsWith(".ttc", StringComparison.OrdinalIgnoreCase) ||
            attachment.FileName.EndsWith(".woff", StringComparison.OrdinalIgnoreCase) ||
            attachment.FileName.EndsWith(".woff2", StringComparison.OrdinalIgnoreCase));

        var hasAss = info.Tracks.Any(track =>
            track.Type == TrackType.Subtitle &&
            (track.Codec.Contains("SubStationAlpha", StringComparison.OrdinalIgnoreCase) ||
             track.Codec.Contains("ASS", StringComparison.OrdinalIgnoreCase) ||
             track.Codec.Contains("SSA", StringComparison.OrdinalIgnoreCase)));

        return (hasFonts, hasAss, info);
    }

    public Task BurnSubtitlesAsync(
        string inputFile,
        string subtitleFile,
        string outputFile,
        string? fontsDir,
        int crf,
        string preset,
        CancellationToken cancellationToken)
    {
        return _ffmpeg.BurnSubtitlesAsync(inputFile, subtitleFile, outputFile, fontsDir, crf, preset, cancellationToken);
    }

    public async Task ExtractAudioAsync(string inputFile, string selector, string outputFile, CancellationToken cancellationToken)
    {
        var info = await _ffprobe.ProbeAsync(inputFile, cancellationToken).ConfigureAwait(false);
        var track = TrackSelection.SelectTrack(info, TrackType.Audio, selector);
        if (track is null)
        {
            throw new InvalidOperationException("Audio track not found.");
        }

        var audioIndex = GetTypeIndex(info, track);
        await _ffmpeg.ExtractAudioAsync(inputFile, audioIndex, outputFile, cancellationToken).ConfigureAwait(false);
    }

    public async Task ExtractVideoAsync(string inputFile, string outputFile, CancellationToken cancellationToken)
    {
        await _ffmpeg.ExtractVideoAsync(inputFile, outputFile, cancellationToken).ConfigureAwait(false);
    }

    public async Task ExtractSubtitleAsync(string inputFile, string selector, string outputFile, CancellationToken cancellationToken)
    {
        var info = await _ffprobe.ProbeAsync(inputFile, cancellationToken).ConfigureAwait(false);
        var track = TrackSelection.SelectTrack(info, TrackType.Subtitle, selector);
        if (track is null)
        {
            throw new InvalidOperationException("Subtitle track not found.");
        }

        if (IsBitmapSubtitle(track))
        {
            throw new InvalidOperationException("Bitmap subtitles not supported for extraction to text.");
        }

        var subtitleIndex = GetTypeIndex(info, track);
        await _ffmpeg.ExtractSubtitleAsync(inputFile, subtitleIndex, outputFile, cancellationToken).ConfigureAwait(false);
    }

    public async Task ConvertSubtitleAsync(string inputFile, string outputFile, CancellationToken cancellationToken)
    {
        await _ffmpeg.ConvertSubtitleAsync(inputFile, outputFile, cancellationToken).ConfigureAwait(false);
    }

    public async Task<string> ExtractSubtitleToTempAsync(string inputFile, string selector, CancellationToken cancellationToken)
    {
        var info = await _ffprobe.ProbeAsync(inputFile, cancellationToken).ConfigureAwait(false);
        var track = TrackSelection.SelectTrack(info, TrackType.Subtitle, selector);
        if (track is null)
        {
            throw new InvalidOperationException("Subtitle track not found.");
        }

        if (IsBitmapSubtitle(track))
        {
            throw new InvalidOperationException("Bitmap subtitles not supported for extraction to text.");
        }

        var extension = track.Codec.Contains("ass", StringComparison.OrdinalIgnoreCase)
            ? ".ass"
            : ".srt";
        var tempFile = Path.Combine(Path.GetTempPath(), $"kitsub_{Guid.NewGuid():N}{extension}");
        var subtitleIndex = GetTypeIndex(info, track);
        await _ffmpeg.ExtractSubtitleAsync(inputFile, subtitleIndex, tempFile, cancellationToken).ConfigureAwait(false);
        return tempFile;
    }

    private static int GetTypeIndex(MediaInfo info, TrackInfo track)
    {
        return info.Tracks
            .Where(t => t.Type == track.Type)
            .OrderBy(t => t.Index)
            .Select((t, index) => new { Track = t, Index = index })
            .First(pair => ReferenceEquals(pair.Track, track)).Index;
    }

    private static bool IsBitmapSubtitle(TrackInfo track)
    {
        return track.Codec.Contains("pgs", StringComparison.OrdinalIgnoreCase) ||
               track.Codec.Contains("dvd", StringComparison.OrdinalIgnoreCase) ||
               track.Codec.Contains("vobsub", StringComparison.OrdinalIgnoreCase) ||
               track.Codec.Contains("hdmv", StringComparison.OrdinalIgnoreCase);
    }
}
