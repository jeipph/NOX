using System.Media;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nox.Core.Focus;

namespace Nox.App.ViewModels;

/// <summary>Un botón de duración ("30 min", "1 h").</summary>
public sealed record FocusPreset(TimeSpan Duration, string Label);

public partial class FocusViewModel : ObservableObject, IDisposable
{
    private readonly FocusTimer _timer = new();

    // Solo corre mientras el temporizador está en marcha: un tic por segundo.
    private readonly DispatcherTimer _tick = new() { Interval = TimeSpan.FromSeconds(1) };

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdle), nameof(IsActive), nameof(IsPaused), nameof(IsFinished))]
    private FocusState _state = FocusState.Idle;

    [ObservableProperty]
    private string _remainingText = "";

    [ObservableProperty]
    private double _progress;

    /// <summary>"de 30 min", para recordar qué se eligió.</summary>
    [ObservableProperty]
    private string _durationText = "";

    /// <param name="includeTestPreset">Solo en el modo <c>--fake</c>: añade "10 s" para probar el aviso sin esperar.</param>
    public FocusViewModel(bool includeTestPreset = false)
    {
        _tick.Tick += (_, _) => Update();

        var presets = new[] { 15, 25, 30, 45, 60, 90 }
            .Select(minutes => TimeSpan.FromMinutes(minutes))
            .Select(duration => new FocusPreset(duration, FocusTimer.Describe(duration)))
            .ToList();
        if (includeTestPreset)
            presets.Insert(0, new FocusPreset(TimeSpan.FromSeconds(10), "10 s"));
        Presets = presets;
    }

    /// <summary>Se acabó el tiempo (la ventana abre el notch un momento para avisar).</summary>
    public event Action? Finished;

    public IReadOnlyList<FocusPreset> Presets { get; }

    public bool IsIdle => State == FocusState.Idle;

    /// <summary>Corriendo o en pausa: hay una sesión en curso.</summary>
    public bool IsActive => State is FocusState.Running or FocusState.Paused;

    public bool IsPaused => State == FocusState.Paused;

    public bool IsFinished => State == FocusState.Finished;

    [RelayCommand]
    private void Start(FocusPreset preset)
    {
        _timer.Start(preset.Duration);
        DurationText = $"de {preset.Label}";
        Update();
    }

    [RelayCommand]
    private void PauseOrResume()
    {
        if (_timer.State == FocusState.Running)
            _timer.Pause();
        else
            _timer.Resume();
        Update();
    }

    /// <summary>Cancelar una sesión o aceptar el aviso de "tiempo cumplido".</summary>
    [RelayCommand]
    private void Reset()
    {
        _timer.Reset();
        Update();
    }

    private void Update()
    {
        bool justFinished = _timer.CheckFinished();

        State = _timer.State;
        RemainingText = FocusTimer.Format(_timer.Remaining);
        Progress = _timer.Progress;

        if (_timer.State == FocusState.Running)
            _tick.Start();
        else
            _tick.Stop();

        if (justFinished)
        {
            SystemSounds.Asterisk.Play();
            Finished?.Invoke();
        }
    }

    public void Dispose() => _tick.Stop();
}
