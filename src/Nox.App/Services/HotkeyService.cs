using System.ComponentModel;
using Nox.App.ViewModels;
using Nox.Core.Hotkeys;
using Nox.Windows.Shell;

namespace Nox.App.Services;

/// <summary>
/// Une los atajos guardados con Windows y con lo que hace cada uno. Se vuelven a registrar cada vez
/// que cambian, y se apartan mientras se escribe un atajo nuevo (si no, Windows se lo quedaría).
/// </summary>
public sealed class HotkeyService : IDisposable
{
    private readonly GlobalHotkeys _hotkeys;
    private readonly NotchViewModel _options;
    private readonly NowPlayingViewModel _nowPlaying;
    private readonly LyricsViewModel _lyrics;
    private bool _started;
    private bool _suspended;

    public HotkeyService(GlobalHotkeys hotkeys, NotchViewModel options, NowPlayingViewModel nowPlaying, LyricsViewModel lyrics)
    {
        _hotkeys = hotkeys;
        _options = options;
        _nowPlaying = nowPlaying;
        _lyrics = lyrics;
        _hotkeys.Pressed += OnPressed;
        _options.PropertyChanged += OnOptionsChanged;
    }

    /// <summary>Se pulsó el atajo de abrir/cerrar (la ventana del notch lo atiende).</summary>
    public event Action? ToggleNotchRequested;

    /// <summary>Cambió el estado de algún atajo (para la ventana de atajos).</summary>
    public event Action? StatusChanged;

    /// <summary>Por atajo: "" si funciona; si no, el motivo.</summary>
    public IReadOnlyDictionary<HotkeyAction, string> Problems { get; private set; } = new Dictionary<HotkeyAction, string>();

    /// <summary>Cuando la ventana del notch ya existe (Windows necesita una ventana a la que avisar).</summary>
    public void Start()
    {
        _started = true;
        Apply();
    }

    /// <summary>Mientras se escribe un atajo nuevo: ninguno está activo.</summary>
    public void Suspend()
    {
        _suspended = true;
        _hotkeys.UnregisterAll();
    }

    public void Resume()
    {
        _suspended = false;
        Apply();
    }

    private void OnOptionsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(NotchViewModel.Hotkeys))
            Apply();
    }

    private void Apply()
    {
        if (!_started || _suspended)
            return;
        HotkeySettings settings = _options.Hotkeys;
        var problems = new Dictionary<HotkeyAction, string>();
        if (settings.Enabled)
        {
            var (usable, invalid) = settings.Check();
            foreach (var (action, reason) in invalid)
                problems[action] = reason;
            foreach (HotkeyAction action in _hotkeys.Register(usable))
                problems[action] = "Ocupado por otra app";
        }
        else
        {
            _hotkeys.UnregisterAll();
        }
        Problems = problems;
        _options.HotkeyProblemCount = problems.Count;
        StatusChanged?.Invoke();
    }

    private void OnPressed(HotkeyAction action)
    {
        switch (action)
        {
            case HotkeyAction.ToggleNotch:
                ToggleNotchRequested?.Invoke();
                break;
            case HotkeyAction.PlayPause:
                _nowPlaying.PlayPauseCommand.Execute(null);
                break;
            case HotkeyAction.LyricsEarlier:
                _lyrics.OffsetEarlierCommand.Execute(null);
                break;
            case HotkeyAction.LyricsLater:
                _lyrics.OffsetLaterCommand.Execute(null);
                break;
            case HotkeyAction.LyricsReset:
                _lyrics.ResetOffsetCommand.Execute(null);
                break;
        }
    }

    public void Dispose()
    {
        _hotkeys.Pressed -= OnPressed;
        _options.PropertyChanged -= OnOptionsChanged;
        _hotkeys.Dispose();
    }
}
