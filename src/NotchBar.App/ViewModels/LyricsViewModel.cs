using System.ComponentModel;
using System.Net;
using System.Net.Http;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NotchBar.Core.Abstractions;
using NotchBar.Core.Lyrics;
using NotchBar.Core.Media;
using NotchBar.Core.Models;

namespace NotchBar.App.ViewModels;

public partial class LyricsViewModel : ObservableObject, IDisposable
{
    private const string PauseMark = "♪";
    private static readonly TimeSpan OffsetStep = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan MinimumWait = TimeSpan.FromMilliseconds(50);
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(30);
    private const int MaxRetries = 3;

    private readonly IMediaService _media;
    private readonly ILyricsService _lyrics;
    private readonly NotchViewModel _options;
    private readonly Dispatcher _dispatcher;

    // Se programa para despertar justo cuando empieza la línea siguiente,
    // y solo con las letras a la vista y la música sonando.
    private readonly DispatcherTimer _lineTimer;

    // Solo se arma tras un fallo de conexión: un único intento más pasado un rato.
    private readonly DispatcherTimer _retryTimer;
    private int _retries;
    private CancellationTokenSource? _loading;
    private MediaInfo? _song;
    private string? _songKey;
    private LyricsResult? _result;
    private TimeSpan _offset;
    private bool _isVisible;

    [ObservableProperty]
    private string _previousLine = "";

    [ObservableProperty]
    private string _currentLine = "";

    [ObservableProperty]
    private string _nextLine = "";

    /// <summary>Mensaje cuando no hay letra sincronizada ("Buscando letra…", "Instrumental"...).</summary>
    [ObservableProperty]
    private string _status = "";

    [ObservableProperty]
    private bool _hasSyncedLyrics;

    [ObservableProperty]
    private string _offsetLabel = FormatOffset(TimeSpan.Zero);

    public LyricsViewModel(IMediaService media, ILyricsService lyrics, NotchViewModel options)
    {
        _media = media;
        _lyrics = lyrics;
        _options = options;
        _dispatcher = Dispatcher.CurrentDispatcher;

        _lineTimer = new DispatcherTimer();
        _lineTimer.Tick += (_, _) => UpdateLines();

        _retryTimer = new DispatcherTimer { Interval = RetryDelay };
        _retryTimer.Tick += (_, _) =>
        {
            _retryTimer.Stop();
            if (_song is not null)
                _ = LoadAsync(_song);
        };

        _media.MediaChanged += OnMediaChanged;
        _options.PropertyChanged += OnOptionsChanged;
    }

    /// <summary>La ventana avisa cuando el panel de letras se ve o deja de verse.</summary>
    public void SetVisible(bool isVisible)
    {
        _isVisible = isVisible;
        UpdateLines();
    }

    [RelayCommand]
    private void OffsetEarlier() => ChangeOffset(OffsetStep);

    [RelayCommand]
    private void OffsetLater() => ChangeOffset(-OffsetStep);

    private void ChangeOffset(TimeSpan change)
    {
        _offset = LyricsSyncEngine.ClampOffset(_offset + change);
        OffsetLabel = FormatOffset(_offset);
        UpdateLines();
    }

    // "+1,5 s" / "-0,5 s" / "0,0 s" (con la coma o el punto del idioma de Windows)
    private static string FormatOffset(TimeSpan offset) => $"{offset.TotalSeconds:+0.0;-0.0;0.0} s";

    private void OnMediaChanged(MediaInfo? song) => _dispatcher.BeginInvoke(() => OnSong(song));

    private void OnOptionsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(NotchViewModel.LyricsEnabled) && _song is not null)
            _ = LoadAsync(_song);
    }

    private void OnSong(MediaInfo? song)
    {
        _song = song;
        string? key = song is null ? null : LyricsKey.For(song);
        if (key != _songKey)
        {
            _songKey = key;
            _retryTimer.Stop();
            _retries = 0;
            _offset = TimeSpan.Zero;
            OffsetLabel = FormatOffset(_offset);
            if (song is null)
                Apply(null, nothingPlaying: true);
            else
                _ = LoadAsync(song);
        }

        UpdateLines();
    }

    private async Task LoadAsync(MediaInfo song)
    {
        _loading?.Cancel();
        var loading = _loading = new CancellationTokenSource();

        _result = null;
        HasSyncedLyrics = false;
        Status = "Buscando letra…";

        try
        {
            LyricsResult? result = await _lyrics.GetLyricsAsync(song, _options.LyricsEnabled, loading.Token);
            if (!loading.IsCancellationRequested)
                Apply(result);
        }
        catch (Exception error) when (!loading.IsCancellationRequested)
        {
            string problem = error is HttpRequestException { StatusCode: HttpStatusCode.ServiceUnavailable }
                ? "lrclib.net está saturado"
                : "No se pudo conectar con lrclib.net";

            // Reintento automático, unas pocas veces y solo mientras siga la misma canción.
            if (_retries < MaxRetries)
            {
                _retries++;
                Status = $"{problem}; reintento {_retries} de {MaxRetries} en {RetryDelay.TotalSeconds:0} s";
                _retryTimer.Start();
            }
            else
            {
                Status = problem;
            }
        }
        catch (OperationCanceledException)
        {
            // Cambió la canción mientras se buscaba.
        }
    }

    private void Apply(LyricsResult? result, bool nothingPlaying = false)
    {
        _result = result;
        HasSyncedLyrics = result?.Kind == LyricsKind.Synced;
        Status = nothingPlaying ? "Nada en reproducción"
            : result is null ? "Letras desactivadas (menú de la bandeja)"
            : result.Kind switch
            {
                LyricsKind.Synced => "",
                LyricsKind.Plain => "Sin letra sincronizada",
                LyricsKind.Instrumental => "Instrumental " + PauseMark,
                _ => "Sin letra para esta canción",
            };
        UpdateLines();
    }

    private void UpdateLines()
    {
        _lineTimer.Stop();
        if (_result is not { Kind: LyricsKind.Synced } result || _song is not { } song)
        {
            PreviousLine = CurrentLine = NextLine = "";
            return;
        }

        IReadOnlyList<LyricLine> lines = result.Lines;
        TimeSpan position = PlaybackClock.EstimatePosition(song, DateTimeOffset.Now);
        int index = LyricsSyncEngine.IndexAt(lines, position, _offset);

        PreviousLine = TextAt(lines, index - 1);
        CurrentLine = index < 0 ? PauseMark : TextAt(lines, index);
        NextLine = TextAt(lines, index + 1);

        if (_isVisible && song.IsPlaying && LyricsSyncEngine.TimeUntilNext(lines, position, _offset) is { } wait)
        {
            _lineTimer.Interval = wait > MinimumWait ? wait : MinimumWait;
            _lineTimer.Start();
        }
    }

    private static string TextAt(IReadOnlyList<LyricLine> lines, int index)
    {
        if (index < 0 || index >= lines.Count)
            return "";
        return lines[index].Text.Length > 0 ? lines[index].Text : PauseMark;
    }

    public void Dispose()
    {
        _lineTimer.Stop();
        _retryTimer.Stop();
        _loading?.Cancel();
        _media.MediaChanged -= OnMediaChanged;
        _options.PropertyChanged -= OnOptionsChanged;
    }
}
