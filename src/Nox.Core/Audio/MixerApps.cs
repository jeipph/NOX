using Nox.Core.Abstractions;

namespace Nox.Core.Audio;

/// <summary>Una fila del mezclador: una app con todas sus sesiones de audio juntas.</summary>
public sealed record MixerApp(string Key, string Name, IReadOnlyList<string> SessionIds, float Volume, bool IsMuted);

/// <summary>Convierte las sesiones de Windows en las filas del mezclador.</summary>
public static class MixerApps
{
    public const string SystemSoundsName = "Sonidos del sistema";

    /// <summary>
    /// Junta las sesiones de una misma app (Chrome abre varias), quita las de NOX y las ordena:
    /// primero las apps por nombre y al final los sonidos del sistema. El volumen de la fila es el
    /// más alto de sus sesiones; está en silencio solo si lo están todas.
    /// </summary>
    public static IReadOnlyList<MixerApp> From(IEnumerable<AudioSessionInfo> sessions, string ownProcessName = "NOX")
    {
        return
        [
            .. sessions
                .Where(s => s.IsSystemSounds || !s.ProcessName.Equals(ownProcessName, StringComparison.OrdinalIgnoreCase))
                .GroupBy(Key, StringComparer.OrdinalIgnoreCase)
                .Select(g => new MixerApp(
                    g.Key,
                    g.First().IsSystemSounds ? SystemSoundsName : NameOf(g),
                    [.. g.Select(s => s.SessionId)],
                    g.Max(s => s.Volume),
                    g.All(s => s.IsMuted)))
                .OrderBy(a => a.Name == SystemSoundsName)
                .ThenBy(a => a.Name, StringComparer.CurrentCultureIgnoreCase),
        ];
    }

    private static string Key(AudioSessionInfo s) =>
        s.IsSystemSounds ? "#system"
        : s.ProcessName.Length > 0 ? s.ProcessName
        : $"#pid{s.ProcessId}";

    // "Google Chrome" si Windows lo sabe; si no, el ejecutable con mayúscula ("Spotify").
    private static string NameOf(IGrouping<string, AudioSessionInfo> sessions)
    {
        string? display = sessions.Select(s => s.DisplayName).FirstOrDefault(n => n.Length > 0 && !n.StartsWith('@'));
        if (display is not null)
            return display;
        string process = sessions.First().ProcessName;
        return process.Length == 0 ? "App desconocida" : char.ToUpperInvariant(process[0]) + process[1..];
    }

    /// <summary>La salida siguiente a la actual (para el botón que las va pasando). Null si solo hay una.</summary>
    public static AudioOutput? NextOutput(IReadOnlyList<AudioOutput> outputs)
    {
        if (outputs.Count < 2)
            return null;
        int current = Math.Max(0, outputs.ToList().FindIndex(o => o.IsDefault));
        return outputs[(current + 1) % outputs.Count];
    }
}
