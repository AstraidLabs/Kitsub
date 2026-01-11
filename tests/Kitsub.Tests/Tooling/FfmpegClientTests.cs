using FluentAssertions;
using Kitsub.Tooling;
using Kitsub.Tooling.Provisioning;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Kitsub.Tests.Tooling;

public class FfmpegClientTests
{
    [Fact]
    public void BuildConvertSubtitleCommand_WhenSrtToAss_ReturnsCommand()
    {
        var client = CreateClient();

        var command = client.BuildConvertSubtitleCommand("input.srt", "output.ass");

        command.Executable.Should().Be("ffmpeg");
        command.Arguments.Should().ContainInOrder("-y", "-i", "input.srt", "output.ass");
    }

    [Fact]
    public void BuildConvertSubtitleCommand_WhenAssToSrt_Throws()
    {
        var client = CreateClient();

        var act = () => client.BuildConvertSubtitleCommand("input.ass", "output.srt");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("ASS to SRT conversion is not supported reliably. Use another tool or convert to ASS first.");
    }

    [Fact]
    public void BuildConvertSubtitleCommand_WhenUnsupportedExtension_Throws()
    {
        var client = CreateClient();

        var act = () => client.BuildConvertSubtitleCommand("input.vtt", "output.srt");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Unsupported subtitle conversion: .vtt -> .srt.");
    }

    [Fact]
    public void BuildBurnSubtitlesCommand_WhenFontsDirProvided_UsesFontsDirInFilter()
    {
        var client = CreateClient();
        var subtitlePath = Path.Combine("Subs", "episode 1.ass");
        var fontsDir = Path.Combine("Fonts", "Special");
        var fullSubtitlePath = Path.GetFullPath(subtitlePath);
        var fullFontsPath = Path.GetFullPath(fontsDir);

        var command = client.BuildBurnSubtitlesCommand("input.mkv", subtitlePath, "output.mkv", fontsDir, 18, "slow");

        var expectedSubtitle = FfmpegClient.EscapeFilterPath(fullSubtitlePath);
        var expectedFonts = FfmpegClient.EscapeFilterPath(fullFontsPath);
        command.Arguments.Should().Contain("-vf");
        command.Arguments.Should().Contain($"subtitles='{expectedSubtitle}':fontsdir='{expectedFonts}'");
    }

    [Fact]
    public void BuildBurnSubtitlesCommand_WhenFontsDirMissing_UsesSubtitleOnlyFilter()
    {
        var client = CreateClient();
        var subtitlePath = Path.Combine("Subs", "episode 1.ass");
        var fullSubtitlePath = Path.GetFullPath(subtitlePath);

        var command = client.BuildBurnSubtitlesCommand("input.mkv", subtitlePath, "output.mkv", null, 20, "medium");

        var expectedSubtitle = FfmpegClient.EscapeFilterPath(fullSubtitlePath);
        command.Arguments.Should().Contain("-vf");
        command.Arguments.Should().Contain($"subtitles='{expectedSubtitle}'");
    }

    private static FfmpegClient CreateClient()
    {
        var runner = Substitute.For<IExternalToolRunner>();
        var paths = new ToolResolution(
            "win-x64",
            "1.0",
            new ToolPathResolution("ffmpeg", ToolSource.Path),
            new ToolPathResolution("ffprobe", ToolSource.Path),
            new ToolPathResolution("mkvmerge", ToolSource.Path),
            new ToolPathResolution("mkvpropedit", ToolSource.Path));

        return new FfmpegClient(runner, paths, new ExternalToolRunOptions(), Substitute.For<ILogger<FfmpegClient>>());
    }
}
