using NotchBar.Core.Abstractions;
using NotchBar.Core.Models;
using Windows.Media.Control;
using Windows.Storage.Streams;
using Session = Windows.Media.Control.GlobalSystemMediaTransportControlsSession;
using SessionManager = Windows.Media.Control.GlobalSystemMediaTransportControlsSessionManager;

namespace NotchBar.Windows.Media;

/// <summary>
/// Lee la música desde los controles multimedia de Windows (SMTC): navegador con YouTube Music,
/// MusicBee, Spotify... Todo por eventos; no hay temporizadores.
/// </summary>
public sealed class SmtcMediaService : IMediaService, IDisposable
{
    // Los eventos de Windows llegan en hilos distintos; esto evita leer dos veces a la vez.
    private readonly SemaphoreSlim _gate = new(1, 1);
    private SessionManager? _manager;
    private Session? _session;

    public event Action<MediaInfo?>? MediaChanged;

    public MediaInfo? Current { get; private set; }

    public async Task StartAsync()
    {
        _manager = await SessionManager.RequestAsync();
        _manager.CurrentSessionChanged += OnCurrentSessionChanged;
        await SwitchSessionAsync();
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

    private void OnCurrentSessionChanged(SessionManager sender, CurrentSessionChangedEventArgs args) =>
        _ = SwitchSessionAsync();

    private async Task SwitchSessionAsync()
    {
        Session? session = _manager?.GetCurrentSession();
        DetachSession();

        _session = session;
        if (session is not null)
        {
            session.MediaPropertiesChanged += OnMediaPropertiesChanged;
            session.PlaybackInfoChanged += OnPlaybackInfoChanged;
            session.TimelinePropertiesChanged += OnTimelinePropertiesChanged;
        }

        await RefreshAsync();
    }

    private void DetachSession()
    {
        if (_session is null)
            return;

        _session.MediaPropertiesChanged -= OnMediaPropertiesChanged;
        _session.PlaybackInfoChanged -= OnPlaybackInfoChanged;
        _session.TimelinePropertiesChanged -= OnTimelinePropertiesChanged;
        _session = null;
    }

    private void OnMediaPropertiesChanged(Session sender, MediaPropertiesChangedEventArgs args) => _ = RefreshAsync();

    private void OnPlaybackInfoChanged(Session sender, PlaybackInfoChangedEventArgs args) => _ = RefreshAsync();

    private void OnTimelinePropertiesChanged(Session sender, TimelinePropertiesChangedEventArgs args) => _ = RefreshAsync();

    private async Task RefreshAsync()
    {
        await _gate.WaitAsync();
        try
        {
            Publish(_session is { } session ? await ReadAsync(session) : null);
        }
        catch (Exception)
        {
            // La sesión desapareció mientras la leíamos (se cerró la pestaña o el reproductor).
            Publish(null);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<MediaInfo?> ReadAsync(Session session)
    {
        var properties = await session.TryGetMediaPropertiesAsync();
        if (properties is null || string.IsNullOrWhiteSpace(properties.Title))
            return null;

        var playback = session.GetPlaybackInfo();
        var timeline = session.GetTimelineProperties();

        // La carátula solo se vuelve a leer si cambia la canción (o si aún no había llegado).
        MediaInfo? previous = Current;
        bool sameSong = previous is not null && previous.Title == properties.Title && previous.Artist == properties.Artist;
        byte[]? thumbnail = sameSong && previous!.Thumbnail is not null
            ? previous.Thumbnail
            : await ReadThumbnailAsync(properties.Thumbnail);

        return new MediaInfo(
            Title: properties.Title,
            Artist: properties.Artist ?? "",
            Album: properties.AlbumTitle ?? "",
            Duration: timeline.EndTime - timeline.StartTime,
            Position: timeline.Position,
            PositionUpdatedAt: timeline.LastUpdatedTime,
            IsPlaying: playback.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing,
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

    private void Publish(MediaInfo? info)
    {
        if (info == Current)
            return;

        Current = info;
        MediaChanged?.Invoke(info);
    }

    public void Dispose()
    {
        if (_manager is not null)
            _manager.CurrentSessionChanged -= OnCurrentSessionChanged;
        _manager = null;
        DetachSession();
        _gate.Dispose();
    }
}
