using NotchBar.Core.Lyrics;

namespace NotchBar.Core.Models;

public enum LyricsKind
{
    /// <summary>Letra con tiempos: se puede seguir línea a línea.</summary>
    Synced,

    /// <summary>Solo el texto, sin tiempos.</summary>
    Plain,

    /// <summary>La canción no tiene letra.</summary>
    Instrumental,

    /// <summary>No se encontró letra.</summary>
    NotFound,
}

/// <summary>Letra de una canción, tal como se guarda en la caché.</summary>
public sealed record LyricsResult(LyricsKind Kind, IReadOnlyList<LyricLine> Lines, string? PlainText)
{
    public static LyricsResult NotFound() => new(LyricsKind.NotFound, [], null);

    public static LyricsResult Instrumental() => new(LyricsKind.Instrumental, [], null);

    /// <summary>Crea el resultado a partir de un texto LRC (con tiempos) y/o un texto plano.</summary>
    public static LyricsResult FromText(string? syncedLrc, string? plainText)
    {
        IReadOnlyList<LyricLine> lines = LrcParser.Parse(syncedLrc);
        if (lines.Count > 0)
            return new LyricsResult(LyricsKind.Synced, lines, plainText);

        return string.IsNullOrWhiteSpace(plainText)
            ? NotFound()
            : new LyricsResult(LyricsKind.Plain, [], plainText.Trim());
    }
}
