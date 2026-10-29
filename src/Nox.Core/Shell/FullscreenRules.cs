namespace Nox.Core.Shell;

/// <summary>Decide si la ventana activa está realmente en pantalla completa.</summary>
public static class FullscreenRules
{
    // Estilos extendidos de ventana de Windows (WS_EX_*).
    public const long TransparentStyle = 0x00000020;   // el ratón la atraviesa
    public const long NoActivateStyle = 0x08000000;    // nunca recibe el teclado

    // Windows a veces anuncia el escritorio o la barra de tareas como "pantalla completa";
    // esas ventanas nunca deben ocultar el notch.
    private static readonly HashSet<string> ShellClasses =
        ["Progman", "WorkerW", "Shell_TrayWnd", "Shell_SecondaryTrayWnd"];

    /// <param name="extendedStyle">
    /// Estilo extendido de la ventana. Las capas transparentes que ocupan la pantalla (la de NVIDIA,
    /// grabadores, la del control remoto de Claude…) no son pantalla completa: un juego o un vídeo
    /// nunca dejan pasar el ratón ni rechazan el teclado.
    /// </param>
    public static bool IsFullscreen(PixelRect window, PixelRect monitor, string windowClass, long extendedStyle = 0) =>
        !ShellClasses.Contains(windowClass)
        && (extendedStyle & (TransparentStyle | NoActivateStyle)) == 0
        && window.Covers(monitor);
}
