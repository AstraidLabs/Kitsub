using FluentAssertions;
using Kitsub.Cli;
using Kitsub.Core;
using Xunit;

namespace Kitsub.Cli.Tests;

public class TrackSelectionValidationTests
{
    [Fact]
    public void ValidateTrackSelectorSyntax_WhenBlank_ReturnsError()
    {
        var result = ValidationHelpers.ValidateTrackSelectorSyntax(" ");

        result.Successful.Should().BeFalse();
        result.Message.Should().Be("Track selector is required. Fix: provide a track index, ID, language, or title.");
    }

    [Fact]
    public void ValidateTrackSelectorSyntax_WhenNegative_ReturnsError()
    {
        var result = ValidationHelpers.ValidateTrackSelectorSyntax("-1");

        result.Successful.Should().BeFalse();
        result.Message.Should().Be("Track selector must be 0 or greater. Fix: use a non-negative track index or ID.");
    }

    [Fact]
    public void ResolveTrackSelection_WhenTrackMissing_ThrowsValidationException()
    {
        var info = new MediaInfo
        {
            FilePath = "input.mkv",
            Tracks = new[]
            {
                new TrackInfo { Index = 0, Type = TrackType.Audio, Codec = "aac" }
            }
        };

        var act = () => ValidationHelpers.ResolveTrackSelection(info, TrackType.Subtitle, "0", rejectBitmapSubtitles: true, "input.mkv");

        act.Should().Throw<ValidationException>()
            .WithMessage("Subtitle track not found for selector \"0\". Fix: run `kitsub inspect input.mkv` to list tracks.");
    }

    [Fact]
    public void ResolveTrackSelection_WhenBitmapSubtitle_ThrowsValidationException()
    {
        var info = new MediaInfo
        {
            FilePath = "input.mkv",
            Tracks = new[]
            {
                new TrackInfo { Index = 0, Type = TrackType.Subtitle, Codec = "hdmv_pgs_subtitle" }
            }
        };

        var act = () => ValidationHelpers.ResolveTrackSelection(info, TrackType.Subtitle, "0", rejectBitmapSubtitles: true, "input.mkv");

        act.Should().Throw<ValidationException>()
            .WithMessage("Bitmap subtitles are not supported for this command. Fix: select a text-based subtitle track from `kitsub inspect input.mkv`.");
    }

    [Fact]
    public void ResolveTrackSelection_WhenMultipleMatches_ReturnsWarning()
    {
        var info = new MediaInfo
        {
            FilePath = "input.mkv",
            Tracks = new[]
            {
                new TrackInfo { Index = 0, Type = TrackType.Subtitle, Codec = "ass", Language = "en" },
                new TrackInfo { Index = 1, Type = TrackType.Subtitle, Codec = "ass", Language = "en" }
            }
        };

        var result = ValidationHelpers.ResolveTrackSelection(info, TrackType.Subtitle, "en", rejectBitmapSubtitles: false, "input.mkv");

        result.Warning.Should().Be("Selector \"en\" matched multiple Subtitle tracks; using the first match. Fix: use a numeric track index or ID from `kitsub inspect input.mkv`.");
    }
}
