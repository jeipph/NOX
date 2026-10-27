namespace Nox.Core.Lyrics;

/// <summary>Un resultado de búsqueda de letras, con lo justo para elegir el mejor.</summary>
public readonly record struct LyricsCandidate(double DurationSeconds, bool HasSyncedLyrics, bool IsInstrumental);

/// <summary>Reglas para aceptar o elegir letras encontradas en internet.</summary>
public static class LyricsMatching
{
    /// <summary>Más de 3 s de diferencia suele ser otra versión (en directo, remix...).</summary>
    public static readonly TimeSpan Tolerance = TimeSpan.FromSeconds(3);

    /// <summary>True si la duración encaja con la canción (o si la canción no dice cuánto dura).</summary>
    public static bool IsSameLength(TimeSpan songDuration, double candidateSeconds) =>
        songDuration <= TimeSpan.Zero
        || Math.Abs(songDuration.TotalSeconds - candidateSeconds) <= Tolerance.TotalSeconds;

    /// <summary>
    /// Elige el mejor resultado: de la misma duración, primero con letra sincronizada,
    /// luego instrumental, luego solo texto; a igualdad, el de duración más cercana. -1 si ninguno sirve.
    /// </summary>
    public static int BestIndex(IReadOnlyList<LyricsCandidate> candidates, TimeSpan songDuration)
    {
        int best = -1;
        for (int i = 0; i < candidates.Count; i++)
        {
            if (!IsSameLength(songDuration, candidates[i].DurationSeconds))
                continue;
            if (best < 0 || IsBetter(candidates[i], candidates[best], songDuration))
                best = i;
        }
        return best;
    }

    private static bool IsBetter(LyricsCandidate a, LyricsCandidate b, TimeSpan songDuration)
    {
        int rankA = Rank(a), rankB = Rank(b);
        if (rankA != rankB)
            return rankA < rankB;

        double distanceA = Math.Abs(songDuration.TotalSeconds - a.DurationSeconds);
        double distanceB = Math.Abs(songDuration.TotalSeconds - b.DurationSeconds);
        return distanceA < distanceB;
    }

    private static int Rank(LyricsCandidate c) => c.HasSyncedLyrics ? 0 : c.IsInstrumental ? 1 : 2;
}
