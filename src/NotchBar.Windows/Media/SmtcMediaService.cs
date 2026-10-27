using NotchBar.Core.Abstractions;
using NotchBar.Core.Media;
using NotchBar.Core.Models;
using Windows.Media.Control;
using Windows.Storage.Streams;
using PlaybackStatus = Windows.Media.Control.GlobalSystemMediaTransportControlsSessionPlaybackStatus;
using Session = Windows.Media.Control.GlobalSystemMediaTransportControlsSession;
using SessionManager = Windows.Media.Control.GlobalSystemMediaTransportControlsSessionManager;

namespace NotchBar.Windows.Media;

/// <summary>
/// Lee la música desde los controles multimedia de Windows (SMTC): navegador con YouTube Music,
/// MusicBee, Spotify... Todo por eventos; no hay temporizadores.
/// De todos los reproductores abiertos solo se escucha play/pausa (para saber cuál suena);
/// los avisos completos (canción, carátula, posición) solo los del reproductor elegido.
/// </summary>
public sealed class SmtcMediaService : IMediaService, IDisposable
{
    private static readonly TimeSpan EmptyGracePeriod = TimeSpan.FromSeconds(1.5);

    // Los eventos de Windows llegan en hilos distintos: _sync protege la lista de reproductores
    // y la elección; _gate evita leer la canción dos veces a la vez.
    private readonly object _sync = new();
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly List<Session> _watched = [];
    private CancellationTokenSource? _pendingEmpty;
    private SessionManager? _manager;
    private Session? _session;

    public event Action<MediaInfo?>? MediaChanged;

    public event Action? SourcesChanged;

    public MediaInfo? Current { get; private set; }

    public IReadOnlyList<MediaSource> Sources { get; private set; } = [];

    public string? PreferredSourceId { get; private set; }

    public async Task StartAsync()
    {
        _manager = await SessionManager.RequestAsync();
        _manager.SessionsChanged += OnSessionsChanged;
        _manager.CurrentSessionChanged += OnCurrentSessionChanged;
        await UpdateSessionsAsync();
    }

    public Task SetPreferredSourceAsync(string? sourceId)
    {
        PreferredSourceId = sourceId;
        SourcesChanged?.Invoke();
        return SelectAsync();
    }

    public Task TogglePlayPauseAsync() => ControlAsync(s => s.TryTogglePlayPauseAsync().AsTask());

    public Task NextAsync() => ControlAsync(s => s.TrySkipNextAsync().AsTask());

    public Task PreviousAsync() => ControlAsync(s => s.TrySkipPreviousAsync().AsTask());

    private async Task ControlAsync(Func<Session, Task<bool>> action)
    {
        if (_session is not { } session)
            return;

        try
        {
            await action(session);
        }
        catch (Exception)
        {
            // El reproductor se cerró justo al pulsar; no hay nada que hacer.
        }
    }

    // --- Reproductores abiertos y elección ---

    private void OnSessionsChanged(SessionManager sender, SessionsChangedEventArgs args) => _ = UpdateSessionsAsync();

    // Probado el 2026-10-27: al abrir un reproductor nuevo, Windows no siempre manda SessionsChanged,
    // pero sí CurrentSessionChanged; por eso aquí también se rehace la lista.
    private void OnCurrentSessionChanged(SessionManager sender, CurrentSessionChangedEventArgs args) => _ = UpdateSessionsAsync();

    private void OnAnyPlaybackInfoChanged(Session sender, PlaybackInfoChangedEventArgs args) => _ = SelectAsync();

    private async Task UpdateSessionsAsync()
    {
        IReadOnlyList<Session> sessions;
        try
        {
            sessions = _manager?.GetSessions() ?? [];
        }
        catch (Exception)
        {
            return;
        }

        lock (_sync)
        {
            foreach (Session old in _watched)
                Unwatch(old);
            _watched.Clear();

            foreach (Session session in sessions)
            {
                session.PlaybackInfoChanged += OnAnyPlaybackInfoChanged;
                _watched.Add(session);
            }

            Sources = sessions
                .Select(s => s.SourceAppUserModelId)
                .Distinct()
                .Select(id => new MediaSource(id, SourceNames.Friendly(id)))
                .ToList();
        }

        SourcesChanged?.Invoke();
        await SelectAsync();
    }

    private async Task SelectAsync()
    {
        bool switched;
        lock (_sync)
        {
            var states = _watched.Select(s => new SourceState(s.SourceAppUserModelId, IsPlaying(s))).ToList();
            string? currentId = TryGetCurrentId();
            int current = currentId is null ? -1 : _watched.FindIndex(s => s.SourceAppUserModelId == currentId);

            int picked = SourcePicker.Pick(states, PreferredSourceId, current);
            Session? target = picked >= 0 ? _watched[picked] : null;

            switched = !ReferenceEquals(target, _session);
            if (switched)
            {
                DetachSelected();
                _session = target;
                AttachSelected();
            }
        }

        await RefreshAsync(readThumbnail: switched);
    }

    private string? TryGetCurrentId()
    {
        try
        {
            return _manager?.GetCurrentSession()?.SourceAppUserModelId;
        }
        catch (Exception)
        {
            return null;
        }
    }

