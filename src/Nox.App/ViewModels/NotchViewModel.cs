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

    /// <summary>Si es true, las ventanas maximizadas empiezan debajo del notch.</summary>
    [ObservableProperty]
    private bool _reserveTopStrip;

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

    /// <summary>Minutos entre avisos de descanso; 0 = nunca.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBreak20), nameof(IsBreak40), nameof(IsBreak60), nameof(IsBreakOff))]
    private int _breakMinutes = 40;

    public bool IsBreak20 => BreakMinutes == 20;
    public bool IsBreak40 => BreakMinutes == 40;
    public bool IsBreak60 => BreakMinutes == 60;
    public bool IsBreakOff => BreakMinutes <= 0;

    [RelayCommand]
    private void SetBreakMinutes(string minutes) => BreakMinutes = int.Parse(minutes);

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
        LyricsEnabled = settings.LyricsEnabled;
        Theme = settings.Theme;
        NotesFolder = settings.NotesFolder;
        Hotkeys = settings.Hotkeys;
        Pet = settings.Pet;
        PetToy = settings.PetToy;
        PetInCollapsed = settings.PetInCollapsed;
        BreakMinutes = settings.BreakMinutes;
    }

    public AppSettings ToSettings(string? preferredSourceId) => new()
    {
        Style = Style,
        OnlyOnHover = OnlyOnHover,
        ReserveTopStrip = ReserveTopStrip,
        LyricsEnabled = LyricsEnabled,
        Theme = Theme,
        PreferredSourceId = preferredSourceId,
        NotesFolder = NotesFolder,
        Hotkeys = Hotkeys,
        Pet = Pet,
        PetToy = PetToy,
        PetInCollapsed = PetInCollapsed,
        BreakMinutes = BreakMinutes,
    };
}
