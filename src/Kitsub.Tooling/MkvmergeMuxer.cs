using Kitsub.Core;

namespace Kitsub.Tooling;

public sealed class MkvmergeMuxer
{
    private static readonly string[] FontExtensions =
    [
        ".ttf",
        ".otf",
        ".ttc",
        ".woff",
        ".woff2"
    ];

    private readonly IExternalToolRunner _runner;
    private readonly ToolPaths _paths;

    public MkvmergeMuxer(IExternalToolRunner runner, ToolPaths paths)
    {
        _runner = runner;
        _paths = paths;
    }

    public async Task MuxSubtitlesAsync(
        string inputMkv,
        IReadOnlyList<SubtitleDescriptor> subtitles,
        string outputMkv,
        CancellationToken cancellationToken)
    {
        var command = BuildMuxSubtitlesCommand(inputMkv, subtitles, outputMkv);
        var result = await _runner.CaptureAsync(command.Executable, command.Arguments, cancellationToken).ConfigureAwait(false);
        if (result.ExitCode != 0)
        {
            throw new ExternalToolException("mkvmerge mux subtitles failed", result);
        }
    }

    public async Task AttachFontsAsync(
        string inputMkv,
        string fontsDir,
        string outputMkv,
        CancellationToken cancellationToken)
    {
        var command = BuildAttachFontsCommand(inputMkv, fontsDir, outputMkv);
        var result = await _runner.CaptureAsync(command.Executable, command.Arguments, cancellationToken).ConfigureAwait(false);
        if (result.ExitCode != 0)
        {
            throw new ExternalToolException("mkvmerge attach fonts failed", result);
        }
    }

    public ToolCommand BuildMuxSubtitlesCommand(
        string inputMkv,
        IReadOnlyList<SubtitleDescriptor> subtitles,
        string outputMkv)
    {
        var args = new List<string> { "-o", outputMkv, inputMkv };

        foreach (var subtitle in subtitles)
        {
            if (!string.IsNullOrWhiteSpace(subtitle.Language))
            {
                args.Add("--language");
                args.Add($"0:{subtitle.Language}");
            }

            if (!string.IsNullOrWhiteSpace(subtitle.Title))
            {
                args.Add("--track-name");
                args.Add($"0:{subtitle.Title}");
            }

            if (subtitle.IsDefault.HasValue)
            {
                args.Add("--default-track");
                args.Add($"0:{(subtitle.IsDefault.Value ? "yes" : "no")}");
            }

            if (subtitle.IsForced.HasValue)
            {
                args.Add("--forced-track");
                args.Add($"0:{(subtitle.IsForced.Value ? "yes" : "no")}");
            }

            args.Add(subtitle.FilePath);
        }

        return new ToolCommand(_paths.Mkvmerge, args);
    }

    public ToolCommand BuildAttachFontsCommand(string inputMkv, string fontsDir, string outputMkv)
    {
        var args = new List<string> { "-o", outputMkv, inputMkv };
        foreach (var fontFile in EnumerateFonts(fontsDir))
        {
            args.Add("--attach-file");
            args.Add(fontFile);
        }

        return new ToolCommand(_paths.Mkvmerge, args);
    }

    public static IReadOnlyList<string> EnumerateFonts(string fontsDir)
    {
        if (!Directory.Exists(fontsDir))
        {
            return Array.Empty<string>();
        }

        return Directory
            .EnumerateFiles(fontsDir, "*", SearchOption.AllDirectories)
            .Where(file => FontExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
            .ToList();
    }
}
