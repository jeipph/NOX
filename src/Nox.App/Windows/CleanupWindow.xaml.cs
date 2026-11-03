using System.Windows;
using System.Windows.Interop;
using Nox.App.Services;
using Nox.App.ViewModels;
using Nox.Windows.Shell;

namespace Nox.App.Windows;

/// <summary>Ventana normal (con barra de título del tema) para limpiar temporales y liberar RAM.</summary>
public partial class CleanupWindow : Window
{
    private readonly ThemeService _theme;

    public CleanupWindow(CleanupViewModel viewModel, ThemeService theme)
    {
        InitializeComponent();
        DataContext = viewModel;
        _theme = theme;
        _theme.Changed += UpdateTitleBar;
        SourceInitialized += (_, _) => UpdateTitleBar();
        Loaded += async (_, _) => await viewModel.LoadAsync();
        Closed += (_, _) => _theme.Changed -= UpdateTitleBar;
    }

    private void UpdateTitleBar()
    {
        nint hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd != 0)
            TitleBar.SetDark(hwnd, !_theme.IsLight);
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();
}
