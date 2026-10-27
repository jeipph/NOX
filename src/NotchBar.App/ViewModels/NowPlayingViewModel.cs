using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NotchBar.Core.Abstractions;
using NotchBar.Core.Media;
using NotchBar.Core.Models;
using NotchBar.Core.Text;

namespace NotchBar.App.ViewModels;

public partial class NowPlayingViewModel : ObservableObject, IDisposable
{
    private const int CollapsedTitleLength = 24;

    private readonly IMediaService _media;
    private readonly Dispatcher _dispatcher;

    // Solo corre con el notch abierto y la música sonando.
    private readonly DispatcherTimer _progressTimer;
    private MediaInfo? _info;
    private bool _isVisible;

    [ObservableProperty]
    private string _title = "";

    [ObservableProperty]
    private string _artist = "";

    [ObservableProperty]
    private string _shortTitle = "";

    [ObservableProperty]
    private ImageSource? _cover;

    [ObservableProperty]
    private bool _isPlaying;

    [ObservableProperty]
    private bool _hasMedia;

    [ObservableProperty]
    private double _progress;

    /// <summary>Texto de la etiqueta del reproductor: "Auto · Chrome" o "MusicBee" (fijado).</summary>
    [ObservableProperty]
    private string _sourceLabel = "";

    /// <summary>La etiqueta solo aparece si hay más de un reproductor o si se fijó uno.</summary>
    [ObservableProperty]
    private bool _showSourceLabel;

    public NowPlayingViewModel(IMediaService media)
    {
        _media = media;
        _dispatcher = Dispatcher.CurrentDispatcher;
        _media.MediaChanged += OnMediaChanged;
        _media.SourcesChanged += OnSourcesChanged;

        _progressTimer = new DispatcherTimer
        {
            Interval = (TimeSpan)Application.Current.FindResource("Progress.Interval"),
        };
        _progressTimer.Tick += (_, _) => UpdateProgress();
    }

    public async Task StartAsync()
    {
        try
        {
            await _media.StartAsync();
        }
        catch (Exception)
        {
            // Sin acceso a los controles multimedia de Windows: el notch sigue funcionando sin música.
        }
    }

    /// <summary>La ventana avisa cuando el panel expandido se ve o deja de verse.</summary>
    public void SetVisible(bool isVisible)
    {
        _isVisible = isVisible;
        UpdateProgress();
        UpdateTimer();
    }

    [RelayCommand]
    private Task PlayPause() => _media.TogglePlayPauseAsync();

    [RelayCommand]
    private Task Next() => _media.NextAsync();

    [RelayCommand]
    private Task Previous() => _media.PreviousAsync();

    /// <summary>Cada clic pasa al siguiente: Automático → reproductor 1 → reproductor 2 → Automático.</summary>
    [RelayCommand]
    private Task NextSource()
    {
        IReadOnlyList<MediaSource> sources = _media.Sources;
        int index = -1;
        for (int i = 0; i < sources.Count; i++)
        {
            if (sources[i].Id == _media.PreferredSourceId)
                index = i;
        }

        string? next = index + 1 < sources.Count ? sources[index + 1].Id : null;
        return _media.SetPreferredSourceAsync(next);
    }

    private void OnMediaChanged(MediaInfo? info) => _dispatcher.BeginInvoke(() => Apply(info));

    private void OnSourcesChanged() => _dispatcher.BeginInvoke(UpdateSourceLabel);

    private void UpdateSourceLabel()
    {
        string? preferred = _media.PreferredSourceId;
        string showing = _info is null ? "" : SourceNames.Friendly(_info.SourceApp);

        SourceLabel = preferred is not null
            ? SourceNames.Friendly(preferred)
            : showing.Length > 0 ? $"Auto · {showing}" : "Auto";
        ShowSourceLabel = _media.Sources.Count > 1 || preferred is not null;
    }

    private void Apply(MediaInfo? info)
    {
        bool coverChanged = !ReferenceEquals(info?.Thumbnail, _info?.Thumbnail);
        _info = info;

        HasMedia = info is not null;
        Title = info?.Title ?? "";
        Artist = info?.Artist ?? "";
        ShortTitle = info is null ? "" : TextShortener.Shorten(info.Title, CollapsedTitleLength);
        IsPlaying = info?.IsPlaying == true;
        if (coverChanged)
            Cover = ToImage(info?.Thumbnail);

        UpdateSourceLabel();
        UpdateProgress();
        UpdateTimer();
    }

    private void UpdateProgress() =>
        Progress = _info is null ? 0 : PlaybackClock.Progress(_info, DateTimeOffset.Now);

    private void UpdateTimer()
    {
        if (_isVisible && IsPlaying)
            _progressTimer.Start();
        else
            _progressTimer.Stop();
    }

    private static ImageSource? ToImage(byte[]? bytes)
    {
        if (bytes is null || bytes.Length == 0)
            return null;

        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.DecodePixelWidth = 256;
            image.StreamSource = new MemoryStream(bytes);
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch (Exception)
        {
            // Carátula con formato raro: mejor sin carátula que sin notch.
            return null;
        }
    }

    public void Dispose()
    {
        _progressTimer.Stop();
        _media.MediaChanged -= OnMediaChanged;
        _media.SourcesChanged -= OnSourcesChanged;
    }
}
