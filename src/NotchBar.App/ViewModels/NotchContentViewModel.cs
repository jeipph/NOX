using System.ComponentModel;
using System.Windows.Threading;
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

/// <summary>Contenido de la pastilla: qué panel se ve, el texto del notch cerrado y avisos de visibilidad.</summary>
public partial class NotchContentViewModel : ObservableObject, IDisposable
{
    // Reloj del notch cerrado: despierta una vez por minuto, justo al cambiar el minuto,
    // y solo mientras no suena nada (si suena, se muestra el título).
    private readonly DispatcherTimer _clockTimer = new();
    private bool _isExpanded;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNowPlayingActive), nameof(IsLyricsActive), nameof(IsVinylActive))]
    private NotchPanel _activePanel = NotchPanel.NowPlaying;

    /// <summary>Texto del notch cerrado: el título de la canción o, si no suena nada, la hora.</summary>
    [ObservableProperty]
    private string _collapsedText = "";

    public NotchContentViewModel(NowPlayingViewModel nowPlaying, LyricsViewModel lyrics, VolumeViewModel volume)
    {
        NowPlaying = nowPlaying;
        Lyrics = lyrics;
        Volume = volume;

        _clockTimer.Tick += (_, _) => UpdateCollapsedText();
        NowPlaying.PropertyChanged += OnNowPlayingChanged;
        UpdateCollapsedText();
    }

    public NowPlayingViewModel NowPlaying { get; }

    public LyricsViewModel Lyrics { get; }

    public VolumeViewModel Volume { get; }

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

    private void OnNowPlayingChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(NowPlayingViewModel.ShortTitle) or nameof(NowPlayingViewModel.HasMedia))
            UpdateCollapsedText();
    }

    private void UpdateCollapsedText()
    {
        _clockTimer.Stop();
        if (NowPlaying.HasMedia)
        {
            CollapsedText = NowPlaying.ShortTitle;
            return;
        }

        DateTime now = DateTime.Now;
        CollapsedText = now.ToString("t"); // hora corta, en el formato de Windows
        _clockTimer.Interval = TimeSpan.FromSeconds(60 - now.Second) - TimeSpan.FromMilliseconds(now.Millisecond);
        _clockTimer.Start();
    }

    public void Dispose()
    {
        _clockTimer.Stop();
        NowPlaying.PropertyChanged -= OnNowPlayingChanged;
    }
}
