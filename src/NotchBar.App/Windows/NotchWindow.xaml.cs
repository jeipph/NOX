using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Microsoft.Win32;
using NotchBar.App.ViewModels;
using NotchBar.Core.Models;
using NotchBar.Core.Shell;
using NotchBar.Windows.Clipboard;
using NotchBar.Windows.Shell;

namespace NotchBar.App.Windows;

public partial class NotchWindow : Window, IDisposable
{
    private readonly NotchViewModel _viewModel;
    private readonly NotchContentViewModel _content;
    private readonly ClipboardMonitor _clipboard;

    // Solo corre mientras espera para contraer; en reposo no hay temporizadores activos.
    private readonly DispatcherTimer _collapseTimer;
    private readonly EscapeKeyHook _escapeKey = new();
    private AppBar? _appBar;
    private nint _hwnd;
    private bool _isExpanded;

    private readonly DispatcherTimer _attentionTimer;

    // "Modo escritura" (pestaña Notas): el notch acepta el teclado y no se cierra solo.
    private bool _isTyping;
    private nint _windowBeforeTyping;

    public NotchWindow(NotchViewModel viewModel, NotchContentViewModel content, ClipboardMonitor clipboard)
    {
        InitializeComponent();

        _viewModel = viewModel;
        _viewModel.PropertyChanged += OnViewModelChanged;
        _content = content;
        _clipboard = clipboard;
        Pill.DataContext = content;

        _collapseTimer = new DispatcherTimer { Interval = (TimeSpan)FindResource("Hover.CollapseDelay") };
        _collapseTimer.Tick += (_, _) =>
        {
            _collapseTimer.Stop();
            if (!_isTyping)
                SetExpanded(false);
        };

        // El gancho de teclado llama desde dentro de Windows: responder rápido y animar después.
        _escapeKey.EscapePressed += () => Dispatcher.BeginInvoke(() => SetExpanded(false));

        Pill.PreviewMouseLeftButtonDown += OnPillPreviewMouseDown;
        Deactivated += (_, _) => ExitTyping(returnFocus: false);

        // Aviso (fin de la concentración): abrir el notch un momento sin necesidad de pasar el ratón.
        _attentionTimer = new DispatcherTimer { Interval = (TimeSpan)FindResource("Attention.Duration") };
        _attentionTimer.Tick += (_, _) =>
        {
            _attentionTimer.Stop();
            if (!Pill.IsMouseOver && !_isTyping)
                SetExpanded(false);
        };
        _content.AttentionRequested += OnAttentionRequested;

        ApplyCollapsedLook();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        _hwnd = new WindowInteropHelper(this).Handle;
        WindowHelper.MakeOverlay(_hwnd);
        HwndSource.FromHwnd(_hwnd).AddHook(WndProc);

        _appBar = new AppBar(_hwnd);
        _appBar.FullscreenChanged += OnFullscreenChanged;
        _appBar.PositionChanged += PlaceWindow;
        _clipboard.Attach(_hwnd);
        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;

        PlaceWindow();
    }

    private nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        // Clic en el notch sin quitarle el foco a la ventana en la que escribías
        // (salvo en modo escritura, donde sí queremos el teclado).
        if (msg == WindowHelper.WmMouseActivate && !_isTyping)
        {
            handled = true;
            return WindowHelper.MaNoActivate;
        }

        // Avisos de "se copió algo" para la pestaña Portapapeles.
        if (_clipboard.HandleMessage(msg))
            handled = true;

        if (_appBar?.HandleMessage(msg, wParam, lParam) == true)
            handled = true;

        // Windows actualiza el área de trabajo un poco después de reservar o liberar la franja;
        // al llegar este aviso se recoloca con los valores ya actualizados.
        if (WindowHelper.IsWorkAreaChanged(msg, wParam))
            Dispatcher.BeginInvoke(PlaceWindow);

