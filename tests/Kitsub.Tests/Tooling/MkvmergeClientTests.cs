using FluentAssertions;
using Kitsub.Core;
using Kitsub.Tooling;
using Kitsub.Tooling.Provisioning;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Kitsub.Tests.Tooling;

public class MkvmergeClientTests
{
    [Fact]
    public async Task IdentifyAsync_WhenExitCodeNonZero_ThrowsExternalToolException()
    {
        var runner = Substitute.For<IExternalToolRunner>();
        runner.CaptureAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<ExternalToolRunOptions>(), Arg.Any<CancellationToken>())
            .Returns(new ExternalToolResult(2, string.Empty, "error", "mkvmerge"));
        var client = CreateClient(runner);

        var act = () => client.IdentifyAsync("movie.mkv", CancellationToken.None);

        await act.Should().ThrowAsync<ExternalToolException>()
            .WithMessage("mkvmerge failed");
    }

    [Fact]
    public async Task IdentifyAsync_WhenJsonParses_ReturnsMappedMediaInfo()
    {
        var json = """
                   {
                     "container": { "type": "Matroska" },
                     "file_size": 5555,
                     "duration": { "seconds": 98.5 },
                     "tracks": [
                       {
                         "id": 0,
                         "type": "video",
                         "codec": "H.264",
                         "properties": {
                           "pixel_dimensions": "1920x1080",
                           "default_track": true,
                           "forced_track": false,
                           "language": "eng",
                           "track_name": "Main"
                         }
                       },
                       {
                         "id": 1,
                         "type": "audio",
                         "codec": "AAC",
                         "properties": {
                           "audio_channels": 2,
                           "sampling_frequency": 48000,
                           "language": "jpn"
                         }
                       },
                       {
                         "id": 2,
                         "type": "subtitles",
                         "codec": "SubStationAlpha",
                         "properties": {
                           "language": "eng",
                           "track_name": "Signs"
                         }
                       }
                     ],
                     "attachments": [
                       { "file_name": "font.ttf", "content_type": "font/ttf", "size": 1234 }
                     ]
                   }
                   """;

        var runner = Substitute.For<IExternalToolRunner>();
        runner.CaptureAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<ExternalToolRunOptions>(), Arg.Any<CancellationToken>())
            .Returns(new ExternalToolResult(0, json, string.Empty, "mkvmerge"));
        var client = CreateClient(runner);

        var result = await client.IdentifyAsync("movie.mkv", CancellationToken.None);

        result.Container.Should().Be("Matroska");
        result.SizeBytes.Should().Be(5555);
        result.Duration.Should().Be(TimeSpan.FromSeconds(98.5));
        result.Tracks.Should().HaveCount(3);
        result.Attachments.Should().ContainSingle();

        var video = result.Tracks[0];
        video.Type.Should().Be(TrackType.Video);
        video.Extra.Should().ContainKey("resolution").WhoseValue.Should().Be("1920x1080");
        video.Language.Should().Be("eng");
        video.Title.Should().Be("Main");
        video.IsDefault.Should().BeTrue();

        var audio = result.Tracks[1];
        audio.Type.Should().Be(TrackType.Audio);
        audio.Extra.Should().ContainKey("channels").WhoseValue.Should().Be("2");
        audio.Extra.Should().ContainKey("sampleRate").WhoseValue.Should().Be("48000");

        var attachment = result.Attachments[0];
        attachment.FileName.Should().Be("font.ttf");
        attachment.MimeType.Should().Be("font/ttf");
        attachment.SizeBytes.Should().Be(1234);
    }

    [Fact]
    public void BuildIdentifyCommand_ShouldIncludeJsonFlagAndInput()
    {
        var client = CreateClient(Substitute.For<IExternalToolRunner>());

        var command = client.BuildIdentifyCommand("movie.mkv");

        command.Executable.Should().Be("mkvmerge");
        command.Arguments.Should().ContainInOrder("-J", "movie.mkv");
    }

    private static MkvmergeClient CreateClient(IExternalToolRunner runner)
    {
        var paths = new ToolResolution(
            "win-x64",
            "1.0",
            new ToolPathResolution("ffmpeg", ToolSource.Path),
            new ToolPathResolution("ffprobe", ToolSource.Path),
            new ToolPathResolution("mkvmerge", ToolSource.Path),
            new ToolPathResolution("mkvpropedit", ToolSource.Path));

        return new MkvmergeClient(runner, paths, new ExternalToolRunOptions(), Substitute.For<ILogger<MkvmergeClient>>());
    }
}
