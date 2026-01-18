// Summary: Provides mkvmerge-based muxing operations for subtitles and font attachments.
using Kitsub.Core;
using Kitsub.Tooling.Provisioning;
using Microsoft.Extensions.Logging;

namespace Kitsub.Tooling;

/// <summary>Executes mkvmerge operations for muxing subtitles and attaching fonts.</summary>
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
    private readonly ToolResolution _paths;
    private readonly ExternalToolRunOptions _options;
    private readonly ILogger<MkvmergeMuxer> _logger;

    /// <summary>Initializes a new instance with required dependencies.</summary>
    /// <param name="runner">The external tool runner used to execute commands.</param>
    /// <param name="paths">The configured tool paths.</param>
    /// <param name="options">The run options applied to tool execution.</param>
    /// <param name="logger">The logger used for diagnostic messages.</param>
    public MkvmergeMuxer(
        IExternalToolRunner runner,
        ToolResolution paths,
        ExternalToolRunOptions options,
        ILogger<MkvmergeMuxer> logger)
    {
        // Block: Store dependencies needed to build and run mkvmerge commands.
        _runner = runner;
        _paths = paths;
        _options = options;
        _logger = logger;
    }

    /// <summary>Muxes subtitle files into an MKV container.</summary>
    /// <param name="inputMkv">The input MKV file path.</param>
    /// <param name="subtitles">The subtitle descriptors to mux.</param>
    /// <param name="outputMkv">The output MKV file path.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <exception cref="ExternalToolException">Thrown when mkvmerge reports a failure.</exception>
    public async Task MuxSubtitlesAsync(
        string inputMkv,
        IReadOnlyList<SubtitleDescriptor> subtitles,
        string outputMkv,
        CancellationToken cancellationToken)
    {
        // Block: Build and execute the mkvmerge command to mux subtitles.
        var command = BuildMuxSubtitlesCommand(inputMkv, subtitles, outputMkv);
        var result = await _runner.CaptureAsync(command.Executable, command.Arguments, _options, cancellationToken)
            .ConfigureAwait(false);
        if (result.ExitCode != 0)
        {
            // Block: Translate non-zero exit codes into a tool-specific exception.
            throw new ExternalToolException("mkvmerge mux subtitles failed", result);
        }

        // Block: Log successful mux completion.
        _logger.LogInformation("Muxed subtitles into {Output}", outputMkv);
    }

    /// <summary>Attaches font files to an MKV container.</summary>
    /// <param name="inputMkv">The input MKV file path.</param>
    /// <param name="fontsDir">The directory containing font files.</param>
    /// <param name="outputMkv">The output MKV file path.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <exception cref="ExternalToolException">Thrown when mkvmerge reports a failure.</exception>
    public async Task AttachFontsAsync(
        string inputMkv,
        string fontsDir,
        string outputMkv,
        CancellationToken cancellationToken)
    {
        // Block: Build and execute the mkvmerge command to attach fonts.
        var command = BuildAttachFontsCommand(inputMkv, fontsDir, outputMkv);
        var result = await _runner.CaptureAsync(command.Executable, command.Arguments, _options, cancellationToken)
            .ConfigureAwait(false);
        if (result.ExitCode != 0)
        {
            // Block: Translate non-zero exit codes into a tool-specific exception.
            throw new ExternalToolException("mkvmerge attach fonts failed", result);
        }

        // Block: Log successful font attachment completion.
        _logger.LogInformation("Attached fonts into {Output}", outputMkv);
    }

    /// <summary>Builds the mkvmerge command for muxing subtitles.</summary>
    /// <param name="inputMkv">The input MKV file path.</param>
    /// <param name="subtitles">The subtitle descriptors to mux.</param>
    /// <param name="outputMkv">The output MKV file path.</param>
    /// <returns>The constructed tool command.</returns>
    public ToolCommand BuildMuxSubtitlesCommand(
        string inputMkv,
        IReadOnlyList<SubtitleDescriptor> subtitles,
        string outputMkv)
    {
        // Block: Build the base arguments for the mux operation.
        var args = new List<string> { "-o", outputMkv, inputMkv };

        foreach (var subtitle in subtitles)
        {
            // Block: Add optional metadata flags for each subtitle file.
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
                // Block: Apply default-track flag when explicitly configured.
                args.Add("--default-track");
                args.Add($"0:{(subtitle.IsDefault.Value ? "yes" : "no")}");
            }

            if (subtitle.IsForced.HasValue)
            {
                // Block: Apply forced-track flag when explicitly configured.
                args.Add("--forced-track");
                args.Add($"0:{(subtitle.IsForced.Value ? "yes" : "no")}");
            }

            // Block: Append the subtitle file path as the next input.
            args.Add(subtitle.FilePath);
        }

        return new ToolCommand(_paths.Mkvmerge.Path, args);
    }

    /// <summary>Builds the mkvmerge command for attaching fonts.</summary>
    /// <param name="inputMkv">The input MKV file path.</param>
    /// <param name="fontsDir">The directory containing font files.</param>
    /// <param name="outputMkv">The output MKV file path.</param>
    /// <returns>The constructed tool command.</returns>
    public ToolCommand BuildAttachFontsCommand(string inputMkv, string fontsDir, string outputMkv)
    {
        // Block: Build the base arguments for the attachment operation.
        var args = new List<string> { "-o", outputMkv, inputMkv };
        foreach (var fontFile in EnumerateFonts(fontsDir))
        {
            // Block: Attach each font file found under the provided directory.
            args.Add("--attach-file");
            args.Add(fontFile);
        }

        return new ToolCommand(_paths.Mkvmerge.Path, args);
    }

    /// <summary>Enumerates font files available in the specified directory.</summary>
    /// <param name="fontsDir">The directory to scan for font files.</param>
    /// <returns>The list of font file paths.</returns>
    public static IReadOnlyList<string> EnumerateFonts(string fontsDir)
    {
        if (!Directory.Exists(fontsDir))
        {
            // Block: Return an empty list when the fonts directory is missing.
            return Array.Empty<string>();
        }

        // Block: Recursively enumerate fonts with known extensions.
        var rootDir = Path.GetFullPath(fontsDir);
        return Directory
            .EnumerateFiles(rootDir, "*", SearchOption.AllDirectories)
            .Where(file => FontExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
            .Select(file =>
            {
                var relativePath = Path.GetRelativePath(rootDir, file)
                    .Replace(Path.DirectorySeparatorChar, '/');
                return Path.Combine(rootDir, relativePath);
            })
            .ToList();
    }
}
