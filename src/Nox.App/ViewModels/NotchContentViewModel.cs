using System.ComponentModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Nox.App.ViewModels;

/// <summary>Paneles del notch abierto (las pestañas de abajo).</summary>
public enum NotchPanel
{
    NowPlaying,
    Lyrics,
    Vinyl,
    Clipboard,
    Notes,
    Focus,
    Pet,
}

/// <summary>Contenido de la pastilla: qué panel se ve, el texto del notch cerrado y avisos de visibilidad.</summary>
public partial class NotchContentViewModel : ObservableObject, IDisposable
{
    // Reloj del notch cerrado: despierta una vez por minuto, justo al cambiar el minuto,
    // y solo mientras no suena nada ni hay una sesión de concentración.
    private readonly DispatcherTimer _clockTimer = new();
    private bool _isExpanded;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNowPlayingActive), nameof(IsLyricsActive), nameof(IsVinylActive),
        nameof(IsClipboardActive), nameof(IsNotesActive), nameof(IsFocusActive), nameof(IsPetActive))]
    private NotchPanel _activePanel = NotchPanel.NowPlaying;

    /// <summary>Texto del notch cerrado, por prioridad: tiempo de concentración, canción, hora.</summary>
    [ObservableProperty]
    private string _collapsedText = "";

    public NotchContentViewModel(
        NowPlayingViewModel nowPlaying,
        LyricsViewModel lyrics,
        VolumeViewModel volume,
        ClipboardViewModel clipboard,
        NotesViewModel notes,
        FocusViewModel focus,
        PetViewModel pet)
    {
        Pet = pet;
        NowPlaying = nowPlaying;
        Lyrics = lyrics;
        Volume = volume;
        Clipboard = clipboard;
        Notes = notes;
        Focus = focus;

        _clockTimer.Tick += (_, _) => UpdateCollapsedText();
        NowPlaying.PropertyChanged += OnPartChanged;
        Focus.PropertyChanged += OnPartChanged;
        Focus.Finished += OnFocusFinished;
        UpdateCollapsedText();
    }

    /// <summary>Algo importante (fin de la concentración): la ventana abre el notch un momento.</summary>
    public event Action? AttentionRequested;

    public NowPlayingViewModel NowPlaying { get; }

    public LyricsViewModel Lyrics { get; }

    public VolumeViewModel Volume { get; }

    public ClipboardViewModel Clipboard { get; }

    public NotesViewModel Notes { get; }

    public FocusViewModel Focus { get; }

    public PetViewModel Pet { get; }

    public bool IsPetActive => ActivePanel == NotchPanel.Pet;

    public bool IsNowPlayingActive => ActivePanel == NotchPanel.NowPlaying;

    public bool IsLyricsActive => ActivePanel == NotchPanel.Lyrics;

    public bool IsVinylActive => ActivePanel == NotchPanel.Vinyl;

    public bool IsClipboardActive => ActivePanel == NotchPanel.Clipboard;

    public bool IsNotesActive => ActivePanel == NotchPanel.Notes;

    public bool IsFocusActive => ActivePanel == NotchPanel.Focus;

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
        Notes.SetVisible(_isExpanded && IsNotesActive);
    }

    private void OnFocusFinished()
    {
        ActivePanel = NotchPanel.Focus;
        AttentionRequested?.Invoke();
    }

    private void OnPartChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(NowPlayingViewModel.ShortTitle) or nameof(NowPlayingViewModel.HasMedia)
            or nameof(FocusViewModel.RemainingText) or nameof(FocusViewModel.State))
            UpdateCollapsedText();
    }

    private void UpdateCollapsedText()
    {
        _clockTimer.Stop();

        if (Focus.IsActive)
        {
            CollapsedText = Focus.IsPaused ? $"En pausa · {Focus.RemainingText}" : $"Concentración · {Focus.RemainingText}";
            return;
        }
        if (Focus.IsFinished)
        {
            CollapsedText = "¡Tiempo cumplido!";
            return;
        }
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
        NowPlaying.PropertyChanged -= OnPartChanged;
        Focus.PropertyChanged -= OnPartChanged;
        Focus.Finished -= OnFocusFinished;
    }
}
