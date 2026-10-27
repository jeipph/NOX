namespace NotchBar.Core.Focus;

public enum FocusState
{
    Idle,
    Running,
    Paused,
    Finished,
}

/// <summary>
/// Temporizador de concentración. No tiene temporizador propio: guarda la hora de fin y
/// calcula lo que falta cuando se le pregunta. Así no se desajusta y se puede probar sin esperar.
/// </summary>
public sealed class FocusTimer(TimeProvider? time = null)
{
    private readonly TimeProvider _time = time ?? TimeProvider.System;
    private DateTimeOffset _endsAt;
    private TimeSpan _remainingWhenPaused;

    public FocusState State { get; private set; } = FocusState.Idle;

    /// <summary>Duración elegida al empezar.</summary>
    public TimeSpan Duration { get; private set; }

    public TimeSpan Remaining => State switch
    {
        FocusState.Running => Max(TimeSpan.Zero, _endsAt - _time.GetUtcNow()),
        FocusState.Paused => _remainingWhenPaused,
        _ => TimeSpan.Zero,
    };

    /// <summary>De 0 (recién empezado) a 1 (terminado).</summary>
    public double Progress => State switch
    {
        FocusState.Finished => 1,
        FocusState.Idle => 0,
        _ => Duration > TimeSpan.Zero ? 1 - Remaining / Duration : 0,
    };

    public void Start(TimeSpan duration)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(duration, TimeSpan.Zero);
        Duration = duration;
        _endsAt = _time.GetUtcNow() + duration;
        State = FocusState.Running;
    }

    public void Pause()
    {
        if (State != FocusState.Running)
            return;
        _remainingWhenPaused = Remaining;
        State = FocusState.Paused;
    }

    public void Resume()
    {
        if (State != FocusState.Paused)
            return;
        _endsAt = _time.GetUtcNow() + _remainingWhenPaused;
        State = FocusState.Running;
    }

    /// <summary>Cancelar mientras corre, o aceptar el aviso cuando terminó: vuelve al principio.</summary>
    public void Reset() => State = FocusState.Idle;

    /// <summary>Pasa a <see cref="FocusState.Finished"/> si se acabó el tiempo. Devuelve true solo en ese momento.</summary>
    public bool CheckFinished()
    {
        if (State != FocusState.Running || _time.GetUtcNow() < _endsAt)
            return false;
        State = FocusState.Finished;
        return true;
    }

    /// <summary>"4:05", "29:59" o "1:00:00".</summary>
    public static string Format(TimeSpan time)
    {
        // Se redondea hacia arriba: con 0,4 s restantes se ve "0:01", no "0:00".
        var seconds = (long)Math.Ceiling(time.TotalSeconds);
        var rounded = TimeSpan.FromSeconds(Math.Max(0, seconds));
        return rounded.TotalHours >= 1
            ? $"{(int)rounded.TotalHours}:{rounded.Minutes:00}:{rounded.Seconds:00}"
            : $"{rounded.Minutes}:{rounded.Seconds:00}";
    }

    /// <summary>"15 min", "1 h", "1 h 30 min".</summary>
    public static string Describe(TimeSpan duration)
    {
        int hours = (int)duration.TotalHours;
        int minutes = duration.Minutes;
        return (hours, minutes) switch
        {
            (0, _) => $"{minutes} min",
            (_, 0) => $"{hours} h",
            _ => $"{hours} h {minutes} min",
        };
    }

    private static TimeSpan Max(TimeSpan a, TimeSpan b) => a > b ? a : b;
}
