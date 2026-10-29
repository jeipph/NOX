using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using Nox.App.Services;
using Nox.App.ViewModels;
using Nox.Core.Hotkeys;
using Nox.Windows.Shell;

namespace Nox.App.Windows;

/// <summary>Cambiar los atajos: se hace clic en un cuadro y se pulsa la combinación.</summary>
public partial class HotkeysWindow : Window
{
    private readonly HotkeysViewModel _viewModel;
    private readonly ThemeService _theme;

    public HotkeysWindow(HotkeysViewModel viewModel, ThemeService theme)
    {
        InitializeComponent();
        DataContext = _viewModel = viewModel;
        _theme = theme;
        _theme.Changed += UpdateTitleBar;
        SourceInitialized += (_, _) => UpdateTitleBar();
        Closed += (_, _) =>
        {
            _theme.Changed -= UpdateTitleBar;
            _viewModel.Dispose();
        };
    }

    // La barra de título la dibuja Windows: hay que pedirle la oscura cuando NOX está en oscuro.
    private void UpdateTitleBar()
    {
        nint hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd != 0)
            TitleBar.SetDark(hwnd, !_theme.IsLight);
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
