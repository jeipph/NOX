using NotchBar.Core.Lyrics;

namespace NotchBar.Core.Tests.Lyrics;

public class LyricsMatchingTests
{
    private static readonly TimeSpan Song = TimeSpan.FromSeconds(180);

    [Theory]
    [InlineData(180, true)]
    [InlineData(182.9, true)]
    [InlineData(177, true)]
    [InlineData(184, false)]
    [InlineData(240, false)]
    public void IsSameLength_AllowsThreeSeconds(double candidate, bool expected)
    {
        Assert.Equal(expected, LyricsMatching.IsSameLength(Song, candidate));
    }

    [Fact]
    public void IsSameLength_UnknownSongDuration_AcceptsAnything()
    {
        Assert.True(LyricsMatching.IsSameLength(TimeSpan.Zero, 999));
    }

    [Fact]
    public void BestIndex_PrefersSynced_OverCloserPlain()
    {
        LyricsCandidate[] candidates = [new(180, false, false), new(182, true, false)];
        Assert.Equal(1, LyricsMatching.BestIndex(candidates, Song));
    }

    [Fact]
    public void BestIndex_AmongSynced_PrefersClosestDuration()
    {
        LyricsCandidate[] candidates = [new(182, true, false), new(180.5, true, false)];
        Assert.Equal(1, LyricsMatching.BestIndex(candidates, Song));
    }

    [Fact]
    public void BestIndex_IgnoresOtherVersions()
    {
        LyricsCandidate[] candidates = [new(240, true, false), new(181, false, false)];
        Assert.Equal(1, LyricsMatching.BestIndex(candidates, Song));
    }

    [Fact]
    public void BestIndex_NothingFits_ReturnsMinusOne()
    {
        Assert.Equal(-1, LyricsMatching.BestIndex([new(300, true, false)], Song));
    }
}
