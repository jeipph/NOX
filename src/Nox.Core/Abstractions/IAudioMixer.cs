namespace Nox.Core.Abstractions;

/// <summary>Una salida de audio (altavoces, auriculares, HDMI…).</summary>
public sealed record AudioOutput(string Id, string Name, bool IsDefault);

/// <summary>
/// Una sesión de audio tal como la da Windows (una app puede tener varias, como Chrome).
/// </summary>
/// <param name="ProcessName">Nombre del ejecutable sin ".exe" ("chrome"), o "" si no se sabe.</param>
/// <param name="DisplayName">Nombre para mostrar ("Google Chrome"), o "" si no se sabe.</param>
public sealed record AudioSessionInfo(
    string SessionId,
    int ProcessId,
    string ProcessName,
    string DisplayName,
    float Volume,
    bool IsMuted,
    bool IsSystemSounds);

/// <summary>Mezclador: volumen por app y salida de audio predeterminada.</summary>
public interface IAudioMixer
{
    /// <summary>Apareció o se cerró una app con sonido, o cambiaron las salidas. Puede llegar desde otro hilo.</summary>
    event Action? Changed;

    IReadOnlyList<AudioOutput> Outputs();

    /// <returns>False si Windows no dejó cambiarla.</returns>
    bool SetDefaultOutput(string id);

    IReadOnlyList<AudioSessionInfo> Sessions();

    void SetVolume(IEnumerable<string> sessionIds, float volume);

    void SetMuted(IEnumerable<string> sessionIds, bool muted);
}
