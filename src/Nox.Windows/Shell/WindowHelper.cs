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

    /// <summary>
    /// Escalado del monitor principal (1 = 100 %, 1,25 = 125 %…). Las ventanas de NOX viven ahí:
    /// se usa este y no el del monitor donde Windows creó la ventana al principio, que puede ser otro.
    /// </summary>
    public static double GetPrimaryMonitorScale()
    {
        nint monitor = MonitorFromPoint(new POINT(), MONITOR_DEFAULTTOPRIMARY);
        return GetDpiForMonitor(monitor, MDT_EFFECTIVE_DPI, out uint dpi, out _) == 0 && dpi > 0 ? dpi / 96.0 : 1.0;
    }

    /// <summary>Coloca y dimensiona la ventana (en píxeles reales) encima de todo, sin activarla.</summary>
    public static void PlaceTopmost(nint hwnd, PixelRect bounds) =>
        SetWindowPos(hwnd, HWND_TOPMOST, bounds.Left, bounds.Top, bounds.Width, bounds.Height, SWP_NOACTIVATE);

    /// <summary>Dónde está el ratón, en píxeles físicos de pantalla.</summary>
    public static (int X, int Y) GetCursorPosition() =>
        GetCursorPos(out POINT point) ? (point.X, point.Y) : (0, 0);

    /// <summary>Rectángulo de la ventana en píxeles físicos.</summary>
    public static PixelRect GetWindowBounds(nint hwnd) =>
        GetWindowRect(hwnd, out RECT rect) ? ToPixelRect(rect) : default;

    /// <summary>Mueve la ventana (en píxeles físicos) sin cambiar su tamaño, encima de todo y sin activarla.</summary>
    public static void MoveTo(nint hwnd, int x, int y) =>
        SetWindowPos(hwnd, HWND_TOPMOST, x, y, 0, 0, SWP_NOSIZE | SWP_NOACTIVATE);

    /// <summary>
    /// Para el WndProc: si la ventana se está arrastrando (<c>WM_MOVING</c>), corrige el destino para
    /// que no salga de <paramref name="area"/>. Devuelve true si el mensaje era ese.
    /// </summary>
    public static unsafe bool KeepMovingInside(int msg, nint lParam, PixelRect area)
    {
        if (msg != WM_MOVING || lParam == 0)
            return false;
        RECT* rect = (RECT*)lParam;
        var (dx, dy) = NotchLayout.ShiftToFit(area, ToPixelRect(*rect));
        rect->Left += dx;
        rect->Right += dx;
        rect->Top += dy;
        rect->Bottom += dy;
        return true;
    }

    internal static PixelRect ToPixelRect(RECT r) => new(r.Left, r.Top, r.Right, r.Bottom);
}
