using FluentAssertions;
using Kitsub.Core;
using Kitsub.Tooling;
using Kitsub.Tooling.Provisioning;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Kitsub.Tests.Tooling;

public class FfprobeClientTests
{
    [Fact]
    public async Task ProbeAsync_WhenExitCodeNonZero_ThrowsExternalToolException()
    {
        var runner = Substitute.For<IExternalToolRunner>();
        var command = new ToolCommand("ffprobe", new List<string>());
        runner.CaptureAsync(command.Executable, Arg.Any<IReadOnlyList<string>>(), Arg.Any<ExternalToolRunOptions>(), Arg.Any<CancellationToken>())
            .Returns(new ExternalToolResult(1, string.Empty, "boom", "ffprobe"));

        var client = CreateClient(runner);

        var act = () => client.ProbeAsync("movie.mkv", CancellationToken.None);

        await act.Should().ThrowAsync<ExternalToolException>()
            .WithMessage("ffprobe failed");
    }

    [Fact]
    public async Task ProbeAsync_WhenJsonParses_ReturnsMappedMediaInfo()
    {
        var json = """
                   {
                     "streams": [
                       {
                         "index": 0,
                         "codec_name": "h264",
                         "codec_type": "video",
                         "width": 1920,
                         "height": 1080,
                         "tags": { "language": "eng", "title": "Main" },
                         "disposition": { "default": 1, "forced": 0 }
                       },
                       {
                         "index": 1,
                         "codec_name": "aac",
                         "codec_type": "audio",
                         "channels": 6,
                         "sample_rate": "48000",
                         "tags": { "language": "jpn" }
                       },
                       {
                         "index": 2,
                         "codec_name": "subrip",
                         "codec_type": "subtitle",
                         "tags": { "title": "Signs" },
                         "disposition": { "default": 0, "forced": 1 }
                       }
                     ],
                     "format": {
                       "format_name": "matroska",
                       "duration": "123.456",
                       "size": "987654"
                     }
                   }
                   """;

        var runner = Substitute.For<IExternalToolRunner>();
        runner.CaptureAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<ExternalToolRunOptions>(), Arg.Any<CancellationToken>())
            .Returns(new ExternalToolResult(0, json, string.Empty, "ffprobe"));
        var client = CreateClient(runner);

        var result = await client.ProbeAsync("video.mkv", CancellationToken.None);

        result.FilePath.Should().Be("video.mkv");
        result.Container.Should().Be("matroska");
        result.Duration.Should().BeCloseTo(TimeSpan.FromSeconds(123.456), precision: TimeSpan.FromMilliseconds(1));
        result.SizeBytes.Should().Be(987654);
        result.Tracks.Should().HaveCount(3);

        var video = result.Tracks[0];
        video.Type.Should().Be(TrackType.Video);
        video.Language.Should().Be("eng");
        video.Title.Should().Be("Main");
        video.IsDefault.Should().BeTrue();
        video.IsForced.Should().BeFalse();
        video.Extra.Should().ContainKey("resolution").WhoseValue.Should().Be("1920x1080");

        var audio = result.Tracks[1];
        audio.Type.Should().Be(TrackType.Audio);
        audio.Language.Should().Be("jpn");
        audio.Extra.Should().ContainKey("channels").WhoseValue.Should().Be("6");
        audio.Extra.Should().ContainKey("sampleRate").WhoseValue.Should().Be("48000");

        var subtitle = result.Tracks[2];
        subtitle.Type.Should().Be(TrackType.Subtitle);
        subtitle.Title.Should().Be("Signs");
        subtitle.IsForced.Should().BeTrue();
    }

    [Fact]
    public void BuildProbeCommand_ShouldIncludeJsonFlagsAndInput()
    {
        var client = CreateClient(Substitute.For<IExternalToolRunner>());

        var command = client.BuildProbeCommand("movie.mp4");

        command.Executable.Should().Be("ffprobe");
        command.Arguments.Should().ContainInOrder("-print_format", "json", "-show_streams", "-show_format", "movie.mp4");
    }

    private static FfprobeClient CreateClient(IExternalToolRunner runner)
    {
        var paths = new ToolResolution(
            "win-x64",
            "1.0",
            new ToolPathResolution("ffmpeg", ToolSource.Path),
            new ToolPathResolution("ffprobe", ToolSource.Path),
            new ToolPathResolution("mkvmerge", ToolSource.Path),
            new ToolPathResolution("mkvpropedit", ToolSource.Path),
            new ToolPathResolution("mediainfo", ToolSource.Path));

        return new FfprobeClient(runner, paths, new ExternalToolRunOptions(), Substitute.For<ILogger<FfprobeClient>>());
    }
}
