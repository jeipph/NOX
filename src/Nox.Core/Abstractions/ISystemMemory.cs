namespace Nox.Core.Abstractions;

/// <summary>Memoria RAM del PC, en bytes.</summary>
public readonly record struct MemoryStatus(long TotalBytes, long AvailableBytes)
{
    public long UsedBytes => Math.Max(TotalBytes - AvailableBytes, 0);
}

/// <summary>Leer la RAM y pedir a las apps que devuelvan la que no usan (lo implementa Nox.Windows).</summary>
public interface ISystemMemory
{
    MemoryStatus GetStatus();

    /// <summary>
    /// Vacía la memoria de trabajo de los procesos a los que hay acceso (Windows la vuelve a cargar
    /// si la necesitan). Devuelve en cuántos procesos se pudo.
    /// </summary>
    int TrimWorkingSets();
}
