using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Nox.App.Services;
using Nox.App.ViewModels;
using Nox.Windows.Shell;

namespace Nox.App.Windows;

/// <summary>«Otro…» del menú de descansos: Intro acepta y Esc cancela.</summary>
public partial class CustomBreakWindow : Window
{
    private readonly CustomBreakViewModel _viewModel;
    private readonly ThemeService _theme;

    public CustomBreakWindow(CustomBreakViewModel viewModel, ThemeService theme)
    {
        InitializeComponent();
        DataContext = _viewModel = viewModel;
        _theme = theme;
        _viewModel.Accepted += Close;
        _theme.Changed += UpdateTitleBar;
        SourceInitialized += (_, _) => UpdateTitleBar();
        Loaded += (_, _) =>
        {
            MinutesBox.Focus();
            MinutesBox.SelectAll();
        };
        Closed += (_, _) =>
        {
            _theme.Changed -= UpdateTitleBar;
            _viewModel.Accepted -= Close;
        };
    }

    private void UpdateTitleBar()
    {
        nint hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd != 0)
            TitleBar.SetDark(hwnd, !_theme.IsLight);
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            _viewModel.AcceptCommand.Execute(null);
        }
        else if (e.Key == Key.Escape)
        {
            e.Handled = true;
            Close();
        }
    }

    private void OnCancelClick(object sender, RoutedEventArgs e) => Close();
}
