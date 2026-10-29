using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;
using Nox.App.ViewModels;
using Nox.App.Windows;
using Nox.Core.Pets;
using Nox.Windows.Shell;

namespace Nox.App.Services;

/// <summary>
/// Cada <c>BreakMinutes</c> de uso aparece la mascota para recordar descansar. Mira cada 30 s
/// (no gasta nada apreciable). Con una app en pantalla completa (un juego, una película) espera.
/// </summary>
public sealed class BreakService : IDisposable
{
    private static readonly TimeSpan SnoozeDelay = TimeSpan.FromMinutes(5);

    private readonly NotchViewModel _options;
    private readonly DispatcherTimer _timer = new();
    private readonly BreakReminder _reminder;
    private readonly bool _fast;
    private Func<bool> _canShow = () => true;
    private BreakWindow? _window;

    /// <param name="fast">Modo de prueba (--fake): los "minutos" son segundos.</param>
    public BreakService(NotchViewModel options, bool fast = false)
    {
        _options = options;
        _fast = fast;
        _reminder = new BreakReminder(Interval, DateTime.Now);
        _timer.Tick += (_, _) => Check();
        _options.PropertyChanged += OnOptionsChanged;
        _options.BreakNowRequested += Show;
    }

    private TimeSpan Interval => _fast ? TimeSpan.FromSeconds(_options.BreakMinutes) : TimeSpan.FromMinutes(_options.BreakMinutes);

    /// <param name="canShow">False mientras hay una app en pantalla completa.</param>
    public void Start(Func<bool> canShow)
    {
        _canShow = canShow;
        _reminder.Restart(Interval, DateTime.Now); // las opciones ya están cargadas
        _timer.Interval = _fast ? TimeSpan.FromSeconds(1) : (TimeSpan)Application.Current.FindResource("Break.CheckInterval");
        _timer.Start();
    }

    private void OnOptionsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(NotchViewModel.BreakMinutes))
            _reminder.Restart(Interval, DateTime.Now);
    }

    private void Check()
    {
        DateTime now = DateTime.Now;
        TimeSpan idle = IdleTime.Get();
        if (_window is not null)
        {
            // Se fue a descansar con el aviso abierto: ya no hace falta, y la cuenta empieza de nuevo.
            if (idle >= BreakReminder.RestCounts)
            {
                _reminder.Dismiss(now);
                _window.CloseQuietly();
            }
            return;
        }
        if (_reminder.Check(now, idle))
        {
            if (_canShow())
                Show();
            else
                _reminder.Snooze(now, TimeSpan.FromMinutes(1)); // en pantalla completa: se vuelve a mirar en 1 min
        }
    }

    private void Show()
    {
        if (_window is not null)
            return;
        int minutes = _options.BreakMinutes > 0 ? _options.BreakMinutes : 40;
        _window = new BreakWindow(_options.Pet, _options.PetToy, _fast ? $"{minutes} s" : $"{minutes} min");
        _window.Chosen += choice =>
        {
            if (choice == BreakChoice.Snooze)
                _reminder.Snooze(DateTime.Now, _fast ? TimeSpan.FromSeconds(5) : SnoozeDelay);
            else
                _reminder.Dismiss(DateTime.Now);
        };
        _window.Closed += (_, _) => _window = null;
        _window.Show();
    }

    public void Dispose()
    {
        _timer.Stop();
        _options.PropertyChanged -= OnOptionsChanged;
        _options.BreakNowRequested -= Show;
        _window?.CloseQuietly();
    }
}
