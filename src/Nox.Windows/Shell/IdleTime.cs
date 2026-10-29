using System.Runtime.InteropServices;

namespace Nox.Windows.Shell;

/// <summary>Cuánto tiempo lleva el PC sin teclado ni ratón (<c>GetLastInputInfo</c>).</summary>
public static class IdleTime
{
    public static TimeSpan Get()
    {
        var info = new NativeMethods.LASTINPUTINFO { cbSize = (uint)Marshal.SizeOf<NativeMethods.LASTINPUTINFO>() };
        if (!NativeMethods.GetLastInputInfo(ref info))
            return TimeSpan.Zero;
        // Ambos son milisegundos desde que arrancó Windows; el de la entrada es de 32 bits y da la vuelta a los 49 días.
        uint now = unchecked((uint)Environment.TickCount64);
        return TimeSpan.FromMilliseconds(unchecked(now - info.dwTime));
    }
}
