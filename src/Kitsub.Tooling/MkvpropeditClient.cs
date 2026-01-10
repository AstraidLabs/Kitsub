using System.Globalization;
using Microsoft.Extensions.Logging;

namespace Kitsub.Tooling;

public sealed class MkvpropeditClient
{
    private readonly IExternalToolRunner _runner;
    private readonly ToolPaths _paths;
    private readonly ExternalToolRunOptions _options;
    private readonly ILogger<MkvpropeditClient> _logger;

    public MkvpropeditClient(
        IExternalToolRunner runner,
        ToolPaths paths,
        ExternalToolRunOptions options,
        ILogger<MkvpropeditClient> logger)
    {
        _runner = runner;
        _paths = paths;
        _options = options;
        _logger = logger;
    }

    public async Task SetTrackFlagsAsync(
        string filePath,
        int trackId,
        bool? isDefault,
        bool? isForced,
        string? language,
        string? name,
        CancellationToken cancellationToken)
    {
        var command = BuildSetTrackFlagsCommand(filePath, trackId, isDefault, isForced, language, name);

        var result = await _runner.CaptureAsync(command.Executable, command.Arguments, _options, cancellationToken)
            .ConfigureAwait(false);
        if (result.ExitCode != 0)
        {
            throw new ExternalToolException("mkvpropedit failed", result);
        }

        _logger.LogInformation("Updated mkv track flags for {FilePath}", filePath);
    }

    public ToolCommand BuildSetTrackFlagsCommand(
        string filePath,
        int trackId,
        bool? isDefault,
        bool? isForced,
        string? language,
        string? name)
    {
        var args = new List<string> { filePath, "--edit", $"track:@{trackId.ToString(CultureInfo.InvariantCulture)}" };

        if (isDefault.HasValue)
        {
            args.Add("--set");
            args.Add($"flag-default={(isDefault.Value ? 1 : 0)}");
        }

        if (isForced.HasValue)
        {
            args.Add("--set");
            args.Add($"flag-forced={(isForced.Value ? 1 : 0)}");
        }

        if (!string.IsNullOrWhiteSpace(language))
        {
            args.Add("--set");
            args.Add($"language={language}");
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            args.Add("--set");
            args.Add($"name={name}");
        }

        return new ToolCommand(_paths.Mkvpropedit, args);
    }
}
