using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Microsoft.Win32;
using NotchBar.Windows.Shell;

namespace NotchBar.App.Windows;

public partial class NotchWindow : Window, IDisposable
{
    // Solo corre mientras espera para contraer; en reposo no hay temporizadores activos.
    private readonly DispatcherTimer _collapseTimer;
    private FullscreenDetector? _fullscreen;
    private nint _hwnd;
    private bool _isExpanded;

    public NotchWindow()
    {
        InitializeComponent();

        _collapseTimer = new DispatcherTimer { Interval = (TimeSpan)FindResource("Hover.CollapseDelay") };
        _collapseTimer.Tick += (_, _) =>
        {
            _collapseTimer.Stop();
            SetExpanded(false);
        };
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        _hwnd = new WindowInteropHelper(this).Handle;
        WindowHelper.MakeOverlay(_hwnd);
        HwndSource.FromHwnd(_hwnd).AddHook(WndProc);

        _fullscreen = new FullscreenDetector(_hwnd);
        _fullscreen.FullscreenChanged += OnFullscreenChanged;
        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;

        PlaceAtTopCenter(VisualTreeHelper.GetDpi(this));
    }

    private nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        // Clic en el notch sin quitarle el foco a la ventana en la que escribías.
        if (msg == WindowHelper.WmMouseActivate)
        {
            handled = true;
            return WindowHelper.MaNoActivate;
        }

        if (_fullscreen?.HandleMessage(msg, wParam, lParam) == true)
            handled = true;

        return 0;
    }

    // --- Posición y escalado ---

    protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
    {
        base.OnDpiChanged(oldDpi, newDpi);
        PlaceAtTopCenter(newDpi);
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs e) =>
        Dispatcher.BeginInvoke(() => PlaceAtTopCenter(VisualTreeHelper.GetDpi(this)));

    // WPF mide en unidades independientes del escalado; Windows coloca en píxeles reales.
    private void PlaceAtTopCenter(DpiScale dpi)
    {
        int pixelWidth = (int)Math.Round(Width * dpi.DpiScaleX);
        WindowHelper.PlaceTopCenter(_hwnd, WindowHelper.GetPrimaryWorkArea(), pixelWidth);
    }

    // --- Pantalla completa ---

    private void OnFullscreenChanged(bool isFullscreen)
    {
        if (isFullscreen)
        {
            _collapseTimer.Stop();
            CollapseImmediately();
            Hide();
        }
        else
        {
            Show();
            PlaceAtTopCenter(VisualTreeHelper.GetDpi(this));
        }
    }

    // --- Expandir y contraer ---

    private void OnPillMouseEnter(object sender, MouseEventArgs e)
    {
        _collapseTimer.Stop();
        SetExpanded(true);
    }

    private void OnPillMouseLeave(object sender, MouseEventArgs e) => _collapseTimer.Start();

    private void SetExpanded(bool expanded)
    {
        if (expanded == _isExpanded)
            return;
        _isExpanded = expanded;

        string size = expanded ? "Size.Expanded" : "Size.Collapsed";
        var duration = new Duration((TimeSpan)FindResource("Duration.Expand"));
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };

        Pill.BeginAnimation(WidthProperty,
            new DoubleAnimation((double)FindResource(size + ".Width"), duration) { EasingFunction = ease });
        Pill.BeginAnimation(HeightProperty,
            new DoubleAnimation((double)FindResource(size + ".Height"), duration) { EasingFunction = ease });

        Pill.SetResourceReference(System.Windows.Controls.Border.CornerRadiusProperty,
            expanded ? "Radius.Notch.Expanded" : "Radius.Notch.Collapsed");
        ExpandedContent.Visibility = expanded ? Visibility.Visible : Visibility.Collapsed;
    }

    // Quitar las animaciones devuelve la pastilla a su tamaño contraído (el de los tokens).
    private void CollapseImmediately()
    {
        _isExpanded = false;
        Pill.BeginAnimation(WidthProperty, null);
        Pill.BeginAnimation(HeightProperty, null);
        Pill.SetResourceReference(System.Windows.Controls.Border.CornerRadiusProperty, "Radius.Notch.Collapsed");
        ExpandedContent.Visibility = Visibility.Collapsed;
    }

    public void Dispose()
    {
        _collapseTimer.Stop();
        SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
        _fullscreen?.Dispose();
        _fullscreen = null;
    }
}
