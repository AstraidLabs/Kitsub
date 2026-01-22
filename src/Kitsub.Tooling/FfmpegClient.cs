// Summary: Provides ffmpeg command construction and execution helpers for media operations.
using System.Globalization;
using Kitsub.Tooling.Provisioning;
using Microsoft.Extensions.Logging;

namespace Kitsub.Tooling;

/// <summary>Executes ffmpeg commands for subtitle, audio, and video operations.</summary>
public sealed class FfmpegClient
{
    private readonly IExternalToolRunner _runner;
    private readonly ToolResolution _paths;
    private readonly ExternalToolRunOptions _options;
    private readonly ILogger<FfmpegClient> _logger;

    /// <summary>Initializes a new instance with required dependencies.</summary>
    /// <param name="runner">The external tool runner used to execute commands.</param>
    /// <param name="paths">The configured tool paths.</param>
    /// <param name="options">The run options applied to tool execution.</param>
    /// <param name="logger">The logger used for diagnostic messages.</param>
    public FfmpegClient(
        IExternalToolRunner runner,
        ToolResolution paths,
        ExternalToolRunOptions options,
        ILogger<FfmpegClient> logger)
    {
        // Block: Store dependencies needed to build and run ffmpeg commands.
        _runner = runner;
        _paths = paths;
        _options = options;
        _logger = logger;
    }

    /// <summary>Burns subtitles into a video using ffmpeg.</summary>
    /// <param name="inputFile">The source media file path.</param>
    /// <param name="subtitleFile">The subtitle file path.</param>
    /// <param name="outputFile">The output media file path.</param>
    /// <param name="fontsDir">The optional fonts directory.</param>
    /// <param name="crf">The constant rate factor used for encoding.</param>
    /// <param name="preset">The encoder preset.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <exception cref="ExternalToolException">Thrown when ffmpeg reports a failure.</exception>
    public async Task BurnSubtitlesAsync(
        string inputFile,
        string subtitleFile,
        string outputFile,
        string? fontsDir,
        int crf,
        string preset,
        CancellationToken cancellationToken)
    {
        // Block: Build and execute the ffmpeg command to burn subtitles.
        var command = BuildBurnSubtitlesCommand(inputFile, subtitleFile, outputFile, fontsDir, crf, preset);
        var result = await _runner.CaptureAsync(command.Executable, command.Arguments, _options, cancellationToken)
            .ConfigureAwait(false);
        if (result.ExitCode != 0)
        {
            // Block: Translate non-zero exit codes into a tool-specific exception.
            throw new ExternalToolException("ffmpeg burn subtitles failed", result);
        }

        // Block: Log successful subtitle burn completion.
        _logger.LogInformation("Burned subtitles into {Output}", outputFile);
    }

    /// <summary>Extracts a subtitle track to a file using ffmpeg.</summary>
    /// <param name="inputFile">The source media file path.</param>
    /// <param name="subtitleIndex">The subtitle stream index.</param>
    /// <param name="outputFile">The output subtitle file path.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <exception cref="ExternalToolException">Thrown when ffmpeg reports a failure.</exception>
    public async Task ExtractSubtitleAsync(
        string inputFile,
        int subtitleIndex,
        string outputFile,
        CancellationToken cancellationToken)
    {
        // Block: Build and execute the ffmpeg command to extract subtitles.
        var command = BuildExtractSubtitleCommand(inputFile, subtitleIndex, outputFile);
        var result = await _runner.CaptureAsync(command.Executable, command.Arguments, _options, cancellationToken)
            .ConfigureAwait(false);
        if (result.ExitCode != 0)
        {
            // Block: Translate non-zero exit codes into a tool-specific exception.
            throw new ExternalToolException("ffmpeg extract subtitle failed", result);
        }

        // Block: Log successful subtitle extraction completion.
        _logger.LogInformation("Extracted subtitles to {Output}", outputFile);
    }

