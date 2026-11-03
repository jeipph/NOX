using Nox.Core.Pets;

namespace Nox.Core.Tests.Pets;

public sealed class BreakIntervalsTests
{
    [Fact]
    public void Presets_AreTheMenuOptions() =>
        Assert.Equal([15, 20, 30, 40, 45, 60, 90, 120], BreakIntervals.Presets);

    [Theory]
    [InlineData(25, true)]
    [InlineData(40, false)]
    [InlineData(0, false)] // «Nunca»
    [InlineData(480, true)]
    public void IsCustom_OnlyForValuesOutsideTheMenu(int minutes, bool expected) =>
        Assert.Equal(expected, BreakIntervals.IsCustom(minutes));

    [Theory]
    [InlineData("5", 5)]
    [InlineData(" 25 ", 25)]
    [InlineData("480", 480)]
    public void TryParse_AcceptsWholeMinutesInRange(string text, int expected)
    {
        Assert.True(BreakIntervals.TryParse(text, out int minutes, out string error));
        Assert.Equal(expected, minutes);
        Assert.Equal("", error);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("4")]
    [InlineData("481")]
    [InlineData("-10")]
    [InlineData("2,5")]
    [InlineData("veinte")]
    public void TryParse_RejectsTheRest_WithAReason(string? text)
    {
        Assert.False(BreakIntervals.TryParse(text, out int minutes, out string error));
        Assert.Equal(0, minutes);
        Assert.NotEmpty(error);
    }
}
