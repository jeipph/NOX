using NotchBar.Core.Shell;
using static NotchBar.Windows.Shell.NativeMethods;

namespace NotchBar.Windows.Shell;

public static class WindowHelper
{
    public const int WmMouseActivate = WM_MOUSEACTIVATE;
    public const int MaNoActivate = MA_NOACTIVATE;

    /// <summary>Quita la ventana de Alt+Tab y evita que robe el foco al hacer clic.</summary>
    public static void MakeOverlay(nint hwnd)
    {
        long style = GetWindowLongPtr(hwnd, GWL_EXSTYLE);
        style |= WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
        style &= ~WS_EX_APPWINDOW;
        SetWindowLongPtr(hwnd, GWL_EXSTYLE, (nint)style);
    }

    /// <summary>Área de trabajo (sin la barra de tareas) del monitor principal, en píxeles físicos.</summary>
    public static PixelRect GetPrimaryWorkArea()
    {
        nint monitor = MonitorFromPoint(new POINT(), MONITOR_DEFAULTTOPRIMARY);
        var info = new MONITORINFO { cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf<MONITORINFO>() };
        GetMonitorInfo(monitor, ref info);
        return ToPixelRect(info.rcWork);
    }

    /// <summary>Coloca la ventana arriba al centro de <paramref name="area"/> y la mantiene encima de todo.</summary>
    public static void PlaceTopCenter(nint hwnd, PixelRect area, int pixelWidth)
    {
        int x = area.Left + (area.Width - pixelWidth) / 2;
        SetWindowPos(hwnd, HWND_TOPMOST, x, area.Top, 0, 0, SWP_NOSIZE | SWP_NOACTIVATE);
    }

    internal static PixelRect ToPixelRect(RECT r) => new(r.Left, r.Top, r.Right, r.Bottom);
}
