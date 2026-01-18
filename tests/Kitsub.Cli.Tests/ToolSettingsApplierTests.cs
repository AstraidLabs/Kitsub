using FluentAssertions;
using Kitsub.Cli;
using Xunit;

namespace Kitsub.Cli.Tests;

public class ToolSettingsApplierTests
{
    [Fact]
    public void Apply_WhenSettingsUnset_PopulatesFromConfig()
    {
        var settings = new TestToolSettings();
        var config = new AppConfig
        {
            Tools = new ToolsConfig
            {
                Ffmpeg = "ffmpeg-path",
                PreferBundled = false,
                PreferPath = true,
                ToolsCacheDir = "/tmp/kitsub"
            },
            Logging = new LoggingConfig
            {
                Enabled = true,
                LogLevel = "debug",
                LogFile = "logs/custom.log"
            },
            Ui = new UiConfig
            {
                NoBanner = true,
                NoColor = true,
                Progress = UiProgressMode.On
            }
        };

        ToolSettingsApplier.Apply(settings, config);

        settings.FfmpegPath.Should().Be("ffmpeg-path");
        settings.PreferBundled.Should().BeFalse();
        settings.PreferPath.Should().BeTrue();
        settings.ToolsCacheDir.Should().Be("/tmp/kitsub");
        settings.LogLevel.Should().Be("debug");
        settings.LogFile.Should().Be("logs/custom.log");
        settings.NoLog.Should().BeFalse();
        settings.NoBanner.Should().BeTrue();
        settings.NoColor.Should().BeTrue();
        settings.Progress.Should().Be(UiProgressMode.On);
    }

    [Fact]
    public void Apply_WhenSettingsProvided_DoesNotOverrideExplicitValues()
    {
        var settings = new TestToolSettings
        {
            FfmpegPath = "manual-ffmpeg",
            PreferBundled = true,
            PreferPath = false,
            ToolsCacheDir = "/custom/cache",
            LogLevel = "warn",
            LogFile = "logs/override.log",
            NoLog = true,
            NoBanner = true,
            NoColor = true,
            Progress = UiProgressMode.Off
        };
        var config = AppConfigDefaults.CreateDefaults();

        ToolSettingsApplier.Apply(settings, config);

        settings.FfmpegPath.Should().Be("manual-ffmpeg");
        settings.PreferBundled.Should().BeTrue();
        settings.PreferPath.Should().BeFalse();
        settings.ToolsCacheDir.Should().Be("/custom/cache");
        settings.LogLevel.Should().Be("warn");
        settings.LogFile.Should().Be("logs/override.log");
        settings.NoLog.Should().BeTrue();
        settings.NoBanner.Should().BeTrue();
        settings.NoColor.Should().BeTrue();
        settings.Progress.Should().Be(UiProgressMode.Off);
    }

    [Fact]
    public void Apply_WhenConfigMissingValues_UsesFallbackDefaults()
    {
        var settings = new TestToolSettings();
        var config = new AppConfig
        {
            Tools = new ToolsConfig(),
            Logging = new LoggingConfig(),
            Ui = new UiConfig()
        };

        ToolSettingsApplier.Apply(settings, config);

        settings.PreferBundled.Should().BeTrue();
        settings.PreferPath.Should().BeFalse();
        settings.LogLevel.Should().Be("info");
        settings.LogFile.Should().Be("logs/kitsub.log");
        settings.NoLog.Should().BeFalse();
        settings.NoBanner.Should().BeFalse();
        settings.NoColor.Should().BeFalse();
        settings.Progress.Should().Be(UiProgressMode.Auto);
    }

    [Fact]
    public void Apply_WhenLoggingDisabled_ConfiguresNoLog()
    {
        var settings = new TestToolSettings();
        var config = new AppConfig
        {
            Logging = new LoggingConfig
            {
                Enabled = false
            }
        };

        ToolSettingsApplier.Apply(settings, config);

        settings.NoLog.Should().BeTrue();
    }

    private sealed class TestToolSettings : ToolSettings
    {
    }
}
