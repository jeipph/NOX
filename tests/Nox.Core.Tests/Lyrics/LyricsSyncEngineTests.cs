using Nox.Core.Lyrics;
using Nox.Core.Models;

namespace Nox.Core.Tests.Lyrics;

public class LyricsSyncEngineTests
{
    private static readonly LyricLine[] Lines =
    [
        new(TimeSpan.FromSeconds(5), "uno"),
        new(TimeSpan.FromSeconds(10), "dos"),
        new(TimeSpan.FromSeconds(15), "tres"),
    ];

    private static TimeSpan S(double seconds) => TimeSpan.FromSeconds(seconds);

    [Theory]
    [InlineData(0, -1)]
    [InlineData(4.99, -1)]
    [InlineData(5, 0)]
    [InlineData(9.5, 0)]
    [InlineData(10, 1)]
    [InlineData(14, 1)]
    [InlineData(15, 2)]
    [InlineData(300, 2)]
    public void IndexAt_FindsTheLineBeingSung(double position, int expected)
    {
        Assert.Equal(expected, LyricsSyncEngine.IndexAt(Lines, S(position), TimeSpan.Zero));
    }

    [Fact]
    public void PositiveOffset_ShowsLinesEarlier()
    {
        Assert.Equal(1, LyricsSyncEngine.IndexAt(Lines, S(9), S(1.5)));
    }

    [Fact]
    public void NegativeOffset_ShowsLinesLater()
    {
        Assert.Equal(0, LyricsSyncEngine.IndexAt(Lines, S(10.5), S(-1)));
    }

    [Fact]
    public void NoLines_ReturnsMinusOne()
    {
        Assert.Equal(-1, LyricsSyncEngine.IndexAt([], S(10), TimeSpan.Zero));
    }

    [Fact]
    public void TimeUntilNext_IsTheGapToTheNextLine()
    {
        Assert.Equal(S(2), LyricsSyncEngine.TimeUntilNext(Lines, S(8), TimeSpan.Zero));
        Assert.Equal(S(5), LyricsSyncEngine.TimeUntilNext(Lines, S(0), TimeSpan.Zero));
    }

    [Fact]
    public void TimeUntilNext_AfterLastLine_IsNull()
    {
        Assert.Null(LyricsSyncEngine.TimeUntilNext(Lines, S(20), TimeSpan.Zero));
    }

    [Theory]
    [InlineData(7, 5)]
    [InlineData(-9, -5)]
    [InlineData(2.5, 2.5)]
    public void ClampOffset_StaysWithinFiveSeconds(double offset, double expected)
    {
        Assert.Equal(S(expected), LyricsSyncEngine.ClampOffset(S(offset)));
    }
}
