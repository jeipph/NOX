using NotchBar.Core.Models;

namespace NotchBar.Core.Abstractions;

/// <summary>Busca letras: primero en la caché del disco y, si se permite, en internet.</summary>
public interface ILyricsService
{
    /// <summary>
    /// Devuelve la letra, o null si no está en caché y no se permite usar internet.
    /// Lanza excepción si falla la conexión (no se guarda nada en ese caso).
    /// </summary>
    Task<LyricsResult?> GetLyricsAsync(MediaInfo song, bool allowNetwork, CancellationToken cancellationToken);

    /// <summary>Guarda una letra que la persona trae en un archivo .lrc para esta canción.</summary>
    Task<LyricsResult> ImportAsync(MediaInfo song, string lrcText, CancellationToken cancellationToken);
}

/// <summary>Una fuente de letras en internet (LRCLIB).</summary>
public interface ILyricsSource
{
    /// <summary>La letra encontrada, o null si no existe. Lanza excepción si falla la conexión.</summary>
    Task<LyricsResult?> FindAsync(MediaInfo song, CancellationToken cancellationToken);
}

/// <summary>Letras guardadas en el disco para no volver a buscarlas.</summary>
public interface ILyricsCache
{
    Task<LyricsResult?> LoadAsync(string key, CancellationToken cancellationToken);

    Task SaveAsync(string key, LyricsResult lyrics, CancellationToken cancellationToken);
}
