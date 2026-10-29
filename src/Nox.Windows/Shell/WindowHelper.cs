using System.Runtime.InteropServices;
using Nox.Core.Shell;
using static Nox.Windows.Shell.NativeMethods;

namespace Nox.Windows.Shell;

public static class WindowHelper
{
    public const int WmMouseActivate = WM_MOUSEACTIVATE;
    public const int MaNoActivate = MA_NOACTIVATE;

    /// <summary>True si el mensaje avisa de que cambió el área de trabajo (barra de tareas, franjas reservadas).</summary>
    public static bool IsWorkAreaChanged(int msg, nint wParam) =>
        msg == WM_SETTINGCHANGE && (int)wParam == SPI_SETWORKAREA;

    /// <summary>Quita la ventana de Alt+Tab y evita que robe el foco al hacer clic.</summary>
    public static void MakeOverlay(nint hwnd)
    {
        long style = GetWindowLongPtr(hwnd, GWL_EXSTYLE);
        style |= WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
        style &= ~WS_EX_APPWINDOW;
        SetWindowLongPtr(hwnd, GWL_EXSTYLE, (nint)style);
    }

    /// <summary>
    /// Permite o impide que la ventana se active (reciba el teclado). El notch normalmente no se activa;
    /// solo mientras se escribe una nota.
    /// </summary>
    public static void SetActivatable(nint hwnd, bool activatable)
    {
        long style = GetWindowLongPtr(hwnd, GWL_EXSTYLE);
        style = activatable ? style & ~WS_EX_NOACTIVATE : style | WS_EX_NOACTIVATE;
        SetWindowLongPtr(hwnd, GWL_EXSTYLE, (nint)style);
    }

    /// <summary>La ventana que tiene el teclado ahora mismo.</summary>
    public static nint ActiveWindow() => GetForegroundWindow();

    /// <summary>Devuelve el teclado a otra ventana (solo funciona si nosotros lo tenemos).</summary>
    public static void Activate(nint hwnd)
    {
        if (hwnd != 0)
            SetForegroundWindow(hwnd);
    }

    /// <summary>
    /// Monitor principal en píxeles físicos: <c>Bounds</c> es la pantalla entera y
    /// <c>WorkArea</c> lo que queda sin la barra de tareas (y sin franjas reservadas).
    /// </summary>
    public static (PixelRect Bounds, PixelRect WorkArea) GetPrimaryMonitor()
    {
        nint monitor = MonitorFromPoint(new POINT(), MONITOR_DEFAULTTOPRIMARY);
        var info = new MONITORINFO { cbSize = (uint)Marshal.SizeOf<MONITORINFO>() };
        GetMonitorInfo(monitor, ref info);
        return (ToPixelRect(info.rcMonitor), ToPixelRect(info.rcWork));
    }

    /// <summary>Coloca la ventana arriba al centro de <paramref name="area"/> y la mantiene encima de todo.</summary>
    public static void PlaceTopCenter(nint hwnd, PixelRect area, int pixelWidth)
    {
        int x = area.Left + (area.Width - pixelWidth) / 2;
        SetWindowPos(hwnd, HWND_TOPMOST, x, area.Top, 0, 0, SWP_NOSIZE | SWP_NOACTIVATE);
    }

    /// <summary>Coloca y dimensiona la ventana (en píxeles reales) encima de todo, sin activarla.</summary>
    public static void PlaceTopmost(nint hwnd, PixelRect bounds) =>
        SetWindowPos(hwnd, HWND_TOPMOST, bounds.Left, bounds.Top, bounds.Width, bounds.Height, SWP_NOACTIVATE);

    internal static PixelRect ToPixelRect(RECT r) => new(r.Left, r.Top, r.Right, r.Bottom);
}
