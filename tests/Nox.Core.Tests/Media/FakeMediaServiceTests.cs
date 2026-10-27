using Nox.Core.Media;
using Nox.Core.Models;

namespace Nox.Core.Tests.Media;

public class FakeMediaServiceTests
{
    private sealed class ManualTime(DateTimeOffset start) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = start;
        public override DateTimeOffset GetUtcNow() => Now;
    }

    [Fact]
    public async Task Start_PublishesASong()
    {
        var media = new FakeMediaService();
        MediaInfo? received = null;
        media.MediaChanged += info => received = info;

        await media.StartAsync();

        Assert.NotNull(received);
        Assert.True(received.IsPlaying);
    }

    [Fact]
    public async Task TogglePlayPause_PausesAndKeepsPosition()
    {
        var time = new ManualTime(new DateTimeOffset(2026, 10, 27, 12, 0, 0, TimeSpan.Zero));
        var media = new FakeMediaService(time);
        await media.StartAsync();

        time.Now += TimeSpan.FromSeconds(12);
        await media.TogglePlayPauseAsync();

        Assert.False(media.Current!.IsPlaying);
        Assert.Equal(TimeSpan.FromSeconds(12), media.Current.Position);
    }

    [Fact]
    public async Task Previous_OnFirstSong_StaysOnFirstSong()
    {
        var media = new FakeMediaService();
        string first = media.Current!.Title;

        await media.PreviousAsync();

        Assert.Equal(first, media.Current!.Title);
    }

    [Fact]
    public async Task Next_ChangesSong()
    {
        var media = new FakeMediaService();
        string first = media.Current!.Title;

        await media.NextAsync();

        Assert.NotEqual(first, media.Current!.Title);
    }
}
