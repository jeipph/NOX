using Nox.Core.Focus;

namespace Nox.Core.Tests.Focus;

public class FocusTimerTests
{
    private sealed class ManualTime : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2026, 10, 27, 9, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private static TimeSpan Min(double minutes) => TimeSpan.FromMinutes(minutes);

    [Fact]
    public void Start_CountsDown()
    {
        var time = new ManualTime();
        var timer = new FocusTimer(time);

        timer.Start(Min(30));
        time.Now += Min(10);

        Assert.Equal(FocusState.Running, timer.State);
        Assert.Equal(Min(20), timer.Remaining);
        Assert.Equal(1.0 / 3, timer.Progress, precision: 6);
    }

    [Fact]
    public void Pause_StopsTheClock_AndResumeContinues()
    {
        var time = new ManualTime();
        var timer = new FocusTimer(time);
        timer.Start(Min(30));
        time.Now += Min(5);

        timer.Pause();
        time.Now += Min(60);
        Assert.Equal(Min(25), timer.Remaining);

        timer.Resume();
        time.Now += Min(5);
        Assert.Equal(Min(20), timer.Remaining);
    }

    [Fact]
    public void CheckFinished_IsTrueOnlyOnce_WhenTimeIsUp()
    {
        var time = new ManualTime();
        var timer = new FocusTimer(time);
        timer.Start(Min(1));

        time.Now += TimeSpan.FromSeconds(59);
        Assert.False(timer.CheckFinished());

        time.Now += TimeSpan.FromSeconds(1);
        Assert.True(timer.CheckFinished());
        Assert.False(timer.CheckFinished());
        Assert.Equal(FocusState.Finished, timer.State);
        Assert.Equal(TimeSpan.Zero, timer.Remaining);
        Assert.Equal(1, timer.Progress);
    }

    [Fact]
    public void Paused_NeverFinishes()
    {
        var time = new ManualTime();
        var timer = new FocusTimer(time);
        timer.Start(Min(1));
        timer.Pause();
        time.Now += Min(10);

        Assert.False(timer.CheckFinished());
        Assert.Equal(FocusState.Paused, timer.State);
    }

    [Fact]
    public void Reset_GoesBackToIdle()
    {
        var timer = new FocusTimer(new ManualTime());
        timer.Start(Min(30));

        timer.Reset();

        Assert.Equal(FocusState.Idle, timer.State);
        Assert.Equal(0, timer.Progress);
    }

    [Fact]
    public void Start_WithZero_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FocusTimer().Start(TimeSpan.Zero));
    }

    [Theory]
    [InlineData(0, "0:00")]
    [InlineData(0.4, "0:01")]
    [InlineData(65, "1:05")]
    [InlineData(1799.2, "30:00")]
    [InlineData(3600, "1:00:00")]
    [InlineData(5430, "1:30:30")]
    public void Format_ShowsMinutesAndSeconds(double seconds, string expected)
    {
        Assert.Equal(expected, FocusTimer.Format(TimeSpan.FromSeconds(seconds)));
    }

    [Theory]
    [InlineData(15, "15 min")]
    [InlineData(60, "1 h")]
    [InlineData(90, "1 h 30 min")]
    public void Describe_IsShortAndReadable(double minutes, string expected)
    {
        Assert.Equal(expected, FocusTimer.Describe(Min(minutes)));
    }
}