    /// <summary>Converts a subtitle file between supported formats using ffmpeg.</summary>
    /// <param name="inputFile">The source subtitle file path.</param>
    /// <param name="outputFile">The output subtitle file path.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <exception cref="ExternalToolException">Thrown when ffmpeg reports a failure.</exception>
    public async Task ConvertSubtitleAsync(string inputFile, string outputFile, CancellationToken cancellationToken)
    {
        // Block: Build and execute the ffmpeg command to convert subtitles.
        var command = BuildConvertSubtitleCommand(inputFile, outputFile);
        var result = await _runner.CaptureAsync(command.Executable, command.Arguments, _options, cancellationToken)
            .ConfigureAwait(false);
        if (result.ExitCode != 0)
        {
            // Block: Translate non-zero exit codes into a tool-specific exception.
            throw new ExternalToolException("ffmpeg convert subtitle failed", result);
        }

        // Block: Log successful subtitle conversion completion.
        _logger.LogInformation("Converted subtitles to {Output}", outputFile);
    }

    /// <summary>Extracts an audio track to a file using ffmpeg.</summary>
    /// <param name="inputFile">The source media file path.</param>
    /// <param name="audioIndex">The audio stream index.</param>
    /// <param name="outputFile">The output audio file path.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <exception cref="ExternalToolException">Thrown when ffmpeg reports a failure.</exception>
    public async Task ExtractAudioAsync(
        string inputFile,
        int audioIndex,
        string outputFile,
        CancellationToken cancellationToken)
    {
        // Block: Build and execute the ffmpeg command to extract audio.
        var command = BuildExtractAudioCommand(inputFile, audioIndex, outputFile);
        var result = await _runner.CaptureAsync(command.Executable, command.Arguments, _options, cancellationToken)
            .ConfigureAwait(false);
        if (result.ExitCode != 0)
        {
            // Block: Translate non-zero exit codes into a tool-specific exception.
            throw new ExternalToolException("ffmpeg extract audio failed", result);
        }

        // Block: Log successful audio extraction completion.
        _logger.LogInformation("Extracted audio to {Output}", outputFile);
    }

    /// <summary>Extracts the primary video stream to a file using ffmpeg.</summary>
    /// <param name="inputFile">The source media file path.</param>
    /// <param name="outputFile">The output video file path.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <exception cref="ExternalToolException">Thrown when ffmpeg reports a failure.</exception>
    public async Task ExtractVideoAsync(
        string inputFile,
        string outputFile,
        CancellationToken cancellationToken)
    {
        // Block: Build and execute the ffmpeg command to extract video.
        var command = BuildExtractVideoCommand(inputFile, outputFile);
        var result = await _runner.CaptureAsync(command.Executable, command.Arguments, _options, cancellationToken)
            .ConfigureAwait(false);
        if (result.ExitCode != 0)
        {
            // Block: Translate non-zero exit codes into a tool-specific exception.
            throw new ExternalToolException("ffmpeg extract video failed", result);
        }

        // Block: Log successful video extraction completion.
        _logger.LogInformation("Extracted video to {Output}", outputFile);
    }

    /// <summary>Builds the ffmpeg command to burn subtitles into video.</summary>
    /// <param name="inputFile">The source media file path.</param>
    /// <param name="subtitleFile">The subtitle file path.</param>
    /// <param name="outputFile">The output media file path.</param>
    /// <param name="fontsDir">The optional fonts directory.</param>
    /// <param name="crf">The constant rate factor used for encoding.</param>
    /// <param name="preset">The encoder preset.</param>
    /// <returns>The constructed tool command.</returns>
    public ToolCommand BuildBurnSubtitlesCommand(
        string inputFile,
        string subtitleFile,
        string outputFile,
        string? fontsDir,
        int crf,
        string preset)
    {
        // Block: Build the subtitle filter and assemble ffmpeg arguments.
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

        return new ToolCommand(_paths.Ffmpeg.Path, args);
    }

    /// <summary>Builds the ffmpeg command to extract a subtitle stream.</summary>
    /// <param name="inputFile">The source media file path.</param>
    /// <param name="subtitleIndex">The subtitle stream index.</param>
    /// <param name="outputFile">The output subtitle file path.</param>
    /// <returns>The constructed tool command.</returns>
    public ToolCommand BuildExtractSubtitleCommand(string inputFile, int subtitleIndex, string outputFile)
    {
        // Block: Assemble ffmpeg arguments for subtitle extraction.
        var args = new List<string>
        {
            "-y",
            "-i", inputFile,
            "-map", $"0:s:{subtitleIndex}",
            "-c", "copy",
            outputFile
        };

        return new ToolCommand(_paths.Ffmpeg.Path, args);
    }

