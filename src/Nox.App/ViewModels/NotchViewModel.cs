using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Nox.Core.Abstractions;
using Nox.Core.Hotkeys;
using Nox.Core.Pets;
using Nox.Core.Models;
using Nox.Core.Notes;

namespace Nox.App.ViewModels;

// Opciones elegidas desde el menú de la bandeja. SettingsCoordinator las carga al arrancar
// y las guarda en disco cada vez que cambian.
public partial class NotchViewModel : ObservableObject
{
    private readonly IKeepAwake _keepAwake;

    public NotchViewModel(IKeepAwake keepAwake) => _keepAwake = keepAwake;

    /// <summary>
    /// El PC no se suspende ni apaga la pantalla. No se guarda: al abrir NOX empieza apagado,
    /// para no dejar el PC encendido toda la noche por olvido.
    /// </summary>
    [ObservableProperty]
    private bool _keepAwakeEnabled;

    partial void OnKeepAwakeEnabledChanged(bool value)
    {
        _keepAwake.SetEnabled(value);
        if (_keepAwake.IsEnabled != value)
            KeepAwakeEnabled = _keepAwake.IsEnabled; // Windows no lo aceptó
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotchStyle), nameof(IsCapsuleStyle), nameof(IsIndicatorStyle))]
    private NotchStyle _style = NotchStyle.Notch;

    /// <summary>Si es true, contraído no se ve nada hasta pasar el ratón por arriba.</summary>
    [ObservableProperty]
    private bool _onlyOnHover;

    /// <summary>Si es true, las ventanas maximizadas empiezan debajo del notch (solo arriba al centro).</summary>
    [ObservableProperty]
    private bool _reserveTopStrip;

    /// <summary>
    /// Dónde se dejó el notch al arrastrarlo: centro de la pastilla cerrada como fracción del área
    /// de trabajo. Null = arriba al centro.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasCustomPosition))]
    [NotifyCanExecuteChangedFor(nameof(ResetPositionCommand))]
    private (double X, double Y)? _position;

    public bool HasCustomPosition => Position is not null;

    /// <summary>«Volver arriba al centro»: olvida la posición elegida.</summary>
    [RelayCommand(CanExecute = nameof(HasCustomPosition))]
    private void ResetPosition() => Position = null;

    /// <summary>
    /// El notch se ocultó con su botón ×. NOX sigue en la bandeja; vuelve con el atajo de abrir/cerrar
    /// o con «Mostrar el notch». No se guarda: al abrir NOX se ve otra vez.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ShowNotchCommand))]
    private bool _isNotchHidden;

    [RelayCommand]
    private void HideNotch() => IsNotchHidden = true;

    [RelayCommand(CanExecute = nameof(IsNotchHidden))]
    private void ShowNotch() => IsNotchHidden = false;

    /// <summary>Permite buscar letras en lrclib.net (envía título, artista y duración).</summary>
    [ObservableProperty]
    private bool _lyricsEnabled = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSystemTheme), nameof(IsDarkTheme), nameof(IsLightTheme))]
    private AppTheme _theme = AppTheme.System;

    [ObservableProperty]
    private bool _startWithWindows;

    /// <summary>Carpeta de las notas rápidas; null = la de por defecto.</summary>
    [ObservableProperty]
    private string? _notesFolder;

    [ObservableProperty]
    private HotkeySettings _hotkeys = new();

    [ObservableProperty]
    private PetKind _pet = PetKind.GrayCat;

    [ObservableProperty]
    private PetToy _petToy = PetToy.Yarn;

    [ObservableProperty]
    private bool _petInCollapsed = true;

    /// <summary>Minutos entre avisos de descanso; 0 = nunca. El menú marca la opción con EqualsConverter.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBreakCustom), nameof(CustomBreakHeader))]
    private int _breakMinutes = 40;

    /// <summary>Un intervalo escrito en «Otro…» (no es ninguna opción fija del menú).</summary>
    public bool IsBreakCustom => BreakIntervals.IsCustom(BreakMinutes);

    public string CustomBreakHeader => IsBreakCustom ? $"Otro… ({BreakMinutes} min)" : "Otro…";

    [RelayCommand]
    private void SetBreakMinutes(string minutes) => BreakMinutes = int.Parse(minutes);

    /// <summary>«Otro…»: la ventanita para escribir los minutos la abre App.</summary>
    public event Action? CustomBreakRequested;

    [RelayCommand]
    private void ChooseCustomBreak() => CustomBreakRequested?.Invoke();

    /// <summary>Hambre y popós de la mascota (lo lleva PetViewModel; aquí solo para guardarlo).</summary>
    [ObservableProperty]
    private PetCareState? _petCare;

    /// <summary>«Limpiar caché y RAM…»: la ventana la abre App.</summary>
    public event Action? CleanupWindowRequested;

    [RelayCommand]
    private void OpenCleanup() => CleanupWindowRequested?.Invoke();

    /// <summary>"Descansar ahora": la mascota aparece ya (también sirve para verla sin esperar).</summary>
    public event Action? BreakNowRequested;

    [RelayCommand]
    private void BreakNow() => BreakNowRequested?.Invoke();

    /// <summary>Atajos que no funcionan (ocupados, repetidos…): se avisa en el menú.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HotkeysMenuHeader))]
    private int _hotkeyProblemCount;

    public string HotkeysMenuHeader => HotkeyProblemCount == 0
        ? "Atajos de teclado…"
        : $"Atajos de teclado… (⚠ {HotkeyProblemCount} sin funcionar)";

    /// <summary>La ventana de atajos la abre App (necesita servicios que este ViewModel no conoce).</summary>
    public event Action? HotkeysWindowRequested;

    [RelayCommand]
    private void OpenHotkeys() => HotkeysWindowRequested?.Invoke();

    public bool IsNotchStyle => Style == NotchStyle.Notch;
    public bool IsCapsuleStyle => Style == NotchStyle.Capsule;
    public bool IsIndicatorStyle => Style == NotchStyle.Indicator;

    public bool IsSystemTheme => Theme == AppTheme.System;
    public bool IsDarkTheme => Theme == AppTheme.Dark;
    public bool IsLightTheme => Theme == AppTheme.Light;

    [RelayCommand]
    private void SetStyle(NotchStyle style) => Style = style;

    [RelayCommand]
    private void SetTheme(AppTheme theme) => Theme = theme;

    [RelayCommand]
    private void ChooseNotesFolder()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Carpeta para las notas rápidas",
            InitialDirectory = NotesFolder ?? NotesStore.DefaultFolder,
        };
        if (dialog.ShowDialog() == true)
            NotesFolder = dialog.FolderName;
    }

    public void Apply(AppSettings settings)
    {
        Style = settings.Style;
        OnlyOnHover = settings.OnlyOnHover;
        ReserveTopStrip = settings.ReserveTopStrip;
        Position = settings.PositionX is double x && settings.PositionY is double y
            ? (Math.Clamp(x, 0, 1), Math.Clamp(y, 0, 1))
            : null;
        LyricsEnabled = settings.LyricsEnabled;
        Theme = settings.Theme;
        NotesFolder = settings.NotesFolder;
        Hotkeys = settings.Hotkeys;
        Pet = settings.Pet;
        PetToy = settings.PetToy;
        PetInCollapsed = settings.PetInCollapsed;
        BreakMinutes = Math.Clamp(settings.BreakMinutes, 0, BreakIntervals.MaxMinutes);
        PetCare = settings.PetCare;
    }

    public AppSettings ToSettings(string? preferredSourceId) => new()
    {
        Style = Style,
        OnlyOnHover = OnlyOnHover,
        ReserveTopStrip = ReserveTopStrip,
        PositionX = Position?.X,
        PositionY = Position?.Y,
        LyricsEnabled = LyricsEnabled,
        Theme = Theme,
        PreferredSourceId = preferredSourceId,
        NotesFolder = NotesFolder,
        Hotkeys = Hotkeys,
        Pet = Pet,
        PetToy = PetToy,
        PetInCollapsed = PetInCollapsed,
        BreakMinutes = BreakMinutes,
        PetCare = PetCare,
    };
}
