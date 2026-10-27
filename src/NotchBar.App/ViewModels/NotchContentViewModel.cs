using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace NotchBar.App.ViewModels;

/// <summary>Paneles del notch abierto (las pestañas de abajo).</summary>
public enum NotchPanel
{
    NowPlaying,
    Lyrics,
    Vinyl,
}

/// <summary>Contenido de la pastilla: qué panel se ve y avisos de visibilidad a cada panel.</summary>
public partial class NotchContentViewModel : ObservableObject
{
    private bool _isExpanded;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNowPlayingActive), nameof(IsLyricsActive), nameof(IsVinylActive))]
    private NotchPanel _activePanel = NotchPanel.NowPlaying;

    public NotchContentViewModel(NowPlayingViewModel nowPlaying, LyricsViewModel lyrics)
    {
        NowPlaying = nowPlaying;
        Lyrics = lyrics;
    }

    public NowPlayingViewModel NowPlaying { get; }

    public LyricsViewModel Lyrics { get; }

    public bool IsNowPlayingActive => ActivePanel == NotchPanel.NowPlaying;

    public bool IsLyricsActive => ActivePanel == NotchPanel.Lyrics;

    public bool IsVinylActive => ActivePanel == NotchPanel.Vinyl;

    public Task StartAsync() => NowPlaying.StartAsync();

    /// <summary>La ventana avisa al abrirse o cerrarse el notch.</summary>
    public void SetExpanded(bool isExpanded)
    {
        _isExpanded = isExpanded;
        UpdateVisibility();
    }

    [RelayCommand]
    private void ShowPanel(NotchPanel panel) => ActivePanel = panel;

    partial void OnActivePanelChanged(NotchPanel value) => UpdateVisibility();

    // Cada panel solo gasta (temporizadores) mientras se ve.
    private void UpdateVisibility()
    {
        NowPlaying.SetVisible(_isExpanded && IsNowPlayingActive);
        Lyrics.SetVisible(_isExpanded && IsLyricsActive);
    }
}