    // Un reproductor recién cerrado puede dar error al quitarle el aviso; no importa.
    private void Unwatch(Session session)
    {
        try
        {
            session.PlaybackInfoChanged -= OnAnyPlaybackInfoChanged;
        }
        catch (Exception)
        {
        }
    }

    private static bool IsPlaying(Session session)
    {
        try
        {
            return session.GetPlaybackInfo().PlaybackStatus == PlaybackStatus.Playing;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private void AttachSelected()
    {
        if (_session is null)
            return;

        _session.MediaPropertiesChanged += OnMediaPropertiesChanged;
        _session.TimelinePropertiesChanged += OnTimelinePropertiesChanged;
    }

    private void DetachSelected()
    {
        if (_session is null)
            return;

        _session.MediaPropertiesChanged -= OnMediaPropertiesChanged;
        _session.TimelinePropertiesChanged -= OnTimelinePropertiesChanged;
        _session = null;
    }

    // Solo el aviso de "propiedades" trae carátula nueva; pausa y posición no la cambian.
    private void OnMediaPropertiesChanged(Session sender, MediaPropertiesChangedEventArgs args) =>
        _ = RefreshAsync(readThumbnail: true);

    private void OnTimelinePropertiesChanged(Session sender, TimelinePropertiesChangedEventArgs args) => _ = RefreshAsync();

    // --- Lectura de la canción ---

    private async Task RefreshAsync(bool readThumbnail = false)
    {
        await _gate.WaitAsync();
        try
        {
            MediaInfo? info = _session is { } session ? await ReadAsync(session, readThumbnail) : null;
            if (info is not null)
                Publish(info);
            else
                PublishEmptyAfterGrace();
        }
        catch (Exception)
        {
            // La sesión desapareció mientras la leíamos (se cerró la pestaña o el reproductor).
            PublishEmptyAfterGrace();
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<MediaInfo?> ReadAsync(Session session, bool readThumbnail)
    {
        var properties = await session.TryGetMediaPropertiesAsync();
        if (properties is null || string.IsNullOrWhiteSpace(properties.Title))
            return null;

        var playback = session.GetPlaybackInfo();
        var timeline = session.GetTimelineProperties();

        // Chrome manda primero su logo como carátula y después la portada real (en otro aviso
        // de propiedades), así que la carátula se relee en cada aviso de propiedades o al cambiar de canción.
        MediaInfo? previous = Current;
        bool sameSong = previous is not null && previous.Title == properties.Title && previous.Artist == properties.Artist;
        byte[]? thumbnail = previous?.Thumbnail;
        if (readThumbnail || !sameSong || thumbnail is null)
        {
            byte[]? fresh = await ReadThumbnailAsync(properties.Thumbnail);
            // Si son los mismos bytes se conserva el arreglo anterior y la interfaz no redibuja la imagen.
            if (fresh is null || thumbnail is null || !fresh.AsSpan().SequenceEqual(thumbnail))
                thumbnail = fresh;
        }

        return new MediaInfo(
            Title: properties.Title,
            Artist: properties.Artist ?? "",
            Album: properties.AlbumTitle ?? "",
            Duration: timeline.EndTime - timeline.StartTime,
            Position: timeline.Position,
            PositionUpdatedAt: timeline.LastUpdatedTime,
            IsPlaying: playback.PlaybackStatus == PlaybackStatus.Playing,
            SourceApp: session.SourceAppUserModelId,
            Thumbnail: thumbnail);
    }

    private static async Task<byte[]?> ReadThumbnailAsync(IRandomAccessStreamReference? reference)
    {
        if (reference is null)
            return null;

        using IRandomAccessStreamWithContentType stream = await reference.OpenReadAsync();
        using Stream input = stream.AsStreamForRead();
        using var memory = new MemoryStream();
        await input.CopyToAsync(memory);
        return memory.ToArray();
    }

    // --- Publicar ---

    // Al cambiar de canción, Chrome cierra su sesión y abre otra casi enseguida. Para no mostrar
    // "Nada en reproducción" durante ese instante, el vacío solo se publica si dura un rato.
    private void PublishEmptyAfterGrace()
    {
        if (Current is null || _pendingEmpty is not null)
            return;

        _pendingEmpty = new CancellationTokenSource();
        _ = PublishEmptyLaterAsync(_pendingEmpty.Token);
    }

    private async Task PublishEmptyLaterAsync(CancellationToken token)
    {
        try
        {
            await Task.Delay(EmptyGracePeriod, token);
            await _gate.WaitAsync(token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        try
        {
            if (!token.IsCancellationRequested)
                Publish(null);
        }
        finally
        {
            _gate.Release();
        }
    }

    private void CancelPendingEmpty()
    {
        _pendingEmpty?.Cancel();
        _pendingEmpty?.Dispose();
        _pendingEmpty = null;
    }

    private void Publish(MediaInfo? info)
    {
        CancelPendingEmpty();
        if (info == Current)
            return;

        Current = info;
        MediaChanged?.Invoke(info);
    }

    public void Dispose()
    {
        if (_manager is not null)
        {
            _manager.SessionsChanged -= OnSessionsChanged;
            _manager.CurrentSessionChanged -= OnCurrentSessionChanged;
        }
        _manager = null;

        lock (_sync)
        {
            DetachSelected();
            foreach (Session session in _watched)
                Unwatch(session);
            _watched.Clear();
        }

        CancelPendingEmpty();
        _gate.Dispose();
    }
}
