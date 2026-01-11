using FluentAssertions;
using Kitsub.Tooling;
using Xunit;

namespace Kitsub.Tests.Tooling;

public class FfmpegPathEscapingTests
{
    [Fact]
    public void EscapeFilterPath_ShouldConvertBackslashesToForwardSlashes()
    {
        var path = "C:\\Videos\\Ep 01.mkv";

        var escaped = FfmpegClient.EscapeFilterPath(path);

        escaped.Should().Be("C\\:/Videos/Ep 01.mkv");
    }

    [Fact]
    public void EscapeFilterPath_ShouldEscapeColonForWindowsDrive()
    {
        var path = "C:\\Subs\\cz signs.ass";

        var escaped = FfmpegClient.EscapeFilterPath(path);

        escaped.Should().Be("C\\:/Subs/cz signs.ass");
    }

    [Fact]
    public void EscapeFilterPath_ShouldProduceStableOutputForTypicalPaths()
    {
        var videoPath = "C:\\Videos\\Ep 01.mkv";
        var subtitlePath = "C:\\Subs\\cz signs.ass";

        var escapedVideo = FfmpegClient.EscapeFilterPath(videoPath);
        var escapedSubtitle = FfmpegClient.EscapeFilterPath(subtitlePath);

        escapedVideo.Should().Be("C\\:/Videos/Ep 01.mkv");
        escapedSubtitle.Should().Be("C\\:/Subs/cz signs.ass");
    }
}
