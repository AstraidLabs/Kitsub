using System.Globalization;
using Kitsub.Core;

namespace Kitsub.Tooling;

public sealed class FfmpegClient
{
    private readonly IExternalToolRunner _runner;
    private readonly ToolPaths _paths;

    public FfmpegClient(IExternalToolRunner runner, ToolPaths paths)
    {
        _runner = runner;
        _paths = paths;
    }

    public async Task BurnSubtitlesAsync(
        string inputFile,
        string subtitleFile,
        string outputFile,
        string? fontsDir,
        int crf,
        string preset,
        CancellationToken cancellationToken)
    {
        var command = BuildBurnSubtitlesCommand(inputFile, subtitleFile, outputFile, fontsDir, crf, preset);
        var result = await _runner.CaptureAsync(command.Executable, command.Arguments, cancellationToken).ConfigureAwait(false);
        if (result.ExitCode != 0)
        {
            throw new ExternalToolException("ffmpeg burn subtitles failed", result);
        }
    }

    public async Task ExtractSubtitleAsync(
        string inputFile,
        int subtitleIndex,
        string outputFile,
        CancellationToken cancellationToken)
    {
        var command = BuildExtractSubtitleCommand(inputFile, subtitleIndex, outputFile);
        var result = await _runner.CaptureAsync(command.Executable, command.Arguments, cancellationToken).ConfigureAwait(false);
        if (result.ExitCode != 0)
        {
            throw new ExternalToolException("ffmpeg extract subtitle failed", result);
        }
    }

    public async Task ConvertSubtitleAsync(string inputFile, string outputFile, CancellationToken cancellationToken)
    {
        var command = BuildConvertSubtitleCommand(inputFile, outputFile);
        var result = await _runner.CaptureAsync(command.Executable, command.Arguments, cancellationToken).ConfigureAwait(false);
        if (result.ExitCode != 0)
        {
            throw new ExternalToolException("ffmpeg convert subtitle failed", result);
        }
    }

    public async Task ExtractAudioAsync(
        string inputFile,
        int audioIndex,
        string outputFile,
        CancellationToken cancellationToken)
    {
        var command = BuildExtractAudioCommand(inputFile, audioIndex, outputFile);
        var result = await _runner.CaptureAsync(command.Executable, command.Arguments, cancellationToken).ConfigureAwait(false);
        if (result.ExitCode != 0)
        {
            throw new ExternalToolException("ffmpeg extract audio failed", result);
        }
    }

    public async Task ExtractVideoAsync(
        string inputFile,
        string outputFile,
        CancellationToken cancellationToken)
    {
        var command = BuildExtractVideoCommand(inputFile, outputFile);
        var result = await _runner.CaptureAsync(command.Executable, command.Arguments, cancellationToken).ConfigureAwait(false);
        if (result.ExitCode != 0)
        {
            throw new ExternalToolException("ffmpeg extract video failed", result);
        }
    }

    public ToolCommand BuildBurnSubtitlesCommand(
        string inputFile,
        string subtitleFile,
        string outputFile,
        string? fontsDir,
        int crf,
        string preset)
    {
        var filter = BuildSubtitlesFilter(subtitleFile, fontsDir);
        var args = new List<string>
        {
            "-y",
            "-i", inputFile,
            "-vf", filter,
            "-c:v", "libx264",
            "-crf", crf.ToString(CultureInfo.InvariantCulture),
            "-preset", preset,
            "-c:a", "copy",
            outputFile
        };

        return new ToolCommand(_paths.Ffmpeg, args);
    }

    public ToolCommand BuildExtractSubtitleCommand(string inputFile, int subtitleIndex, string outputFile)
    {
        var args = new List<string>
        {
            "-y",
            "-i", inputFile,
            "-map", $"0:s:{subtitleIndex}",
            "-c", "copy",
            outputFile
        };

        return new ToolCommand(_paths.Ffmpeg, args);
    }

    public ToolCommand BuildConvertSubtitleCommand(string inputFile, string outputFile)
    {
        var inputExtension = Path.GetExtension(inputFile).ToLowerInvariant();
        var outputExtension = Path.GetExtension(outputFile).ToLowerInvariant();

        if (inputExtension == ".srt" && outputExtension == ".ass")
        {
            var args = new List<string> { "-y", "-i", inputFile, outputFile };
            return new ToolCommand(_paths.Ffmpeg, args);
        }

        if (inputExtension == ".ass" && outputExtension == ".srt")
        {
            throw new InvalidOperationException("ASS to SRT conversion is not supported reliably. Use another tool or convert to ASS first.");
        }

        throw new InvalidOperationException($"Unsupported subtitle conversion: {inputExtension} -> {outputExtension}.");
    }

    public ToolCommand BuildExtractAudioCommand(string inputFile, int audioIndex, string outputFile)
    {
        var args = new List<string>
        {
            "-y",
            "-i", inputFile,
            "-map", $"0:a:{audioIndex}",
            "-c", "copy",
            outputFile
        };

        return new ToolCommand(_paths.Ffmpeg, args);
    }

    public ToolCommand BuildExtractVideoCommand(string inputFile, string outputFile)
    {
        var args = new List<string>
        {
            "-y",
            "-i", inputFile,
            "-map", "0:v:0",
            "-c", "copy",
            outputFile
        };

        return new ToolCommand(_paths.Ffmpeg, args);
    }

    private static string BuildSubtitlesFilter(string subtitleFile, string? fontsDir)
    {
        var fullPath = Path.GetFullPath(subtitleFile);
        var escapedPath = EscapeFilterPath(fullPath);
        if (string.IsNullOrWhiteSpace(fontsDir))
        {
            return $"subtitles='{escapedPath}'";
        }

        var fontsPath = EscapeFilterPath(Path.GetFullPath(fontsDir));
        return $"subtitles='{escapedPath}':fontsdir='{fontsPath}'";
    }

    private static string EscapeFilterPath(string path)
    {
        var normalized = path.Replace("\\", "/");
        normalized = normalized.Replace(":", "\\:");
        normalized = normalized.Replace("'", "\\'");
        return normalized;
    }
}
