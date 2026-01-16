// Summary: Applies configuration defaults and overrides to tool settings.
namespace Kitsub.Cli;

/// <summary>Applies configuration settings to command-line tool settings.</summary>
public static class ToolSettingsApplier
{
    public static void Apply(ToolSettings settings, AppConfig config)
    {
        settings.FfmpegPath ??= config.Tools.Ffmpeg;
        settings.FfprobePath ??= config.Tools.Ffprobe;
        settings.MkvmergePath ??= config.Tools.Mkvmerge;
        settings.MkvpropeditPath ??= config.Tools.Mkvpropedit;
        settings.MediainfoPath ??= config.Tools.Mediainfo;
        settings.PreferBundled ??= config.Tools.PreferBundled;
        settings.PreferPath ??= config.Tools.PreferPath;
        settings.ToolsCacheDir ??= config.Tools.ToolsCacheDir;

        settings.LogFile ??= config.Logging.LogFile;
        settings.LogLevel ??= config.Logging.LogLevel;

        if (settings.NoLog is null && config.Logging.Enabled.HasValue)
        {
            settings.NoLog = !config.Logging.Enabled.Value;
        }

        settings.NoBanner ??= config.Ui.NoBanner;
        settings.NoColor ??= config.Ui.NoColor;
        settings.Progress ??= config.Ui.Progress;

        settings.PreferBundled ??= true;
        settings.PreferPath ??= false;
        settings.LogLevel ??= "info";
        settings.LogFile ??= "logs/kitsub.log";
        settings.NoLog ??= false;
        settings.NoBanner ??= false;
        settings.NoColor ??= false;
        settings.Progress ??= UiProgressMode.Auto;
    }
}
