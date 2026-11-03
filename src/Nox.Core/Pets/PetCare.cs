namespace Nox.Core.Pets;

/// <summary>
/// Lo que se guarda de los cuidados de la mascota (en <c>settings.json</c>), en hora UTC.
/// </summary>
/// <param name="LastFedUtc">Cuándo comió por última vez.</param>
/// <param name="PoopClockUtc">Desde cuándo se cuenta para el siguiente popó.</param>
/// <param name="Poops">Cuántos hay en el suelo (de 0 a <see cref="PetCare.MaxPoops"/>).</param>
public sealed record PetCareState(DateTime LastFedUtc, DateTime PoopClockUtc, int Poops);

/// <summary>
/// Hambre y popós de la mascota. Cada 4 h sin comer tiene hambre; cada 5 h hace un popó (como
/// mucho 3 en el suelo, hasta que se limpien). Si el PC estuvo apagado, se calcula al arrancar.
/// No usa relojes: quien lo usa le pasa la hora y programa un temporizador hasta <see cref="NextChange"/>.
/// </summary>
public sealed class PetCare
{
    public static readonly TimeSpan DefaultHungerAfter = TimeSpan.FromHours(4);
    public static readonly TimeSpan DefaultPoopEvery = TimeSpan.FromHours(5);
    public const int MaxPoops = 3;

    private DateTime _lastFed;
    private DateTime _poopClock;

    /// <param name="saved">Lo guardado, o null la primera vez (acaba de comer y está todo limpio).</param>
    /// <param name="hungerAfter">Para probar (--fake usa segundos en vez de horas).</param>
    public PetCare(PetCareState? saved, DateTime nowUtc, TimeSpan? hungerAfter = null, TimeSpan? poopEvery = null)
    {
        HungerAfter = hungerAfter ?? DefaultHungerAfter;
        PoopEvery = poopEvery ?? DefaultPoopEvery;
        _lastFed = saved?.LastFedUtc ?? nowUtc;
        _poopClock = saved?.PoopClockUtc ?? nowUtc;
        Poops = Math.Clamp(saved?.Poops ?? 0, 0, MaxPoops);
        // Un reloj que se fue al futuro (cambio de hora a mano) no debe dejarla sin hambre para siempre.
        if (_lastFed > nowUtc)
            _lastFed = nowUtc;
        Update(nowUtc);
    }

    public TimeSpan HungerAfter { get; }

    public TimeSpan PoopEvery { get; }

    public int Poops { get; private set; }

    public PetCareState State => new(_lastFed, _poopClock, Poops);

    public bool IsHungry(DateTime nowUtc) => nowUtc - _lastFed >= HungerAfter;

    /// <summary>
    /// Pone los popós al día. Devuelve true si cambió algo que hay que guardar.
    /// </summary>
    public bool Update(DateTime nowUtc)
    {
        if (_poopClock > nowUtc)
        {
            _poopClock = nowUtc; // el reloj fue hacia atrás: se cuenta desde ahora
            return true;
        }
        if (Poops >= MaxPoops)
        {
            // Lleno: no se acumulan más; tras limpiar se cuenta desde la limpieza.
            return false;
        }

        long due = (nowUtc - _poopClock).Ticks / PoopEvery.Ticks;
        if (due <= 0)
            return false;
        int added = (int)Math.Min(due, MaxPoops - Poops);
        Poops += added;
        _poopClock += TimeSpan.FromTicks(PoopEvery.Ticks * due);
        return true;
    }

    /// <summary>«Dar comida»: se le quita el hambre y vuelve a contar.</summary>
    public void Feed(DateTime nowUtc) => _lastFed = nowUtc;

    /// <summary>«Limpiar»: el suelo queda limpio y el siguiente popó es dentro de <see cref="PoopEvery"/>.</summary>
    public void Clean(DateTime nowUtc)
    {
        Poops = 0;
        _poopClock = nowUtc;
    }

    /// <summary>
    /// Cuándo cambiará algo por sí solo (le entra hambre o hace popó), o null si nada puede cambiar
    /// (tiene hambre y el suelo está lleno). Sirve para programar un único temporizador.
    /// </summary>
    public DateTime? NextChange(DateTime nowUtc)
    {
        DateTime? next = null;
        if (!IsHungry(nowUtc))
            next = _lastFed + HungerAfter;
        if (Poops < MaxPoops)
        {
            DateTime poop = _poopClock + PoopEvery;
            if (next is null || poop < next)
                next = poop;
        }
        return next;
    }
}