    /// <summary>Builds the ffmpeg command to convert subtitle formats.</summary>
    /// <param name="inputFile">The source subtitle file path.</param>
    /// <param name="outputFile">The output subtitle file path.</param>
    /// <returns>The constructed tool command.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the conversion format is unsupported.</exception>
    public ToolCommand BuildConvertSubtitleCommand(string inputFile, string outputFile)
    {
        // Block: Inspect file extensions to determine supported conversions.
        var inputExtension = Path.GetExtension(inputFile).ToLowerInvariant();
        var outputExtension = Path.GetExtension(outputFile).ToLowerInvariant();

        if (inputExtension == ".srt" && outputExtension == ".ass")
        {
            // Block: Support SRT to ASS conversion with a basic ffmpeg call.
            var args = new List<string> { "-y", "-i", inputFile, outputFile };
            return new ToolCommand(_paths.Ffmpeg.Path, args);
        }

        if (inputExtension == ".ass" && outputExtension == ".srt")
        {
            // Block: Reject ASS to SRT conversion to avoid quality loss.
            throw new InvalidOperationException("ASS to SRT conversion is not supported reliably. Fix: keep subtitles in ASS or export from your editor.");
        }

        // Block: Reject any other unsupported conversion combinations.
        throw new InvalidOperationException($"Unsupported subtitle conversion: {inputExtension} -> {outputExtension}. Fix: use .srt -> .ass.");
    }

    /// <summary>Builds the ffmpeg command to extract an audio stream.</summary>
    /// <param name="inputFile">The source media file path.</param>
    /// <param name="audioIndex">The audio stream index.</param>
    /// <param name="outputFile">The output audio file path.</param>
    /// <returns>The constructed tool command.</returns>
    public ToolCommand BuildExtractAudioCommand(string inputFile, int audioIndex, string outputFile)
    {
        // Block: Assemble ffmpeg arguments for audio extraction.
        var args = new List<string>
        {
            "-y",
            "-i", inputFile,
            "-map", $"0:a:{audioIndex}",
            "-c", "copy",
            outputFile
        };

        return new ToolCommand(_paths.Ffmpeg.Path, args);
    }

    /// <summary>Builds the ffmpeg command to extract the primary video stream.</summary>
    /// <param name="inputFile">The source media file path.</param>
    /// <param name="outputFile">The output video file path.</param>
    /// <returns>The constructed tool command.</returns>
    public ToolCommand BuildExtractVideoCommand(string inputFile, string outputFile)
    {
        // Block: Assemble ffmpeg arguments for video extraction.
        var args = new List<string>
        {
            "-y",
            "-i", inputFile,
            "-map", "0:v:0",
            "-c", "copy",
            outputFile
        };

        return new ToolCommand(_paths.Ffmpeg.Path, args);
    }

    private static string BuildSubtitlesFilter(string subtitleFile, string? fontsDir)
    {
        // Block: Normalize and escape paths for safe use in ffmpeg filters.
        var fullPath = Path.GetFullPath(subtitleFile);
        var escapedPath = EscapeFilterPath(fullPath);
        if (string.IsNullOrWhiteSpace(fontsDir))
        {
            // Block: Build a subtitles filter without fonts directory when absent.
            return $"subtitles='{escapedPath}'";
        }

        // Block: Build a subtitles filter with an explicit fonts directory.
        var fontsPath = EscapeFilterPath(Path.GetFullPath(fontsDir));
        return $"subtitles='{escapedPath}':fontsdir='{fontsPath}'";
    }

    internal static string EscapeFilterPath(string path)
    {
        // Block: Escape filter path characters required by ffmpeg filters.
        var normalized = path.Replace("\\", "/");
        normalized = normalized.Replace(":", "\\:");
        normalized = normalized.Replace("'", "\\'");
        return normalized;
    }
}
