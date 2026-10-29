using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Nox.App.ViewModels;
using Nox.Core.Hotkeys;

namespace Nox.App.Windows;

/// <summary>Cambiar los atajos: se hace clic en un cuadro y se pulsa la combinación.</summary>
public partial class HotkeysWindow : Window
{
    private readonly HotkeysViewModel _viewModel;

    public HotkeysWindow(HotkeysViewModel viewModel)
    {
        InitializeComponent();
        DataContext = _viewModel = viewModel;
        Closed += (_, _) => _viewModel.Dispose();
    }

    // Mientras un cuadro tiene el foco, los atajos se apartan: si no, Windows se quedaría la combinación.
    private void OnHotkeyBoxFocused(object sender, KeyboardFocusChangedEventArgs e) => _viewModel.BeginEditing();

    private void OnHotkeyBoxUnfocused(object sender, KeyboardFocusChangedEventArgs e) => _viewModel.EndEditing();

    private void OnHotkeyBoxKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not TextBox { DataContext: HotkeyRowViewModel row })
            return;
        // Con Alt pulsado WPF entrega la tecla en SystemKey.
        Key key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key is Key.Tab)
            return; // Tab sigue sirviendo para pasar al siguiente cuadro
        e.Handled = true;

        HotkeyModifiers modifiers = CurrentModifiers();
        if (key is Key.Back or Key.Delete && modifiers == HotkeyModifiers.None)
        {
            _viewModel.SetHotkey(row, "");
            return;
        }
        if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin)
            return; // aún falta la tecla final

        var hotkey = new Hotkey(modifiers, KeyInterop.VirtualKeyFromKey(key));
        if (hotkey.IsValid)
            _viewModel.SetHotkey(row, hotkey.ToString());
        else
            row.Problem = Hotkey.IsUsableKey(hotkey.Key)
                ? "Añade Ctrl, Alt o Win a la combinación"
                : "Esa tecla no se puede usar en un atajo";
    }

    private static HotkeyModifiers CurrentModifiers()
    {
        var modifiers = HotkeyModifiers.None;
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) modifiers |= HotkeyModifiers.Ctrl;
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt)) modifiers |= HotkeyModifiers.Alt;
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) modifiers |= HotkeyModifiers.Shift;
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Windows)) modifiers |= HotkeyModifiers.Win;
        return modifiers;
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();
}
