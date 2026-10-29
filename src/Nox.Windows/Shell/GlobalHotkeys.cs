using Nox.Core.Hotkeys;
using static Nox.Windows.Shell.NativeMethods;

namespace Nox.Windows.Shell;

/// <summary>
/// Atajos de teclado que funcionan desde cualquier app (<c>RegisterHotKey</c>). Windows avisa a la
/// ventana del notch con WM_HOTKEY: no hay gancho de teclado ni se mira cada tecla.
/// Si otra app ya tiene un atajo, Windows lo rechaza y se informa como ocupado.
/// </summary>
public sealed class GlobalHotkeys : IDisposable
{
    private const int FirstId = 0x4E00; // "N": cualquier número de 0 a 0xBFFF sirve para una app
    private readonly List<int> _registered = [];
    private nint _hwnd;

    public event Action<HotkeyAction>? Pressed;

    public void Attach(nint hwnd) => _hwnd = hwnd;

    /// <summary>Cambia todos los atajos por estos.</summary>
    /// <returns>Los que Windows no aceptó (los tiene otra app).</returns>
    public IReadOnlyList<HotkeyAction> Register(IReadOnlyDictionary<HotkeyAction, Hotkey> hotkeys)
    {
        UnregisterAll();
        var taken = new List<HotkeyAction>();
        if (_hwnd == 0)
            return taken;
        foreach ((HotkeyAction action, Hotkey hotkey) in hotkeys)
        {
            int id = FirstId + (int)action;
            // MOD_NOREPEAT: mantener la tecla pulsada no repite la acción (no pausa y reanuda sin parar).
            if (RegisterHotKey(_hwnd, id, (uint)hotkey.Modifiers | MOD_NOREPEAT, (uint)hotkey.Key))
                _registered.Add(id);
            else
                taken.Add(action);
        }
        return taken;
    }

    public void UnregisterAll()
    {
        foreach (int id in _registered)
            UnregisterHotKey(_hwnd, id);
        _registered.Clear();
    }

    /// <summary>Llamar desde el procedimiento de la ventana. True si era uno de nuestros atajos.</summary>
    public bool HandleMessage(int msg, nint wParam)
    {
        if (msg != WM_HOTKEY || !_registered.Contains((int)wParam))
            return false;
        Pressed?.Invoke((HotkeyAction)((int)wParam - FirstId));
        return true;
    }

    public void Dispose() => UnregisterAll();
}
