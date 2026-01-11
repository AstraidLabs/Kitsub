// Summary: Provides mkvpropedit command execution for updating MKV track metadata.
using System.Globalization;
using Kitsub.Tooling.Provisioning;
using Microsoft.Extensions.Logging;

namespace Kitsub.Tooling;

/// <summary>Executes mkvpropedit commands to update track flags and metadata.</summary>
public sealed class MkvpropeditClient
{
    private readonly IExternalToolRunner _runner;
    private readonly ToolResolution _paths;
    private readonly ExternalToolRunOptions _options;
    private readonly ILogger<MkvpropeditClient> _logger;

    /// <summary>Initializes a new instance with required dependencies.</summary>
    /// <param name="runner">The external tool runner used to execute commands.</param>
    /// <param name="paths">The configured tool paths.</param>
    /// <param name="options">The run options applied to tool execution.</param>
    /// <param name="logger">The logger used for diagnostic messages.</param>
    public MkvpropeditClient(
        IExternalToolRunner runner,
        ToolResolution paths,
        ExternalToolRunOptions options,
        ILogger<MkvpropeditClient> logger)
    {
        // Block: Store dependencies needed to build and run mkvpropedit commands.
        _runner = runner;
        _paths = paths;
        _options = options;
        _logger = logger;
    }

    /// <summary>Updates track flags and metadata for an MKV file.</summary>
    /// <param name="filePath">The MKV file path to edit.</param>
    /// <param name="trackId">The track identifier to update.</param>
    /// <param name="isDefault">The optional default flag value.</param>
    /// <param name="isForced">The optional forced flag value.</param>
    /// <param name="language">The optional language tag to set.</param>
    /// <param name="name">The optional track name to set.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <exception cref="ExternalToolException">Thrown when mkvpropedit reports a failure.</exception>
    public async Task SetTrackFlagsAsync(
        string filePath,
        int trackId,
        bool? isDefault,
        bool? isForced,
        string? language,
        string? name,
        CancellationToken cancellationToken)
    {
        // Block: Build and execute the mkvpropedit command to update track flags.
        var command = BuildSetTrackFlagsCommand(filePath, trackId, isDefault, isForced, language, name);

        var result = await _runner.CaptureAsync(command.Executable, command.Arguments, _options, cancellationToken)
            .ConfigureAwait(false);
        if (result.ExitCode != 0)
        {
            // Block: Translate non-zero exit codes into a tool-specific exception.
            throw new ExternalToolException("mkvpropedit failed", result);
        }

        // Block: Log successful track flag update.
        _logger.LogInformation("Updated mkv track flags for {FilePath}", filePath);
    }

    /// <summary>Builds the mkvpropedit command to set track flags and metadata.</summary>
    /// <param name="filePath">The MKV file path to edit.</param>
    /// <param name="trackId">The track identifier to update.</param>
    /// <param name="isDefault">The optional default flag value.</param>
    /// <param name="isForced">The optional forced flag value.</param>
    /// <param name="language">The optional language tag to set.</param>
    /// <param name="name">The optional track name to set.</param>
    /// <returns>The constructed tool command.</returns>
    public ToolCommand BuildSetTrackFlagsCommand(
        string filePath,
        int trackId,
        bool? isDefault,
        bool? isForced,
        string? language,
        string? name)
    {
        // Block: Build the base arguments for track editing.
        var args = new List<string> { filePath, "--edit", $"track:@{trackId.ToString(CultureInfo.InvariantCulture)}" };

        if (isDefault.HasValue)
        {
            // Block: Apply the default flag when provided.
            args.Add("--set");
            args.Add($"flag-default={(isDefault.Value ? 1 : 0)}");
        }

        if (isForced.HasValue)
        {
            // Block: Apply the forced flag when provided.
            args.Add("--set");
            args.Add($"flag-forced={(isForced.Value ? 1 : 0)}");
        }

        if (!string.IsNullOrWhiteSpace(language))
        {
            // Block: Apply the language tag when provided.
            args.Add("--set");
            args.Add($"language={language}");
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            // Block: Apply the track name when provided.
            args.Add("--set");
            args.Add($"name={name}");
        }

        return new ToolCommand(_paths.Mkvpropedit.Path, args);
    }
}
