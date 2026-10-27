using System.Runtime.InteropServices;
using NotchBar.Core.Shell;
using static NotchBar.Windows.Shell.NativeMethods;

namespace NotchBar.Windows.Shell;

/// <summary>
/// Registra la ventana como "barra de escritorio" (AppBar), igual que la barra de tareas.
/// Sin reservar espacio sirve para que Windows nos avise (ABN_FULLSCREENAPP) cuando una app
/// entra o sale de pantalla completa, sin preguntar cada segundo.
/// Opcionalmente reserva una franja arriba para que las ventanas maximizadas no queden debajo.
/// </summary>
public sealed class AppBar : IDisposable
{
    private readonly nint _hwnd;
    private readonly uint _callbackMessage;
    private bool _registered;

    public event Action<bool>? FullscreenChanged;

    /// <summary>Otra barra (por ejemplo la de tareas) cambió y hay que recolocarse.</summary>
    public event Action? PositionChanged;

    public bool IsFullscreen { get; private set; }

    /// <summary>Franja reservada en píxeles físicos, o null si no se reserva nada.</summary>
    public PixelRect? Reserved { get; private set; }

    public AppBar(nint hwnd)
    {
        _hwnd = hwnd;
        _callbackMessage = RegisterWindowMessage("NotchBar.AppBarCallback");
        Register();
    }

    /// <summary>Llamar desde el WndProc de la ventana. Devuelve true si el mensaje era para nosotros.</summary>
    public bool HandleMessage(int msg, nint wParam, nint lParam)
    {
        if ((uint)msg != _callbackMessage)
            return false;

        switch ((int)wParam)
        {
            case ABN_FULLSCREENAPP:
                bool isFullscreen = lParam != 0 && ForegroundCoversOurMonitor();
                if (isFullscreen != IsFullscreen)
                {
                    IsFullscreen = isFullscreen;
                    FullscreenChanged?.Invoke(isFullscreen);
                }
                break;

            case ABN_POSCHANGED:
                if (Reserved is not null)
                    PositionChanged?.Invoke();
                break;
        }
        return true;
    }

    /// <summary>
    /// Reserva una franja de <paramref name="height"/> píxeles arriba del monitor.
    /// Devuelve dónde quedó (Windows la baja si ya hay otra barra arriba).
    /// </summary>
    public PixelRect ReserveTop(PixelRect monitor, int height)
    {
        var data = NewAppBarData();
        data.uEdge = ABE_TOP;
        data.rc = new RECT { Left = monitor.Left, Top = monitor.Top, Right = monitor.Right, Bottom = monitor.Top + height };
        SHAppBarMessage(ABM_QUERYPOS, ref data);

        data.rc.Bottom = data.rc.Top + height;
        var wanted = WindowHelper.ToPixelRect(data.rc);
        if (Reserved != wanted)
            SHAppBarMessage(ABM_SETPOS, ref data);

        Reserved = wanted;
        return wanted;
    }

    /// <summary>Deja de reservar espacio (sigue recibiendo los avisos de pantalla completa).</summary>
    public void ReleaseReservation()
    {
        if (Reserved is null)
            return;

        Unregister();
        Register();
        Reserved = null;
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

    private void Register()
    {
        var data = NewAppBarData();
        data.uCallbackMessage = _callbackMessage;
        _registered = SHAppBarMessage(ABM_NEW, ref data) != 0;
    }

    private void Unregister()
    {
        if (!_registered)
            return;

        var data = NewAppBarData();
        SHAppBarMessage(ABM_REMOVE, ref data);
        _registered = false;
    }

    private APPBARDATA NewAppBarData() => new()
    {
        cbSize = (uint)Marshal.SizeOf<APPBARDATA>(),
        hWnd = _hwnd,
    };

    public void Dispose()
    {
        Unregister();
        Reserved = null;
    }
}
