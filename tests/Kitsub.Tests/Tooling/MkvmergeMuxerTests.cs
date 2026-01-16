using FluentAssertions;
using Kitsub.Core;
using Kitsub.Tests.Helpers;
using Kitsub.Tooling;
using Kitsub.Tooling.Provisioning;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Kitsub.Tests.Tooling;

public class MkvmergeMuxerTests
{
    [Fact]
    public void EnumerateFonts_WhenDirectoryMissing_ReturnsEmpty()
    {
        var fonts = MkvmergeMuxer.EnumerateFonts(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));

        fonts.Should().BeEmpty();
    }

    [Fact]
    public void EnumerateFonts_WhenFontsPresent_ReturnsOnlySupportedExtensions()
    {
        using var temp = new TempDirectory();
        var ttf = temp.CreateFile("Fonts/Regular.ttf");
        var otf = temp.CreateFile("Fonts/Noto.otf");
        temp.CreateFile("Fonts/readme.txt");

        var fonts = MkvmergeMuxer.EnumerateFonts(temp.Path);

        fonts.Should().BeEquivalentTo(new[] { ttf, otf });
    }

    [Fact]
    public void BuildMuxSubtitlesCommand_WhenMetadataPresent_AddsFlags()
    {
        var muxer = CreateMuxer();
        var subtitles = new[]
        {
            new SubtitleDescriptor("subtitles/episode1.ass", "eng", "Signs", true, false)
        };

        var command = muxer.BuildMuxSubtitlesCommand("input.mkv", subtitles, "output.mkv");

        command.Executable.Should().Be("mkvmerge");
        command.Arguments.Should().ContainInOrder("-o", "output.mkv", "input.mkv");
        command.Arguments.Should().Contain("--language");
        command.Arguments.Should().Contain("0:eng");
        command.Arguments.Should().Contain("--track-name");
        command.Arguments.Should().Contain("0:Signs");
        command.Arguments.Should().Contain("--default-track");
        command.Arguments.Should().Contain("0:yes");
        command.Arguments.Should().Contain("--forced-track");
        command.Arguments.Should().Contain("0:no");
        command.Arguments.Should().Contain("subtitles/episode1.ass");
    }

    [Fact]
    public void BuildAttachFontsCommand_WhenFontsPresent_AddsAttachFileFlags()
    {
        using var temp = new TempDirectory();
        var fontA = temp.CreateFile("Fonts/Regular.ttf");
        var fontB = temp.CreateFile("Fonts/More/font.otf");

        var muxer = CreateMuxer();
        var command = muxer.BuildAttachFontsCommand("input.mkv", temp.Path, "output.mkv");

        command.Arguments.Should().ContainInOrder("-o", "output.mkv", "input.mkv");
        command.Arguments.Should().Contain("--attach-file");
        command.Arguments.Should().Contain(fontA);
        command.Arguments.Should().Contain(fontB);
    }

    private static MkvmergeMuxer CreateMuxer()
    {
        var runner = Substitute.For<IExternalToolRunner>();
        var paths = new ToolResolution(
            "win-x64",
            "1.0",
            new ToolPathResolution("ffmpeg", ToolSource.Path),
            new ToolPathResolution("ffprobe", ToolSource.Path),
            new ToolPathResolution("mkvmerge", ToolSource.Path),
            new ToolPathResolution("mkvpropedit", ToolSource.Path),
            new ToolPathResolution("mediainfo", ToolSource.Path));

        return new MkvmergeMuxer(runner, paths, new ExternalToolRunOptions(), Substitute.For<ILogger<MkvmergeMuxer>>());
    }
}
