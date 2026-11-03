using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Microsoft.Win32;
using Nox.App.Services;
using Nox.App.ViewModels;
using Nox.Core.Models;
using Nox.Core.Shell;
using Nox.Windows.Clipboard;
using Nox.Windows.Shell;

namespace Nox.App.Windows;

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

    private readonly GlobalHotkeys _hotkeys;
    private readonly HotkeyService _hotkeyService;

    // --- Posición (todo en píxeles físicos) ---
    // Desplazamiento visual de la pastilla: cuando la ventana se recoloca (para que el notch abierto
    // no se salga de la pantalla), empieza en lo que se movió y se anima a 0, así no se ve un salto.
    private readonly TranslateTransform _pillShift = new();
    private PixelRect _workArea;
    private (int X, int Y) _home;   // dónde va la ventana con el notch cerrado
    private bool _placed;
    private bool _growsUp;          // en la mitad de abajo: la pastilla cuelga del borde de abajo

    // Arrastre con el asa: dónde estaban el ratón y la ventana al empezar.
    private bool _isDragging;
    private (int X, int Y) _dragCursorStart;
    private (int X, int Y) _dragWindowStart;

    public NotchWindow(NotchViewModel viewModel, NotchContentViewModel content, ClipboardMonitor clipboard,
        GlobalHotkeys hotkeys, HotkeyService hotkeyService)
    {
        InitializeComponent();

        _viewModel = viewModel;
        _viewModel.PropertyChanged += OnViewModelChanged;
        _content = content;
        _clipboard = clipboard;
        _hotkeys = hotkeys;
        _hotkeyService = hotkeyService;
        _hotkeyService.ToggleNotchRequested += OnToggleRequested;
        Pill.DataContext = content;
        Pill.RenderTransform = _pillShift;
        _content.Pet.PropertyChanged += OnPetChanged;

        UIElement handle = ExpandedContent.DragHandle;
        handle.MouseLeftButtonDown += OnDragHandleDown;
        handle.MouseMove += OnDragHandleMove;
        handle.MouseLeftButtonUp += (_, _) => handle.ReleaseMouseCapture();
        handle.LostMouseCapture += (_, _) => EndDrag();
        ExpandedContent.CloseClicked += () => _viewModel.HideNotchCommand.Execute(null);

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
        _appBar.PositionChanged += () => PlaceWindow();
        _clipboard.Attach(_hwnd);
        _hotkeys.Attach(_hwnd);
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

        // Atajos de teclado globales (Ctrl+Alt+N…).
        if (_hotkeys.HandleMessage(msg, wParam))
            handled = true;

        if (_appBar?.HandleMessage(msg, wParam, lParam) == true)
            handled = true;

        // Windows actualiza el área de trabajo un poco después de reservar o liberar la franja;
        // al llegar este aviso se recoloca con los valores ya actualizados.
        if (WindowHelper.IsWorkAreaChanged(msg, wParam))
            Dispatcher.BeginInvoke(() => PlaceWindow());

        return 0;
    }

    // --- Avisos ---

    /// <summary>Hay una app en pantalla completa (el notch está escondido): el aviso de descanso espera.</summary>
    public bool IsFullscreenAppActive => _appBar?.IsFullscreen == true;

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
            case nameof(NotchViewModel.Position):
                // Al soltarlo tras arrastrar, o «Volver arriba al centro»: se desliza hasta su sitio.
                PlaceWindow(animate: true);
                break;
            case nameof(NotchViewModel.IsNotchHidden):
                UpdateShown();
                break;
            case nameof(NotchViewModel.Pet) or nameof(NotchViewModel.PetInCollapsed):
                UpdateCollapsedPet();
                break;
        }
    }

    // Con hambre, la mascota del notch cerrado abre la boca.
    private void OnPetChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PetViewModel.IsHungry))
            UpdateCollapsedPet();
    }

    // Se ve salvo que se haya ocultado con × o haya una app en pantalla completa.
    private void UpdateShown()
    {
        bool show = !_viewModel.IsNotchHidden && !IsFullscreenAppActive;
        if (show == IsVisible)
            return;
        if (show)
        {
            Show();
            PlaceWindow();
        }
        else
        {
            _collapseTimer.Stop();
            _attentionTimer.Stop();
            ApplyCollapsedLook();
            Hide();
        }
    }

    // --- Posición, escalado y franja reservada ---

    protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
    {
        base.OnDpiChanged(oldDpi, newDpi);
        PlaceWindow();
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs e) => Dispatcher.BeginInvoke(() => PlaceWindow());

    // WPF mide en unidades independientes del escalado; Windows coloca en píxeles reales.
    // El escalado es el del monitor principal (donde vive el notch), no el de donde esté la ventana
    // ahora: con monitores de distinto escalado, la primera vez la ventana puede nacer en el otro.
    private static double Scale => WindowHelper.GetPrimaryMonitorScale();

    private static int Px(double units, double scale) => (int)Math.Round(units * scale);

    /// <param name="animate">True: la pastilla se desliza desde donde estaba en vez de saltar.</param>
    private void PlaceWindow(bool animate = false)
    {
        if (_hwnd == 0 || _appBar is null || _isDragging)
            return;

        double scale = Scale;
        var (bounds, workArea) = WindowHelper.GetPrimaryMonitor();
        _workArea = workArea;
        int windowWidth = Px(Width, scale), windowHeight = Px(Height, scale);
        bool growsUp = false;

        if (_viewModel.Position is (double fractionX, double fractionY))
        {
            // En otro sitio la franja reservada no tiene sentido (no está arriba).
            _appBar.ReleaseReservation();
            growsUp = NotchLayout.GrowsUp(fractionY);
            var (pillWidth, pillHeight) = PillSizePx(expanded: false, scale);
            var (centerX, centerY) = NotchLayout.PillCenter(workArea, fractionX, fractionY, pillWidth, pillHeight);
            _home = NotchLayout.WindowForPill(centerX, centerY, windowWidth, windowHeight, pillHeight,
                EdgeGapPx(growsUp, scale), growsUp);
        }
        else
        {
            PixelRect area = workArea;
            if (_viewModel.ReserveTopStrip)
                area = _appBar.ReserveTop(bounds, Px(CollapsedStripHeight(), scale));
            else
                _appBar.ReleaseReservation();
            _home = (area.Left + (area.Width - windowWidth) / 2, area.Top);
        }

        PixelRect before = WindowHelper.GetWindowBounds(_hwnd);
        bool grewUp = _growsUp;
        SetGrowsUp(growsUp);
        _placed = true;
        MoveWindow(before, grewUp, _isExpanded ? FitExpanded(_home, scale) : _home, animate);
    }

    // Alto que ocupa la pastilla contraída arriba del todo, contando su separación del borde.
    private double CollapsedStripHeight() =>
        (double)FindResource(CollapsedSizeKey + ".Height") + ((Thickness)FindResource(MarginKeyFor(growsUp: false))).Top;

    private (int Width, int Height) PillSizePx(bool expanded, double scale)
    {
        string key = expanded ? "Size.Expanded" : CollapsedSizeKey;
        return (Px((double)FindResource(key + ".Width"), scale), Px((double)FindResource(key + ".Height"), scale));
    }

    // Separación entre la pastilla y el borde de la ventana del que cuelga (solo la Cápsula tiene).
    private int EdgeGapPx(bool growsUp, double scale)
    {
        var margin = (Thickness)FindResource(MarginKeyFor(growsUp));
        return Px(growsUp ? margin.Bottom : margin.Top, scale);
    }

    // Dónde poner la ventana para que el notch abierto quepa entero en la pantalla.
    private (int X, int Y) FitExpanded((int X, int Y) window, double scale)
    {
        var (width, height) = PillSizePx(expanded: true, scale);
        PixelRect pill = NotchLayout.PillRect(window.X, window.Y, Px(Width, scale), Px(Height, scale),
            width, height, EdgeGapPx(_growsUp, scale), _growsUp);
        var (dx, dy) = NotchLayout.ShiftToFit(_workArea, pill);
        return (window.X + dx, window.Y + dy);
    }

    // En la mitad de abajo la pastilla cuelga del borde de abajo de la ventana y crece hacia arriba.
    private void SetGrowsUp(bool growsUp)
    {
        _growsUp = growsUp;
        var alignment = growsUp ? VerticalAlignment.Bottom : VerticalAlignment.Top;
        Pill.VerticalAlignment = alignment;
        ExpandedContent.VerticalAlignment = alignment;
        Pill.SetResourceReference(MarginProperty, MarginKey);
        Pill.SetResourceReference(Border.CornerRadiusProperty, _isExpanded ? ExpandedRadiusKey : CollapsedRadiusKey);
    }

    /// <summary>
    /// Mueve la ventana. Con <paramref name="animate"/>, la pastilla empieza dibujada donde se veía
    /// antes (de la ventana vieja con su alineación vieja) y se desliza hasta su sitio nuevo.
    /// </summary>
    private void MoveWindow(PixelRect before, bool grewUp, (int X, int Y) target, bool animate)
    {
        double scale = Scale;
        bool moved = before.Left != target.X || before.Top != target.Y;
        WindowHelper.MoveTo(_hwnd, target.X, target.Y);

        if (!animate || !IsVisible || before.Width == 0)
        {
            // Sin animación, la pastilla se dibuja ya en su sitio (si quedaba un deslizamiento, se corta).
            if (moved)
            {
                _pillShift.BeginAnimation(TranslateTransform.XProperty, null);
                _pillShift.BeginAnimation(TranslateTransform.YProperty, null);
            }
            return;
        }

        var (width, height) = PillSizePx(_isExpanded, scale);
        int windowWidth = Px(Width, scale), windowHeight = Px(Height, scale);
        PixelRect from = NotchLayout.PillRect(before.Left, before.Top, windowWidth, windowHeight, width, height,
            EdgeGapPx(grewUp, scale), grewUp);
        PixelRect to = NotchLayout.PillRect(target.X, target.Y, windowWidth, windowHeight, width, height,
            EdgeGapPx(_growsUp, scale), _growsUp);
        if (from == to)
            return;

        // Se suma a lo que quede de un deslizamiento anterior (abrir y cerrar muy rápido).
        var duration = new Duration((TimeSpan)FindResource("Duration.Expand"));
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        _pillShift.BeginAnimation(TranslateTransform.XProperty,
            new DoubleAnimation(_pillShift.X + (from.Left - to.Left) / scale, 0, duration) { EasingFunction = ease });
        _pillShift.BeginAnimation(TranslateTransform.YProperty,
            new DoubleAnimation(_pillShift.Y + (from.Top - to.Top) / scale, 0, duration) { EasingFunction = ease });
    }

    private void MoveWindowTo((int X, int Y) target, bool animate)
    {
        if (_hwnd == 0 || !_placed)
            return;
        MoveWindow(WindowHelper.GetWindowBounds(_hwnd), _growsUp, target, animate);
    }

    // --- Arrastrar con el asa ---
    // La ventana sigue al ratón en vivo (en píxeles físicos, para que no se desfase con el escalado).
    // Mientras se arrastra no se cierra. Al soltar se guarda dónde quedó.

    private void OnDragHandleDown(object sender, MouseButtonEventArgs e)
    {
        if (_hwnd == 0 || sender is not UIElement handle)
            return;
        e.Handled = true;
        _isDragging = true;
        _collapseTimer.Stop();
        _attentionTimer.Stop();
        _pillShift.BeginAnimation(TranslateTransform.XProperty, null);
        _pillShift.BeginAnimation(TranslateTransform.YProperty, null);
        _dragCursorStart = WindowHelper.GetCursorPosition();
        PixelRect bounds = WindowHelper.GetWindowBounds(_hwnd);
        _dragWindowStart = (bounds.Left, bounds.Top);
        handle.CaptureMouse();
    }

    private void OnDragHandleMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging)
            return;
        var (x, y) = WindowHelper.GetCursorPosition();
        WindowHelper.MoveTo(_hwnd, _dragWindowStart.X + x - _dragCursorStart.X, _dragWindowStart.Y + y - _dragCursorStart.Y);
    }

    private void EndDrag()
    {
        if (!_isDragging)
            return;
        _isDragging = false;

        double scale = Scale;
        var (_, workArea) = WindowHelper.GetPrimaryMonitor();
        PixelRect window = WindowHelper.GetWindowBounds(_hwnd);
        var (width, height) = PillSizePx(expanded: true, scale);
        PixelRect expanded = NotchLayout.PillRect(window.Left, window.Top, window.Width, window.Height,
            width, height, EdgeGapPx(_growsUp, scale), _growsUp);
        var (_, collapsedHeight) = PillSizePx(expanded: false, scale);
        var (centerX, centerY, _) = NotchLayout.PillCenterAfterDrag(workArea, expanded, collapsedHeight);

        // Guardar la posición recoloca la ventana (OnViewModelChanged → PlaceWindow).
        var position = NotchLayout.ToFraction(workArea, centerX, centerY);
        if (_viewModel.Position == position)
            PlaceWindow(animate: true);
        else
            _viewModel.Position = position;

        if (!Pill.IsMouseOver)
            _collapseTimer.Start();
    }

    // --- Pantalla completa ---

    // Al salir de la pantalla completa vuelve a verse, salvo que se hubiera ocultado con ×.
    private void OnFullscreenChanged(bool isFullscreen) => UpdateShown();

    // --- Estilo (claves de los tokens según el estilo elegido) ---

    private string CollapsedSizeKey =>
        _viewModel.Style == NotchStyle.Indicator ? "Size.Indicator" : "Size.Collapsed";

    // Creciendo hacia arriba se usan las versiones ".Up" (esquinas y separación al revés).
    private string Up => _growsUp ? ".Up" : "";

    private string CollapsedRadiusKey => _viewModel.Style switch
    {
        NotchStyle.Capsule => "Radius.Capsule.Collapsed",
        NotchStyle.Indicator => "Radius.Indicator",
        _ => "Radius.Notch.Collapsed",
    } + Up;

    private string ExpandedRadiusKey =>
        (_viewModel.Style == NotchStyle.Capsule ? "Radius.Capsule.Expanded" : "Radius.Notch.Expanded") + Up;

    private string MarginKey => MarginKeyFor(_growsUp);

    private string MarginKeyFor(bool growsUp) =>
        (_viewModel.Style == NotchStyle.Capsule ? "Margin.Capsule" : "Margin.Notch") + (growsUp ? ".Up" : "");

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

    private void OnPillMouseLeave(object sender, MouseEventArgs e)
    {
        if (!_isDragging)
            _collapseTimer.Start();
    }

    // Atajo "abrir o cerrar": se queda abierto hasta pulsarlo otra vez, Esc o pasar el ratón y salir.
    // Si se había ocultado con ×, lo vuelve a mostrar (abierto).
    private void OnToggleRequested()
    {
        if (_viewModel.IsNotchHidden)
        {
            _viewModel.IsNotchHidden = false;
            if (IsVisible)
            {
                _collapseTimer.Stop();
                SetExpanded(true);
            }
            return;
        }
        if (!IsVisible) // oculto por una app en pantalla completa
            return;
        _collapseTimer.Stop();
        SetExpanded(!_isExpanded);
    }

    private void SetExpanded(bool expanded)
    {
        // Cerrar (Esc, o el temporizador) termina el modo escritura y devuelve el teclado a donde estaba.
        if (!expanded && _isDragging)
            return; // mientras se arrastra no se cierra
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
        UpdateCollapsedPet();
        _content.SetExpanded(expanded);

        // Abierto no debe salirse de la pantalla: la ventana se corre lo necesario y la pastilla se
        // desliza (sin salto); al cerrar vuelve a su sitio.
        MoveWindowTo(expanded ? FitExpanded(_home, Scale) : _home, animate: true);
    }

    // La mascota sentada a la izquierda del notch cerrado (solo donde también se ve el texto).
    private void UpdateCollapsedPet()
    {
        bool show = !_isExpanded && ShowsCollapsedText && _viewModel.PetInCollapsed;
        CollapsedPet.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        CollapsedContent.SetResourceReference(MarginProperty, show ? "Padding.CollapsedWithPet" : "Padding.Collapsed");
        if (!show)
            return;
        double scale = (double)FindResource("Pet.CollapsedScale");
        var pose = _content.Pet.IsHungry ? Nox.Core.Pets.PetPose.Talk : Nox.Core.Pets.PetPose.Sit;
        CollapsedPet.Source = PetBitmaps.Pet(_viewModel.Pet, pose);
        CollapsedPet.Width = Nox.Core.Pets.PetSprites.Width * scale;
        CollapsedPet.Height = Nox.Core.Pets.PetSprites.Height * scale;
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
        UpdateCollapsedPet();
        _content.SetExpanded(false);
        if (!_isDragging)
            MoveWindowTo(_home, animate: false);
    }

    public void Dispose()
    {
        _collapseTimer.Stop();
        _escapeKey.Dispose();
        _hotkeyService.ToggleNotchRequested -= OnToggleRequested;
        _viewModel.PropertyChanged -= OnViewModelChanged;
        _content.Pet.PropertyChanged -= OnPetChanged;
        SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
        _appBar?.Dispose();
        _appBar = null;
    }
}
