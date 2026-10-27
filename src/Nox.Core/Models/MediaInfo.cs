namespace Nox.Core.Models;

/// <summary>Lo que está sonando ahora, tal como lo cuenta el reproductor.</summary>
/// <param name="Position">Posición en el momento <paramref name="PositionUpdatedAt"/> (los reproductores no la avisan a cada segundo).</param>
/// <param name="SourceApp">Identificador del reproductor (por ejemplo, el navegador).</param>
/// <param name="Thumbnail">Carátula en bytes (PNG/JPG), o null si no hay.</param>
public sealed record MediaInfo(
    string Title,
    string Artist,
    string Album,
    TimeSpan Duration,
    TimeSpan Position,
    DateTimeOffset PositionUpdatedAt,
    bool IsPlaying,
    string SourceApp,
    byte[]? Thumbnail);
