using FluentAssertions;
using Kitsub.Tooling;
using Xunit;

namespace Kitsub.Tests.Tooling;

public class ExternalToolRunnerFormattingTests
{
    [Fact]
    public void RenderCommandLine_ShouldQuoteArgumentsWithSpaces()
    {
        var commandLine = ExternalToolRunner.RenderCommandLine(
            "C:/Program Files/ffmpeg.exe",
            new[] { "-i", "C:/Media Files/input.mkv", "-c", "copy" });

        commandLine.Should().Be("\"C:/Program Files/ffmpeg.exe\" -i \"C:/Media Files/input.mkv\" -c copy");
    }

    [Fact]
    public void RenderCommandLine_ShouldPreserveAllArguments()
    {
        var args = new[] { "-i", "input.mkv", "-map", "0:s:0", "output.ass" };

        var commandLine = ExternalToolRunner.RenderCommandLine("ffmpeg", args);

        commandLine.Should().Contain("-i");
        commandLine.Should().Contain("input.mkv");
        commandLine.Should().Contain("-map");
        commandLine.Should().Contain("0:s:0");
        commandLine.Should().Contain("output.ass");
    }

    [Fact]
    public void RenderCommandLine_ShouldEscapeQuotesInArguments()
    {
        var commandLine = ExternalToolRunner.RenderCommandLine(
            "ffmpeg",
            new[] { "-metadata", "title=He said \"Hello\"" });

        commandLine.Should().Be("ffmpeg -metadata \"title=He said \\\"Hello\\\"\"");
    }
}
