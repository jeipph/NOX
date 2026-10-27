using NotchBar.Core.Models;

namespace NotchBar.Core.Lyrics;

/// <summary>Decide qué línea de la letra se está cantando.</summary>
public static class LyricsSyncEngine
{
    public static readonly TimeSpan MaxOffset = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Posición de la línea activa, o -1 si aún no empezó la primera.
    /// </summary>
    /// <param name="lines">Líneas ordenadas por tiempo (como las devuelve <see cref="LrcParser"/>).</param>
    /// <param name="offset">Ajuste de la persona: positivo = la letra sale antes.</param>
    public static int IndexAt(IReadOnlyList<LyricLine> lines, TimeSpan position, TimeSpan offset)
    {
        TimeSpan time = position + offset;

        // Búsqueda binaria: la última línea cuyo inicio ya pasó.
        int low = 0, high = lines.Count - 1, found = -1;
        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            if (lines[middle].Start <= time)
            {
                found = middle;
                low = middle + 1;
            }
            else
            {
                high = middle - 1;
            }
        }
        return found;
    }

    /// <summary>
    /// Cuánto falta (en tiempo de la canción) para que empiece la línea siguiente, o null si no hay más.
    /// Sirve para despertar justo a tiempo en vez de revisar cada pocos milisegundos.
    /// </summary>
    public static TimeSpan? TimeUntilNext(IReadOnlyList<LyricLine> lines, TimeSpan position, TimeSpan offset)
    {
        int next = IndexAt(lines, position, offset) + 1;
        return next < lines.Count ? lines[next].Start - (position + offset) : null;
    }

    /// <summary>Limita el ajuste a ±5 s.</summary>
    public static TimeSpan ClampOffset(TimeSpan offset) =>
        offset > MaxOffset ? MaxOffset : offset < -MaxOffset ? -MaxOffset : offset;
}
