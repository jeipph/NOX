using System.Runtime.InteropServices;
using NotchBar.Core.Shell;
using static NotchBar.Windows.Shell.NativeMethods;

namespace NotchBar.Windows.Shell;

/// <summary>
/// Avisa cuando una app entra o sale de pantalla completa.
/// Se registra como "barra de escritorio" (AppBar) sin reservar espacio: así Windows
/// envía el aviso ABN_FULLSCREENAPP por sí solo, sin que tengamos que preguntar cada segundo.
/// </summary>
public sealed class FullscreenDetector : IDisposable
{
    private readonly nint _hwnd;
    private readonly uint _callbackMessage;
    private bool _registered;

    public event Action<bool>? FullscreenChanged;

    public bool IsFullscreen { get; private set; }

    public FullscreenDetector(nint hwnd)
    {
        _hwnd = hwnd;
        _callbackMessage = RegisterWindowMessage("NotchBar.AppBarCallback");

        var data = NewAppBarData();
        data.uCallbackMessage = _callbackMessage;
        _registered = SHAppBarMessage(ABM_NEW, ref data) != 0;
    }

    /// <summary>Llamar desde el WndProc de la ventana. Devuelve true si el mensaje era para nosotros.</summary>
    public bool HandleMessage(int msg, nint wParam, nint lParam)
    {
        if ((uint)msg != _callbackMessage || (int)wParam != ABN_FULLSCREENAPP)
            return false;

        bool isFullscreen = lParam != 0 && ForegroundCoversOurMonitor();
        if (isFullscreen != IsFullscreen)
        {
            IsFullscreen = isFullscreen;
            FullscreenChanged?.Invoke(isFullscreen);
        }
        return true;
    }

    // El aviso de Windows no dice qué ventana es; comprobamos que la ventana activa
    // esté en nuestro monitor y lo tape entero.
    private bool ForegroundCoversOurMonitor()
    {
        nint foreground = GetForegroundWindow();
        if (foreground == 0)
            return false;

        nint monitor = MonitorFromWindow(_hwnd, MONITOR_DEFAULTTONEAREST);
        if (MonitorFromWindow(foreground, MONITOR_DEFAULTTONEAREST) != monitor)
            return false;

        var info = new MONITORINFO { cbSize = (uint)Marshal.SizeOf<MONITORINFO>() };
        if (!GetMonitorInfo(monitor, ref info) || !GetWindowRect(foreground, out RECT windowRect))
            return false;

        return FullscreenRules.IsFullscreen(
            WindowHelper.ToPixelRect(windowRect),
            WindowHelper.ToPixelRect(info.rcMonitor),
            GetClassName(foreground));
    }

    private static unsafe string GetClassName(nint hwnd)
    {
        char* buffer = stackalloc char[256];
        int length = NativeMethods.GetClassName(hwnd, buffer, 256);
        return new string(buffer, 0, length);
    }

    private APPBARDATA NewAppBarData() => new()
    {
        cbSize = (uint)Marshal.SizeOf<APPBARDATA>(),
        hWnd = _hwnd,
    };

    public void Dispose()
    {
        if (!_registered)
            return;

        var data = NewAppBarData();
        SHAppBarMessage(ABM_REMOVE, ref data);
        _registered = false;
    }
}
