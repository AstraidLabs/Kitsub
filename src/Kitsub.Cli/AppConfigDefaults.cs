// Summary: Provides built-in configuration defaults for Kitsub.
namespace Kitsub.Cli;

/// <summary>Supplies built-in configuration defaults and sample templates.</summary>
public static class AppConfigDefaults
{
    public static AppConfig CreateDefaults()
    {
        return new AppConfig
        {
            ConfigVersion = 1,
            Tools = new ToolsConfig
            {
                PreferBundled = true,
                PreferPath = false,
                ToolsCacheDir = null,
                Ffmpeg = null,
                Ffprobe = null,
                Mkvmerge = null,
                Mkvpropedit = null
            },
            Logging = new LoggingConfig
            {
                Enabled = true,
                LogLevel = "info",
                LogFile = "logs/kitsub.log"
            },
            Ui = new UiConfig
            {
                NoBanner = false,
                NoColor = false,
                Progress = UiProgressMode.Auto
            },
            Defaults = new DefaultsConfig
            {
                Burn = new BurnDefaults
                {
                    Crf = 18,
                    Preset = "medium",
                    FontsDir = null
                },
                Mux = new MuxDefaults
                {
                    DefaultLanguage = null,
                    DefaultTrackName = null,
                    DefaultDefaultFlag = null,
                    DefaultForcedFlag = null
                }
            }
        };
    }
}
