using System.Globalization;

namespace Kitsub.Tooling;

public sealed class MkvpropeditClient
{
    private readonly IExternalToolRunner _runner;
    private readonly ToolPaths _paths;

    public MkvpropeditClient(IExternalToolRunner runner, ToolPaths paths)
    {
        _runner = runner;
        _paths = paths;
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

        var result = await _runner.CaptureAsync(command.Executable, command.Arguments, cancellationToken).ConfigureAwait(false);
        if (result.ExitCode != 0)
        {
            throw new ExternalToolException("mkvpropedit failed", result);
        }
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