        return 0;
    }

    // --- Avisos ---

    private void OnAttentionRequested()
    {
        // Con una app en pantalla completa el notch está oculto: basta con el sonido.
        if (!IsVisible)
            return;

        SetExpanded(true);
        _attentionTimer.Stop();
        _attentionTimer.Start();
    }

    // --- Modo escritura ---

    private void OnPillPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is DependencyObject clicked && FindAncestor<TextBox>(clicked) is not null)
            EnterTyping();
    }

    private void EnterTyping()
    {
        if (_isTyping)
            return;

        _isTyping = true;
        RememberUserWindow();
        WindowHelper.SetActivatable(_hwnd, true);
        Activate();
    }

    /// <param name="returnFocus">True (con Esc): devolver el teclado a la ventana de antes.</param>
    private void ExitTyping(bool returnFocus)
    {
        if (!_isTyping)
            return;

        _isTyping = false;
        Keyboard.ClearFocus();
        WindowHelper.SetActivatable(_hwnd, false);
        if (returnFocus)
            WindowHelper.Activate(_windowBeforeTyping);
        if (!Pill.IsMouseOver)
            _collapseTimer.Start();
    }

    // La ventana donde estabas trabajando, para devolverle el teclado al salir del modo escritura.
    // Probado: al hacer clic en el cuadro de nota, Windows a veces ya ha activado el notch,
    // así que se recuerda también al abrir el notch y nunca se guarda el propio notch.
    private void RememberUserWindow()
    {
        nint active = WindowHelper.ActiveWindow();
        if (active != 0 && active != _hwnd)
            _windowBeforeTyping = active;
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);
        if (e.Key == Key.Escape && _isTyping)
        {
            e.Handled = true;
            SetExpanded(false);
        }
    }

    private static T? FindAncestor<T>(DependencyObject? element) where T : DependencyObject
    {
        while (element is not null and not T)
            element = element is Visual or System.Windows.Media.Media3D.Visual3D
                ? VisualTreeHelper.GetParent(element)
                : LogicalTreeHelper.GetParent(element);
        return element as T;
    }

    private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(NotchViewModel.Style):
                ApplyCollapsedLook();
                PlaceWindow();
                break;
            case nameof(NotchViewModel.OnlyOnHover):
                ApplyCollapsedLook();
                break;
            case nameof(NotchViewModel.ReserveTopStrip):
                PlaceWindow();
                break;
        }
    }

    // --- Posición, escalado y franja reservada ---

    protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
    {
        base.OnDpiChanged(oldDpi, newDpi);
        PlaceWindow();
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs e) => Dispatcher.BeginInvoke(PlaceWindow);

    // WPF mide en unidades independientes del escalado; Windows coloca en píxeles reales.
    private void PlaceWindow()
    {
        if (_hwnd == 0 || _appBar is null)
            return;

        DpiScale dpi = VisualTreeHelper.GetDpi(this);
        var (bounds, workArea) = WindowHelper.GetPrimaryMonitor();
        PixelRect area = workArea;

        if (_viewModel.ReserveTopStrip)
        {
            int stripHeight = (int)Math.Round(CollapsedStripHeight() * dpi.DpiScaleY);
            area = _appBar.ReserveTop(bounds, stripHeight);
        }
        else
        {
            _appBar.ReleaseReservation();
        }

        WindowHelper.PlaceTopCenter(_hwnd, area, (int)Math.Round(Width * dpi.DpiScaleX));
    }

    // Alto que ocupa la pastilla contraída, contando su separación del borde.
    private double CollapsedStripHeight() =>
        (double)FindResource(CollapsedSizeKey + ".Height") + ((Thickness)FindResource(MarginKey)).Top;

    // --- Pantalla completa ---

    private void OnFullscreenChanged(bool isFullscreen)
    {
        if (isFullscreen)
        {
            _collapseTimer.Stop();
            ApplyCollapsedLook();
            Hide();
        }
        else
        {
            Show();
            PlaceWindow();
        }
    }

    // --- Estilo (claves de los tokens según el estilo elegido) ---

    private string CollapsedSizeKey =>
        _viewModel.Style == NotchStyle.Indicator ? "Size.Indicator" : "Size.Collapsed";

    private string CollapsedRadiusKey => _viewModel.Style switch
    {
        NotchStyle.Capsule => "Radius.Capsule.Collapsed",
        NotchStyle.Indicator => "Radius.Indicator",
        _ => "Radius.Notch.Collapsed",
    };

    private string ExpandedRadiusKey =>
        _viewModel.Style == NotchStyle.Capsule ? "Radius.Capsule.Expanded" : "Radius.Notch.Expanded";

    private string MarginKey =>
        _viewModel.Style == NotchStyle.Capsule ? "Margin.Capsule" : "Margin.Notch";

    // Con "solo al pasar el ratón", la pastilla contraída es invisible pero sigue detectando el ratón.
    private string CollapsedBackgroundKey =>
        _viewModel.OnlyOnHover ? "Brush.HitArea" : "Brush.Background";

    // El título corto solo cabe en la pastilla visible (no en el indicador ni en el modo invisible).
    private bool ShowsCollapsedText =>
        !_viewModel.OnlyOnHover && _viewModel.Style != NotchStyle.Indicator;

    // --- Expandir y contraer ---

    private void OnPillMouseEnter(object sender, MouseEventArgs e)
    {
        _collapseTimer.Stop();
        SetExpanded(true);
    }

    private void OnPillMouseLeave(object sender, MouseEventArgs e) => _collapseTimer.Start();

    private void SetExpanded(bool expanded)
    {
        // Cerrar (Esc, o el temporizador) termina el modo escritura y devuelve el teclado a donde estaba.
        if (!expanded)
            ExitTyping(returnFocus: true);

        if (expanded == _isExpanded)
            return;
        _isExpanded = expanded;

        string sizeKey = expanded ? "Size.Expanded" : CollapsedSizeKey;
        var duration = new Duration((TimeSpan)FindResource("Duration.Expand"));
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };

        var widthAnimation = new DoubleAnimation((double)FindResource(sizeKey + ".Width"), duration) { EasingFunction = ease };
        var heightAnimation = new DoubleAnimation((double)FindResource(sizeKey + ".Height"), duration) { EasingFunction = ease };

        if (expanded)
        {
            RememberUserWindow();
            Pill.SetResourceReference(Border.BackgroundProperty, "Brush.Background");
            _escapeKey.Start();
        }
        else
        {
            _escapeKey.Stop();
            // Al terminar de encoger vuelve al aspecto contraído (invisible si toca).
            heightAnimation.Completed += (_, _) =>
            {
                if (!_isExpanded)
                    ApplyCollapsedLook();
            };
        }

        Pill.BeginAnimation(WidthProperty, widthAnimation);
        Pill.BeginAnimation(HeightProperty, heightAnimation);
        Pill.SetResourceReference(Border.CornerRadiusProperty, expanded ? ExpandedRadiusKey : CollapsedRadiusKey);
        ExpandedContent.Visibility = expanded ? Visibility.Visible : Visibility.Collapsed;
        CollapsedContent.Visibility = !expanded && ShowsCollapsedText ? Visibility.Visible : Visibility.Collapsed;
        _content.SetExpanded(expanded);
    }

    // Aspecto contraído del estilo actual, sin animación.
    private void ApplyCollapsedLook()
    {
        ExitTyping(returnFocus: false);
        _isExpanded = false;
        _escapeKey.Stop();

        Pill.BeginAnimation(WidthProperty, null);
        Pill.BeginAnimation(HeightProperty, null);
        Pill.SetResourceReference(WidthProperty, CollapsedSizeKey + ".Width");
        Pill.SetResourceReference(HeightProperty, CollapsedSizeKey + ".Height");
        Pill.SetResourceReference(Border.CornerRadiusProperty, CollapsedRadiusKey);
        Pill.SetResourceReference(MarginProperty, MarginKey);
        Pill.SetResourceReference(Border.BackgroundProperty, CollapsedBackgroundKey);
        ExpandedContent.Visibility = Visibility.Collapsed;
        CollapsedContent.Visibility = ShowsCollapsedText ? Visibility.Visible : Visibility.Collapsed;
        _content.SetExpanded(false);
    }

    public void Dispose()
    {
        _collapseTimer.Stop();
        _escapeKey.Dispose();
        _viewModel.PropertyChanged -= OnViewModelChanged;
        SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
        _appBar?.Dispose();
        _appBar = null;
    }
}
