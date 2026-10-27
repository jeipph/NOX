using Nox.Core.Models;

namespace Nox.Core.Media;

/// <summary>
/// Calcula por dónde va la canción. Los reproductores solo avisan de la posición de vez en cuando,
/// así que se suma el tiempo pasado desde el último aviso si está sonando.
/// </summary>
public static class PlaybackClock
{
    public static TimeSpan EstimatePosition(MediaInfo info, DateTimeOffset now)
    {
        TimeSpan position = info.Position;
        if (info.IsPlaying && now > info.PositionUpdatedAt)
            position += now - info.PositionUpdatedAt;

        if (position < TimeSpan.Zero)
            return TimeSpan.Zero;
        if (info.Duration > TimeSpan.Zero && position > info.Duration)
            return info.Duration;
        return position;
    }

    /// <summary>Progreso de 0 a 1 (0 si no se conoce la duración).</summary>
    public static double Progress(MediaInfo info, DateTimeOffset now) =>
        info.Duration > TimeSpan.Zero
            ? EstimatePosition(info, now) / info.Duration
            : 0;
}
