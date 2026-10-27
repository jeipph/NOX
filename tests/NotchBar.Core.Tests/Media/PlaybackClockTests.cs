using NotchBar.Core.Media;
using NotchBar.Core.Models;

namespace NotchBar.Core.Tests.Media;

public class PlaybackClockTests
{
    private static readonly DateTimeOffset T0 = new(2026, 10, 27, 12, 0, 0, TimeSpan.Zero);

    private static MediaInfo Song(bool playing, double positionSeconds, double durationSeconds = 200) => new(
        "Título", "Artista", "Álbum",
        TimeSpan.FromSeconds(durationSeconds), TimeSpan.FromSeconds(positionSeconds), T0,
        playing, "test", null);

    [Fact]
    public void Paused_KeepsReportedPosition()
    {
        var position = PlaybackClock.EstimatePosition(Song(playing: false, 30), T0.AddSeconds(10));
        Assert.Equal(TimeSpan.FromSeconds(30), position);
    }

    [Fact]
    public void Playing_AddsElapsedTime()
    {
        var position = PlaybackClock.EstimatePosition(Song(playing: true, 30), T0.AddSeconds(10));
        Assert.Equal(TimeSpan.FromSeconds(40), position);
    }

    [Fact]
    public void Playing_NeverGoesPastTheEnd()
    {
        var position = PlaybackClock.EstimatePosition(Song(playing: true, 195), T0.AddSeconds(60));
        Assert.Equal(TimeSpan.FromSeconds(200), position);
    }

    [Fact]
    public void ClockBehindReport_DoesNotGoBackwards()
    {
        var position = PlaybackClock.EstimatePosition(Song(playing: true, 30), T0.AddSeconds(-5));
        Assert.Equal(TimeSpan.FromSeconds(30), position);
    }

    [Fact]
    public void Progress_IsFractionOfDuration()
    {
        Assert.Equal(0.25, PlaybackClock.Progress(Song(playing: false, 50), T0), precision: 6);
    }

    [Fact]
    public void Progress_UnknownDuration_IsZero()
    {
        Assert.Equal(0, PlaybackClock.Progress(Song(playing: true, 50, durationSeconds: 0), T0.AddSeconds(5)));
    }
}
