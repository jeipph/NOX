namespace NotchBar.Core.Media;

/// <summary>Estado de un reproductor para decidir cuál mostrar.</summary>
public readonly record struct SourceState(string Id, bool IsPlaying);

/// <summary>Decide qué reproductor muestra el notch cuando hay varios abiertos.</summary>
public static class SourcePicker
{
    /// <summary>
    /// Devuelve la posición del reproductor elegido en <paramref name="sources"/>, o -1 si no hay ninguno.
    /// </summary>
    /// <param name="preferredId">Reproductor elegido por la persona; null = automático.</param>
    /// <param name="windowsCurrent">Posición del reproductor que Windows considera "actual", o -1.</param>
    public static int Pick(IReadOnlyList<SourceState> sources, string? preferredId, int windowsCurrent)
    {
        if (sources.Count == 0)
            return -1;

        // 1. El preferido, si está abierto (si tiene varias sesiones, la que suena).
        if (preferredId is not null)
        {
            int preferred = FirstIndex(sources, s => s.Id == preferredId && s.IsPlaying);
            if (preferred < 0)
                preferred = FirstIndex(sources, s => s.Id == preferredId);
            if (preferred >= 0)
                return preferred;
        }

        bool hasCurrent = windowsCurrent >= 0 && windowsCurrent < sources.Count;

        // 2. Automático: el que suena, empezando por el que Windows considera actual.
        if (hasCurrent && sources[windowsCurrent].IsPlaying)
            return windowsCurrent;

        int playing = FirstIndex(sources, s => s.IsPlaying);
        if (playing >= 0)
            return playing;

        // 3. Si nada suena: el actual de Windows, o el primero.
        return hasCurrent ? windowsCurrent : 0;
    }

    private static int FirstIndex(IReadOnlyList<SourceState> sources, Func<SourceState, bool> match)
    {
        for (int i = 0; i < sources.Count; i++)
        {
            if (match(sources[i]))
                return i;
        }
        return -1;
    }
}
