namespace Nox.Windows.Shell;

/// <summary>Barra de título clara u oscura (la de Windows, con sus botones), según el tema de NOX.</summary>
public static class TitleBar
{
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

    public static void SetDark(nint hwnd, bool dark)
    {
        int value = dark ? 1 : 0;
        NativeMethods.DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref value, sizeof(int));
    }
}
