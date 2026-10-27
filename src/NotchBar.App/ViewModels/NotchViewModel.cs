using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NotchBar.Core.Models;

namespace NotchBar.App.ViewModels;

// Opciones elegidas desde el menú de la bandeja. SettingsCoordinator las carga al arrancar
// y las guarda en disco cada vez que cambian.
public partial class NotchViewModel : ObservableObject
{
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

    public void Apply(AppSettings settings)
    {
        Style = settings.Style;
        OnlyOnHover = settings.OnlyOnHover;
        ReserveTopStrip = settings.ReserveTopStrip;
        LyricsEnabled = settings.LyricsEnabled;
        Theme = settings.Theme;
    }

    public AppSettings ToSettings(string? preferredSourceId) => new()
    {
        Style = Style,
        OnlyOnHover = OnlyOnHover,
        ReserveTopStrip = ReserveTopStrip,
        LyricsEnabled = LyricsEnabled,
        Theme = Theme,
        PreferredSourceId = preferredSourceId,
    };
}
