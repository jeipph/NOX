using System.Collections.Concurrent;
using Nox.Core.Abstractions;
using Nox.Core.Models;

namespace Nox.Core.Lyrics;

/// <summary>Caché que solo vive en memoria (para pruebas y para el modo <c>--fake</c>).</summary>
public sealed class MemoryLyricsCache : ILyricsCache
{
    private readonly ConcurrentDictionary<string, LyricsResult> _items = new();

    public Task<LyricsResult?> LoadAsync(string key, CancellationToken cancellationToken) =>
        Task.FromResult(_items.TryGetValue(key, out LyricsResult? value) ? value : null);

    public Task SaveAsync(string key, LyricsResult lyrics, CancellationToken cancellationToken)
    {
        _items[key] = lyrics;
        return Task.CompletedTask;
    }
}
