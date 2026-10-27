using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nox.Core.Abstractions;

namespace Nox.App.ViewModels;

public partial class VolumeViewModel : ObservableObject, IDisposable
{
    private readonly IVolumeService _volume;
    private readonly Dispatcher _dispatcher;

    // True mientras se copia el valor de Windows al deslizador, para no devolvérselo a Windows.
    private bool _refreshing;

    /// <summary>De 0 a 100 (enteros, para que el deslizador no tiemble).</summary>
    [ObservableProperty]
    private double _level;

    [ObservableProperty]
    private bool _isMuted;

    public VolumeViewModel(IVolumeService volume)
    {
        _volume = volume;
        _dispatcher = Dispatcher.CurrentDispatcher;
        _volume.Changed += OnVolumeChanged;
        Refresh();
    }

    [RelayCommand]
    private void ToggleMute() => _volume.SetMuted(!IsMuted);

    partial void OnLevelChanged(double value)
    {
        if (!_refreshing)
            _volume.SetLevel((float)(value / 100));
    }

    private void OnVolumeChanged() => _dispatcher.BeginInvoke(Refresh);

    private void Refresh()
    {
        _refreshing = true;
        Level = Math.Round(_volume.Level * 100);
        IsMuted = _volume.IsMuted;
        _refreshing = false;
    }

    public void Dispose() => _volume.Changed -= OnVolumeChanged;
}
