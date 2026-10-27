namespace NotchBar.Core.Abstractions;

/// <summary>Volumen general de la salida de audio predeterminada.</summary>
public interface IVolumeService
{
    /// <summary>Cambió el volumen o el silencio (también desde fuera: teclado, Windows). Puede llegar desde otro hilo.</summary>
    event Action? Changed;

    /// <summary>De 0 a 1.</summary>
    float Level { get; }

    bool IsMuted { get; }

    void SetLevel(float level);

    void SetMuted(bool muted);
}
