using NotchBar.Core.Abstractions;
using NotchBar.Core.Models;

namespace NotchBar.Core.Lyrics;

/// <summary>Letra de mentira (una línea cada 3 s) para el modo <c>--fake</c>, sin internet.</summary>
public sealed class FakeLyricsSource : ILyricsSource
{
    public Task<LyricsResult?> FindAsync(MediaInfo song, CancellationToken cancellationToken)
    {
        var lines = new List<LyricLine>();
        for (int i = 0; i * 3 < song.Duration.TotalSeconds; i++)
            lines.Add(new LyricLine(TimeSpan.FromSeconds(i * 3), i % 5 == 4 ? "" : $"Línea de prueba número {i + 1}"));

        return Task.FromResult<LyricsResult?>(new LyricsResult(LyricsKind.Synced, lines, null));
    }
}
