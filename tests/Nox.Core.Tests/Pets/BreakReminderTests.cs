using Nox.Core.Pets;

namespace Nox.Core.Tests.Pets;

public sealed class BreakReminderTests
{
    private static readonly DateTime Start = new(2026, 10, 29, 9, 0, 0);
    private static readonly TimeSpan Busy = TimeSpan.FromSeconds(10); // tocaste el ratón hace 10 s

    private static DateTime At(int minutes) => Start.AddMinutes(minutes);

    [Fact]
    public void Shows_After40MinutesOfWork_Once()
    {
        var reminder = new BreakReminder(TimeSpan.FromMinutes(40), Start);

        Assert.False(reminder.Check(At(39), Busy));
        Assert.True(reminder.Check(At(40), Busy));
        Assert.True(reminder.IsShowing);
        Assert.False(reminder.Check(At(41), Busy)); // no se repite mientras se ve
    }

    [Fact]
    public void FiveMinutesAway_CountsAsRest()
    {
        var reminder = new BreakReminder(TimeSpan.FromMinutes(40), Start);

        reminder.Check(At(30), TimeSpan.FromMinutes(5)); // te fuiste 5 min
        Assert.False(reminder.Check(At(45), Busy));      // solo llevas 15 min desde que volviste
        Assert.True(reminder.Check(At(70), Busy));       // 40 min desde que volviste
    }

    [Fact]
    public void ShortPauses_DoNotReset()
    {
        var reminder = new BreakReminder(TimeSpan.FromMinutes(40), Start);

        reminder.Check(At(20), TimeSpan.FromMinutes(3));
        Assert.True(reminder.Check(At(40), Busy));
    }

    [Fact]
    public void LeavingWhileShown_ClosesIt()
    {
        var reminder = new BreakReminder(TimeSpan.FromMinutes(40), Start);
        reminder.Check(At(40), Busy);

        reminder.Check(At(46), TimeSpan.FromMinutes(6)); // se fue a descansar

        Assert.False(reminder.IsShowing);
    }

    [Fact]
    public void Dismiss_StartsCountingAgain()
    {
        var reminder = new BreakReminder(TimeSpan.FromMinutes(40), Start);
        reminder.Check(At(40), Busy);

        reminder.Dismiss(At(42));

        Assert.False(reminder.Check(At(81), Busy));
        Assert.True(reminder.Check(At(82), Busy));
    }

    [Fact]
    public void Snooze_ShowsAgainAfterTheDelay()
    {
        var reminder = new BreakReminder(TimeSpan.FromMinutes(40), Start);
        reminder.Check(At(40), Busy);

        reminder.Snooze(At(41), TimeSpan.FromMinutes(5));

        Assert.False(reminder.Check(At(45), Busy));
        Assert.True(reminder.Check(At(46), Busy));
    }

    [Fact]
    public void ZeroInterval_NeverShows()
    {
        var reminder = new BreakReminder(TimeSpan.Zero, Start);

        Assert.False(reminder.Check(At(600), Busy));
        Assert.Equal(TimeSpan.Zero, reminder.Remaining(At(10)));
    }

    [Fact]
    public void Restart_UsesTheNewInterval()
    {
        var reminder = new BreakReminder(TimeSpan.FromMinutes(40), Start);

        reminder.Restart(TimeSpan.FromMinutes(20), At(30));

        Assert.Equal(TimeSpan.FromMinutes(20), reminder.Remaining(At(30)));
        Assert.True(reminder.Check(At(50), Busy));
    }
}
