using System.Collections.Concurrent;
using NotchBar.Core.Abstractions;
using NotchBar.Core.Models;

namespace NotchBar.Core.Lyrics;

public sealed class LyricsService(ILyricsSource source, ILyricsCache cache) : ILyricsService
{
    // Canciones sin letra en internet: no se vuelve a preguntar durante esta sesión.
    // No se guardan en disco porque alguien puede subir la letra más adelante.
    private readonly ConcurrentDictionary<string, bool> _notFound = new();

    public async Task<LyricsResult?> GetLyricsAsync(MediaInfo song, bool allowNetwork, CancellationToken cancellationToken)
    {
        string key = LyricsKey.For(song);

        if (await cache.LoadAsync(key, cancellationToken) is { } cached)
            return cached;

        if (!allowNetwork)
            return null;

        if (_notFound.ContainsKey(key))
            return LyricsResult.NotFound();

        LyricsResult? found = await source.FindAsync(song, cancellationToken);
        if (found is null || found.Kind == LyricsKind.NotFound)
        {
            _notFound.TryAdd(key, true);
            return LyricsResult.NotFound();
        }

        await cache.SaveAsync(key, found, cancellationToken);
        return found;
    }

    public async Task<LyricsResult> ImportAsync(MediaInfo song, string lrcText, CancellationToken cancellationToken)
    {
        // Un .lrc sin tiempos se guarda como letra sin sincronizar.
        LyricsResult result = LyricsResult.FromText(lrcText, lrcText);
        string key = LyricsKey.For(song);

        await cache.SaveAsync(key, result, cancellationToken);
        _notFound.TryRemove(key, out _);
        return result;
    }
}
