using Nox.Core.Media;

namespace Nox.Core.Tests.Media;

public class SourcePickerTests
{
    private static SourceState Playing(string id) => new(id, IsPlaying: true);
    private static SourceState Paused(string id) => new(id, IsPlaying: false);

    [Fact]
    public void NoSources_ReturnsMinusOne()
    {
        Assert.Equal(-1, SourcePicker.Pick([], preferredId: null, windowsCurrent: -1));
    }

    [Fact]
    public void Automatic_PrefersThePlayingOne_OverPausedCurrent()
    {
        SourceState[] sources = [Paused("chrome"), Playing("musicbee")];
        Assert.Equal(1, SourcePicker.Pick(sources, preferredId: null, windowsCurrent: 0));
    }

    [Fact]
    public void Automatic_KeepsWindowsCurrent_WhenItIsPlaying()
    {
        SourceState[] sources = [Playing("chrome"), Playing("musicbee")];
        Assert.Equal(1, SourcePicker.Pick(sources, preferredId: null, windowsCurrent: 1));
    }

    [Fact]
    public void Automatic_NothingPlaying_UsesWindowsCurrent()
    {
        SourceState[] sources = [Paused("chrome"), Paused("musicbee")];
        Assert.Equal(1, SourcePicker.Pick(sources, preferredId: null, windowsCurrent: 1));
    }

    [Fact]
    public void Automatic_NothingPlayingAndNoCurrent_UsesFirst()
    {
        SourceState[] sources = [Paused("chrome"), Paused("musicbee")];
        Assert.Equal(0, SourcePicker.Pick(sources, preferredId: null, windowsCurrent: -1));
    }

    [Fact]
    public void Preferred_WinsEvenIfPaused()
    {
        SourceState[] sources = [Playing("chrome"), Paused("musicbee")];
        Assert.Equal(1, SourcePicker.Pick(sources, preferredId: "musicbee", windowsCurrent: 0));
    }

    [Fact]
    public void Preferred_WithSeveralSessions_PicksThePlayingOne()
    {
        SourceState[] sources = [Paused("chrome"), Playing("musicbee"), Playing("chrome")];
        Assert.Equal(2, SourcePicker.Pick(sources, preferredId: "chrome", windowsCurrent: 1));
    }

    [Fact]
    public void Preferred_NotOpen_FallsBackToAutomatic()
    {
        SourceState[] sources = [Paused("chrome"), Playing("musicbee")];
        Assert.Equal(1, SourcePicker.Pick(sources, preferredId: "spotify", windowsCurrent: 0));
    }
}
