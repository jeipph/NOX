using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nox.Core.Abstractions;
using Nox.Core.Audio;
using Nox.Core.Text;

namespace Nox.App.ViewModels;

/// <summary>Una app en el mezclador: su volumen (0–100) y silencio.</summary>
public partial class MixerRowViewModel : ObservableObject
{
    private readonly IAudioMixer _mixer;
    private bool _refreshing;

    public MixerRowViewModel(IAudioMixer mixer, MixerApp app)
    {
        _mixer = mixer;
        Key = app.Key;
        Update(app);
    }

    public string Key { get; }

    public IReadOnlyList<string> SessionIds { get; private set; } = [];

    [ObservableProperty]
    private string _name = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LevelText))]
    private double _level;

    [ObservableProperty]
    private bool _isMuted;

    public string LevelText => $"{Level:0}";

    [RelayCommand]
    private void ToggleMute()
    {
        IsMuted = !IsMuted;
        _mixer.SetMuted(SessionIds, IsMuted);
    }

    partial void OnLevelChanged(double value)
    {
        if (!_refreshing)
            _mixer.SetVolume(SessionIds, (float)(value / 100));
    }

    public void Update(MixerApp app)
    {
        _refreshing = true;
        SessionIds = app.SessionIds;
        Name = app.Name;
        Level = Math.Round(app.Volume * 100);
        IsMuted = app.IsMuted;
        _refreshing = false;
    }
}

/// <summary>
/// Panel "Mezclador": la salida de audio (un clic pasa a la siguiente) y el volumen de cada app.
/// Solo lee a Windows mientras se ve.
/// </summary>
public partial class MixerViewModel : ObservableObject, IDisposable
{
    private const int OutputNameLength = 34;

    private readonly IAudioMixer _mixer;
    private readonly Dispatcher _dispatcher;
    private bool _isVisible;

    public MixerViewModel(IAudioMixer mixer)
    {
        _mixer = mixer;
        _dispatcher = Dispatcher.CurrentDispatcher;
        _mixer.Changed += OnMixerChanged;
    }

    public ObservableCollection<MixerRowViewModel> Apps { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasApps))]
    private int _appCount;

    public bool HasApps => AppCount > 0;

    [ObservableProperty]
    private string _outputLabel = "";

    /// <summary>Hay más de una salida: el botón las va pasando.</summary>
    [ObservableProperty]
    private bool _canSwitchOutput;

    /// <summary>Aviso si Windows no dejó cambiar la salida; si no, "".</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStatus))]
    private string _status = "";

    public bool HasStatus => Status.Length > 0;

    public void SetVisible(bool isVisible)
    {
        _isVisible = isVisible;
        if (isVisible)
            Refresh();
    }

    [RelayCommand]
    private void NextOutput()
    {
        if (MixerApps.NextOutput(_mixer.Outputs()) is not { } next)
            return;
        Status = _mixer.SetDefaultOutput(next.Id) ? "" : "Windows no dejó cambiar la salida. Hazlo desde el icono de sonido de la barra de tareas.";
        Refresh();
    }

    private void OnMixerChanged() => _dispatcher.BeginInvoke(() =>
    {
        if (_isVisible)
            Refresh();
    });

    private void Refresh()
    {
        IReadOnlyList<AudioOutput> outputs = _mixer.Outputs();
        AudioOutput? current = outputs.FirstOrDefault(o => o.IsDefault);
        OutputLabel = current is null ? "Sin salida de audio" : "Salida: " + TextShortener.Shorten(current.Name, OutputNameLength);
        CanSwitchOutput = outputs.Count > 1;

        // Se reutilizan las filas que siguen (para no cortar un arrastre del deslizador).
        IReadOnlyList<MixerApp> apps = MixerApps.From(_mixer.Sessions());
        for (int i = Apps.Count - 1; i >= 0; i--)
        {
            if (apps.All(a => a.Key != Apps[i].Key))
                Apps.RemoveAt(i);
        }
        for (int i = 0; i < apps.Count; i++)
        {
            MixerRowViewModel? row = Apps.FirstOrDefault(r => r.Key == apps[i].Key);
            if (row is null)
                Apps.Insert(Math.Min(i, Apps.Count), new MixerRowViewModel(_mixer, apps[i]));
            else
                row.Update(apps[i]);
        }
        AppCount = Apps.Count;
    }

    public void Dispose() => _mixer.Changed -= OnMixerChanged;
}
