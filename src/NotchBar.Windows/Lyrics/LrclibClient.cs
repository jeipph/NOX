using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using NotchBar.Core.Abstractions;
using NotchBar.Core.Lyrics;
using NotchBar.Core.Models;

namespace NotchBar.Windows.Lyrics;

/// <summary>
/// Busca letras en lrclib.net (gratis, sin clave). Es la única conexión a internet de la app.
/// Solo envía título, artista, álbum y duración de la canción.
/// </summary>
public sealed class LrclibClient : ILyricsSource, IDisposable
{
    private readonly HttpClient _http = new()
    {
        BaseAddress = new Uri("https://lrclib.net/"),
        Timeout = TimeSpan.FromSeconds(5),
    };

    public LrclibClient()
    {
        // LRCLIB pide identificarse con un User-Agent descriptivo.
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("NotchBar/0.1 (app personal de escritorio)");
    }

    public async Task<LyricsResult?> FindAsync(MediaInfo song, CancellationToken cancellationToken)
    {
        // 1. Búsqueda exacta con todos los datos.
        LrclibTrack? track = await GetAsync(song.Title, song.Artist, song.Album, song.Duration, cancellationToken);

        // 2. Solo con el primer artista ("A, B y C" → "A").
        string primary = ArtistNames.Primary(song.Artist);
        if (track is null && primary != song.Artist)
            track = await GetAsync(song.Title, primary, song.Album, song.Duration, cancellationToken);

        // 3. Búsqueda libre: el resultado de duración más parecida.
        track ??= await SearchAsync(song.Title, primary, song.Duration, cancellationToken);

        if (track is null || !LyricsMatching.IsSameLength(song.Duration, track.Duration))
            return null;

        return track.Instrumental
            ? LyricsResult.Instrumental()
            : LyricsResult.FromText(track.SyncedLyrics, track.PlainLyrics);
    }

    private async Task<LrclibTrack?> GetAsync(string title, string artist, string album, TimeSpan duration, CancellationToken cancellationToken)
    {
        var query = new List<string>
        {
            "track_name=" + Uri.EscapeDataString(title),
            "artist_name=" + Uri.EscapeDataString(artist),
        };
        if (!string.IsNullOrWhiteSpace(album))
            query.Add("album_name=" + Uri.EscapeDataString(album));
        if (duration > TimeSpan.Zero)
            query.Add("duration=" + Math.Round(duration.TotalSeconds).ToString(CultureInfo.InvariantCulture));

        using HttpResponseMessage response = await _http.GetAsync("api/get?" + string.Join('&', query), cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<LrclibTrack>(cancellationToken);
    }

    private async Task<LrclibTrack?> SearchAsync(string title, string artist, TimeSpan duration, CancellationToken cancellationToken)
    {
        string url = $"api/search?track_name={Uri.EscapeDataString(title)}&artist_name={Uri.EscapeDataString(artist)}";
        LrclibTrack[]? results = await _http.GetFromJsonAsync<LrclibTrack[]>(url, cancellationToken);
        if (results is null || results.Length == 0)
            return null;

        var candidates = results
            .Select(r => new LyricsCandidate(r.Duration, !string.IsNullOrWhiteSpace(r.SyncedLyrics), r.Instrumental))
            .ToList();
        int best = LyricsMatching.BestIndex(candidates, duration);
        return best >= 0 ? results[best] : null;
    }

    // Respuesta JSON de LRCLIB (los nombres coinciden en camelCase).
    private sealed record LrclibTrack(
        long Id,
        string? TrackName,
        string? ArtistName,
        string? AlbumName,
        double Duration,
        bool Instrumental,
        string? PlainLyrics,
        string? SyncedLyrics);

    public void Dispose() => _http.Dispose();
}
