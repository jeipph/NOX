using NotchBar.Core.Abstractions;
using NotchBar.Core.Lyrics;
using NotchBar.Core.Models;

namespace NotchBar.Core.Tests.Lyrics;

public class LyricsServiceTests
{
    private static readonly MediaInfo Song = new(
        "Crazy", "Gnarls Barkley", "St. Elsewhere",
        TimeSpan.FromSeconds(178), TimeSpan.Zero, DateTimeOffset.UnixEpoch, true, "test", null);

    private static readonly LyricsResult Synced =
        new(LyricsKind.Synced, [new LyricLine(TimeSpan.FromSeconds(1), "I remember when")], null);

    private sealed class CountingSource(LyricsResult? answer) : ILyricsSource
    {
        public int Calls { get; private set; }

        public Task<LyricsResult?> FindAsync(MediaInfo song, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(answer);
        }
    }

    private sealed class FailingSource : ILyricsSource
    {
        public Task<LyricsResult?> FindAsync(MediaInfo song, CancellationToken cancellationToken) =>
            throw new HttpRequestException("sin conexión");
    }

    [Fact]
    public async Task Found_IsSavedAndNotAskedAgain()
    {
        var source = new CountingSource(Synced);
        var service = new LyricsService(source, new MemoryLyricsCache());

        await service.GetLyricsAsync(Song, allowNetwork: true, CancellationToken.None);
        var second = await service.GetLyricsAsync(Song, allowNetwork: true, CancellationToken.None);

        Assert.Equal(1, source.Calls);
        Assert.Equal(LyricsKind.Synced, second!.Kind);
    }

    [Fact]
    public async Task NotFound_IsRememberedDuringTheSession()
    {
        var source = new CountingSource(null);
        var service = new LyricsService(source, new MemoryLyricsCache());

        var first = await service.GetLyricsAsync(Song, allowNetwork: true, CancellationToken.None);
        await service.GetLyricsAsync(Song, allowNetwork: true, CancellationToken.None);

        Assert.Equal(LyricsKind.NotFound, first!.Kind);
        Assert.Equal(1, source.Calls);
    }

    [Fact]
    public async Task NetworkDisabled_UsesOnlyTheCache()
    {
        var source = new CountingSource(Synced);
        var cache = new MemoryLyricsCache();
        var service = new LyricsService(source, cache);

        Assert.Null(await service.GetLyricsAsync(Song, allowNetwork: false, CancellationToken.None));

        await cache.SaveAsync(LyricsKey.For(Song), Synced, CancellationToken.None);
        Assert.NotNull(await service.GetLyricsAsync(Song, allowNetwork: false, CancellationToken.None));
        Assert.Equal(0, source.Calls);
    }

    [Fact]
    public async Task ConnectionError_IsReported_AndNothingIsCached()
    {
        var cache = new MemoryLyricsCache();
        var service = new LyricsService(new FailingSource(), cache);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => service.GetLyricsAsync(Song, allowNetwork: true, CancellationToken.None));
        Assert.Null(await cache.LoadAsync(LyricsKey.For(Song), CancellationToken.None));
    }

    [Fact]
    public async Task Import_ReplacesNotFound()
    {
        var service = new LyricsService(new CountingSource(null), new MemoryLyricsCache());
        await service.GetLyricsAsync(Song, allowNetwork: true, CancellationToken.None);

        await service.ImportAsync(Song, "[00:01.00] I remember when", CancellationToken.None);
        var result = await service.GetLyricsAsync(Song, allowNetwork: true, CancellationToken.None);

        Assert.Equal(LyricsKind.Synced, result!.Kind);
    }

    [Fact]
    public async Task Import_WithoutTimes_IsPlainLyrics()
    {
        var service = new LyricsService(new CountingSource(null), new MemoryLyricsCache());

        var result = await service.ImportAsync(Song, "I remember when\nI remember", CancellationToken.None);

        Assert.Equal(LyricsKind.Plain, result.Kind);
    }

    [Fact]
    public void Key_IgnoresCaseAndSpaces_ButNotDuration()
    {
        var same = Song with { Title = "  CRAZY ", Artist = "gnarls barkley" };
        var longer = Song with { Duration = TimeSpan.FromSeconds(240) };

        Assert.Equal(LyricsKey.For(Song), LyricsKey.For(same));
        Assert.NotEqual(LyricsKey.For(Song), LyricsKey.For(longer));
    }
}
