using Nox.Core.Abstractions;

namespace Nox.Windows.Shell;

/// <summary>
/// "Mantener despierto" con <c>SetThreadExecutionState</c>: mientras está activo, Windows no suspende
/// el PC ni apaga la pantalla por inactividad. No cambia ninguna opción de energía y se deshace
/// solo si NOX se cierra (el aviso va unido al hilo que lo pidió).
/// </summary>
public sealed class KeepAwake : IKeepAwake, IDisposable
{
    public bool IsEnabled { get; private set; }

    public void SetEnabled(bool enabled)
    {
        if (enabled == IsEnabled)
            return;
        uint flags = enabled
            ? NativeMethods.ES_CONTINUOUS | NativeMethods.ES_SYSTEM_REQUIRED | NativeMethods.ES_DISPLAY_REQUIRED
            : NativeMethods.ES_CONTINUOUS;
        // Devuelve 0 si Windows no lo aceptó: entonces se queda como estaba.
        if (NativeMethods.SetThreadExecutionState(flags) != 0)
            IsEnabled = enabled;
    }

    public void Dispose() => SetEnabled(false);
}
