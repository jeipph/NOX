namespace Nox.Core.Pets;

/// <summary>
/// Cuándo toca descansar: tras <see cref="Interval"/> usando el PC sin parar. Si dejas el PC
/// (sin tocar teclado ni ratón) al menos <see cref="RestCounts"/>, ya descansaste y se empieza de nuevo.
/// No usa relojes propios: quien lo usa le pasa la hora y cuánto lleva el PC sin tocarse.
/// </summary>
public sealed class BreakReminder(TimeSpan interval, DateTime now)
{
    /// <summary>Una pausa de este tamaño (o más) cuenta como descanso.</summary>
    public static readonly TimeSpan RestCounts = TimeSpan.FromMinutes(5);

    private DateTime _workingSince = now;

    /// <summary>Cada cuánto avisar. Cero o negativo = nunca.</summary>
    public TimeSpan Interval { get; set; } = interval;

    /// <summary>Se está mostrando el aviso (hasta que se cierre o se posponga).</summary>
    public bool IsShowing { get; private set; }

    /// <summary>
    /// Llamar cada poco (p. ej. cada 30 s).
    /// </summary>
    /// <param name="idle">Cuánto lleva el PC sin teclado ni ratón.</param>
    /// <returns>True una sola vez, cuando hay que mostrar el aviso.</returns>
    public bool Check(DateTime now, TimeSpan idle)
    {
        if (idle >= RestCounts)
        {
            // Ya está descansando (o se fue): el aviso sobra y la cuenta vuelve a empezar al volver.
            _workingSince = now;
            IsShowing = false;
            return false;
        }
        if (IsShowing || Interval <= TimeSpan.Zero || now - _workingSince < Interval)
            return false;
        IsShowing = true;
        return true;
    }

    /// <summary>Cuánto falta para el próximo aviso (para mostrarlo en el menú).</summary>
    public TimeSpan Remaining(DateTime now) =>
        Interval <= TimeSpan.Zero ? TimeSpan.Zero : Max(TimeSpan.Zero, Interval - (now - _workingSince));

    /// <summary>"Listo, descansé": la cuenta empieza de nuevo.</summary>
    public void Dismiss(DateTime now)
    {
        IsShowing = false;
        _workingSince = now;
    }

    /// <summary>"Más tarde": vuelve a avisar dentro de <paramref name="delay"/>.</summary>
    public void Snooze(DateTime now, TimeSpan delay)
    {
        IsShowing = false;
        _workingSince = now - Interval + delay;
    }

    /// <summary>Cambió el intervalo en las opciones: se cuenta desde ahora.</summary>
    public void Restart(TimeSpan interval, DateTime now)
    {
        Interval = interval;
        IsShowing = false;
        _workingSince = now;
    }

    private static TimeSpan Max(TimeSpan a, TimeSpan b) => a > b ? a : b;
}
