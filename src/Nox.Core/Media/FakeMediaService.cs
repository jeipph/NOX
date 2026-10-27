using Nox.Core.Abstractions;
using Nox.Core.Models;

namespace Nox.Core.Media;

/// <summary>
/// Canción de mentira para probar la interfaz sin abrir un reproductor.
/// Se activa arrancando la app con <c>--fake</c>.
/// </summary>
public sealed class FakeMediaService : IMediaService
{
    private readonly TimeProvider _time;
    private int _track = 1;
    private MediaInfo _info;

    public FakeMediaService(TimeProvider? time = null)
    {
        _time = time ?? TimeProvider.System;
        _info = NewTrack(_track);
    }

    public event Action<MediaInfo?>? MediaChanged;

    public event Action? SourcesChanged;

    public MediaInfo? Current => _info;

    public IReadOnlyList<MediaSource> Sources { get; } = [new MediaSource("fake", "Prueba")];

    public string? PreferredSourceId { get; private set; }

    public Task StartAsync()
    {
        SourcesChanged?.Invoke();
        Publish(_info);
        return Task.CompletedTask;
    }

    public Task SetPreferredSourceAsync(string? sourceId)
    {
        PreferredSourceId = sourceId;
        SourcesChanged?.Invoke();
        return Task.CompletedTask;
    }

    public Task TogglePlayPauseAsync()
    {
        DateTimeOffset now = _time.GetUtcNow();
        Publish(_info with
        {
            Position = PlaybackClock.EstimatePosition(_info, now),
            PositionUpdatedAt = now,
            IsPlaying = !_info.IsPlaying,
        });
        return Task.CompletedTask;
    }

    public Task NextAsync()
    {
        Publish(NewTrack(++_track));
        return Task.CompletedTask;
    }

    public Task PreviousAsync()
    {
        _track = Math.Max(1, _track - 1);
        Publish(NewTrack(_track));
        return Task.CompletedTask;
    }

    private MediaInfo NewTrack(int number) => new(
        Title: $"Canción de prueba número {number} con un título largo",
        Artist: "Artista de prueba",
        Album: "Álbum de prueba",
        Duration: TimeSpan.FromSeconds(210),
        Position: TimeSpan.Zero,
        PositionUpdatedAt: _time.GetUtcNow(),
        IsPlaying: true,
        SourceApp: "fake",
        Thumbnail: null);

    private void Publish(MediaInfo info)
    {
        _info = info;
        MediaChanged?.Invoke(info);
    }
}
