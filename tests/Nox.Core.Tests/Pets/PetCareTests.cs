using Nox.Core.Pets;

namespace Nox.Core.Tests.Pets;

public sealed class PetCareTests
{
    private static readonly DateTime Start = new(2026, 11, 3, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void FirstTime_IsFed_AndClean()
    {
        var care = new PetCare(null, Start);

        Assert.False(care.IsHungry(Start));
        Assert.Equal(0, care.Poops);
        Assert.Equal(new PetCareState(Start, Start, 0), care.State);
    }

    [Fact]
    public void FourHoursWithoutFood_IsHungry()
    {
        var care = new PetCare(null, Start);

        Assert.False(care.IsHungry(Start.AddHours(3.9)));
        Assert.True(care.IsHungry(Start.AddHours(4)));
    }

    [Fact]
    public void Feed_TakesAwayHunger_AndCountsAgain()
    {
        var care = new PetCare(null, Start);
        DateTime later = Start.AddHours(5);

        care.Feed(later);

        Assert.False(care.IsHungry(later));
        Assert.True(care.IsHungry(later.AddHours(4)));
    }

    [Fact]
    public void OnePoopEveryFiveHours()
    {
        var care = new PetCare(null, Start);

        Assert.False(care.Update(Start.AddHours(4.9)));
        Assert.Equal(0, care.Poops);
        Assert.True(care.Update(Start.AddHours(5)));
        Assert.Equal(1, care.Poops);
        care.Update(Start.AddHours(10.5));
        Assert.Equal(2, care.Poops);
    }

    [Fact]
    public void PcWasOff_PoopsAreCountedAtStartup_UpToThree()
    {
        var saved = new PetCareState(Start, Start, 1);

        var care = new PetCare(saved, Start.AddDays(3));

        Assert.Equal(PetCare.MaxPoops, care.Poops);
        Assert.True(care.IsHungry(Start.AddDays(3)));
    }

    [Fact]
    public void FullFloor_DoesNotPileUp_AndCleaningRestartsTheCount()
    {
        var care = new PetCare(new PetCareState(Start, Start, 3), Start.AddDays(2));
        DateTime cleanedAt = Start.AddDays(2);

        care.Clean(cleanedAt);

        Assert.Equal(0, care.Poops);
        care.Update(cleanedAt.AddHours(4));
        Assert.Equal(0, care.Poops);
        care.Update(cleanedAt.AddHours(5));
        Assert.Equal(1, care.Poops);
    }

    [Fact]
    public void Update_KeepsTheRemainder_ForTheNextPoop()
    {
        var care = new PetCare(null, Start);

        care.Update(Start.AddHours(7)); // 1 popó y 2 h de cuenta para el siguiente
        care.Update(Start.AddHours(10));

        Assert.Equal(2, care.Poops);
    }

    [Fact]
    public void NextChange_IsTheSoonestOfHungerAndPoop()
    {
        var care = new PetCare(null, Start);

        Assert.Equal(Start.AddHours(4), care.NextChange(Start)); // antes el hambre (4 h) que el popó (5 h)

        care.Feed(Start.AddHours(3));
        Assert.Equal(Start.AddHours(5), care.NextChange(Start.AddHours(3))); // ahora el popó
    }

    [Fact]
    public void NextChange_IsNull_WhenHungryAndFloorFull()
    {
        var care = new PetCare(new PetCareState(Start, Start, 3), Start.AddHours(6));

        Assert.Null(care.NextChange(Start.AddHours(6)));
    }

    [Fact]
    public void ClockGoingBackwards_StartsCountingFromNow()
    {
        var care = new PetCare(new PetCareState(Start.AddHours(10), Start.AddHours(10), 0), Start);

        Assert.False(care.IsHungry(Start));
        Assert.Equal(Start, care.State.LastFedUtc);
        Assert.Equal(Start, care.State.PoopClockUtc);
    }

    [Fact]
    public void FakeMode_UsesSeconds()
    {
        var care = new PetCare(null, Start, TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(5));

        Assert.True(care.IsHungry(Start.AddSeconds(4)));
        care.Update(Start.AddSeconds(11));
        Assert.Equal(2, care.Poops);
    }

    [Fact]
    public void SavedPoops_OutOfRange_AreClamped()
    {
        Assert.Equal(PetCare.MaxPoops, new PetCare(new PetCareState(Start, Start, 99), Start).Poops);
        Assert.Equal(0, new PetCare(new PetCareState(Start, Start, -2), Start).Poops);
    }
}
