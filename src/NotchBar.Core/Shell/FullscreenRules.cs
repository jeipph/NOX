namespace NotchBar.Core.Shell;

/// <summary>Decide si la ventana activa está realmente en pantalla completa.</summary>
public static class FullscreenRules
{
    // Windows a veces anuncia el escritorio o la barra de tareas como "pantalla completa";
    // esas ventanas nunca deben ocultar el notch.
    private static readonly HashSet<string> ShellClasses =
        ["Progman", "WorkerW", "Shell_TrayWnd", "Shell_SecondaryTrayWnd"];

    public static bool IsFullscreen(PixelRect window, PixelRect monitor, string windowClass) =>
        !ShellClasses.Contains(windowClass) && window.Covers(monitor);
}
