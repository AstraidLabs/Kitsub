using Kitsub.Tooling;
using Spectre.Console;

namespace Kitsub.Cli;

public static class ToolingFactory
{
    public static ToolPaths BuildToolPaths(ToolSettings settings)
    {
        return new ToolPaths(
            settings.FfmpegPath,
            settings.FfprobePath,
            settings.MkvmergePath,
            settings.MkvpropeditPath);
    }

    public static KitsubService CreateService(ToolSettings settings, IAnsiConsole console)
    {
        var paths = BuildToolPaths(settings);
        var runner = new CliExternalToolRunner(console, settings.DryRun, settings.Verbose);
        var ffprobe = new FfprobeClient(runner, paths);
        var mkvmerge = new MkvmergeClient(runner, paths);
        var mkvmergeMuxer = new MkvmergeMuxer(runner, paths);
        var ffmpeg = new FfmpegClient(runner, paths);
        return new KitsubService(ffprobe, mkvmerge, mkvmergeMuxer, ffmpeg);
    }
}
