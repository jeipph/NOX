using System.Runtime.InteropServices;
using static NotchBar.Windows.Shell.NativeMethods;

namespace NotchBar.Windows.Shell;

/// <summary>
/// Avisa cuando se pulsa Esc. El notch nunca tiene el foco del teclado, así que hace falta
/// un gancho de teclado de bajo nivel. Solo se activa mientras el notch está abierto,
/// solo mira la tecla Esc y no la bloquea: la tecla sigue llegando a la app que la esperaba.
/// </summary>
public sealed class EscapeKeyHook : IDisposable
{
    private delegate nint HookProc(int code, nint wParam, nint lParam);

    // Se guarda en un campo para que el recolector de basura no lo borre mientras Windows lo usa.
    private readonly HookProc _proc;
    private nint _hook;

    public event Action? EscapePressed;

    public EscapeKeyHook() => _proc = OnKey;

    public void Start()
    {
        if (_hook != 0)
            return;

        _hook = SetWindowsHookEx(WH_KEYBOARD_LL, Marshal.GetFunctionPointerForDelegate(_proc), GetModuleHandle(null), 0);
    }

    public void Stop()
    {
        if (_hook == 0)
            return;

        UnhookWindowsHookEx(_hook);
        _hook = 0;
    }

    private nint OnKey(int code, nint wParam, nint lParam)
    {
        // El primer campo de KBDLLHOOKSTRUCT es el código de la tecla.
        if (code >= 0 && (int)wParam == WM_KEYDOWN && Marshal.ReadInt32(lParam) == VK_ESCAPE)
            EscapePressed?.Invoke();

        return CallNextHookEx(_hook, code, wParam, lParam);
    }

    public void Dispose() => Stop();
}
