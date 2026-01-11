using FluentAssertions;
using Kitsub.Core;
using Xunit;

namespace Kitsub.Tests.Core;

public class TrackSelectionTests
{
    [Fact]
    public void SelectTrack_WhenSelectorMatchesIndex_ReturnsTrack()
    {
        var info = new MediaInfo
        {
            Tracks =
            [
                new TrackInfo { Index = 0, Type = TrackType.Audio, Codec = "aac" },
                new TrackInfo { Index = 1, Type = TrackType.Subtitle, Codec = "srt" }
            ]
        };

        var track = TrackSelection.SelectTrack(info, TrackType.Audio, "0");

        track.Should().NotBeNull();
        track!.Index.Should().Be(0);
    }

    [Fact]
    public void SelectTrack_WhenSelectorMatchesId_ReturnsTrack()
    {
        var info = new MediaInfo
        {
            Tracks =
            [
                new TrackInfo { Index = 0, Id = 3, Type = TrackType.Audio, Codec = "aac" },
                new TrackInfo { Index = 1, Id = 9, Type = TrackType.Audio, Codec = "aac" }
            ]
        };

        var track = TrackSelection.SelectTrack(info, TrackType.Audio, "9");

        track.Should().NotBeNull();
        track!.Id.Should().Be(9);
    }

    [Fact]
    public void SelectTrack_WhenSelectorMatchesLanguage_ReturnsTrack()
    {
        var info = new MediaInfo
        {
            Tracks =
            [
                new TrackInfo { Index = 0, Type = TrackType.Audio, Language = "eng", Codec = "aac" },
                new TrackInfo { Index = 1, Type = TrackType.Audio, Language = "jpn", Codec = "aac" }
            ]
        };

        var track = TrackSelection.SelectTrack(info, TrackType.Audio, "JPN");

        track.Should().NotBeNull();
        track!.Language.Should().Be("jpn");
    }

    [Fact]
    public void SelectTrack_WhenSelectorMatchesTitleSubstring_ReturnsTrack()
    {
        var info = new MediaInfo
        {
            Tracks =
            [
                new TrackInfo { Index = 0, Type = TrackType.Subtitle, Title = "Full Signs", Codec = "srt" },
                new TrackInfo { Index = 1, Type = TrackType.Subtitle, Title = "Songs", Codec = "srt" }
            ]
        };

        var track = TrackSelection.SelectTrack(info, TrackType.Subtitle, "sign");

        track.Should().NotBeNull();
        track!.Title.Should().Be("Full Signs");
    }

    [Fact]
    public void SelectTrack_WhenSelectorDoesNotMatch_ReturnsNull()
    {
        var info = new MediaInfo
        {
            Tracks =
            [
                new TrackInfo { Index = 0, Type = TrackType.Audio, Language = "eng", Codec = "aac" }
            ]
        };

        var track = TrackSelection.SelectTrack(info, TrackType.Audio, "jpn");

        track.Should().BeNull();
    }
}
