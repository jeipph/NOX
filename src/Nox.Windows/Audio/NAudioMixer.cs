using System.Diagnostics;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using Nox.Core.Abstractions;

namespace Nox.Windows.Audio;

/// <summary>
/// Mezclador con la API de audio de Windows (NAudio): salidas disponibles, salida predeterminada
/// y volumen de cada app. Sin sondeo: Windows avisa cuando aparece una app con sonido o cambian
/// las salidas; el resto se lee al abrir el mezclador.
/// </summary>
public sealed class NAudioMixer : IAudioMixer, IMMNotificationClient, IDisposable
{
    private readonly MMDeviceEnumerator _enumerator = new();
    private readonly Lock _lock = new();
    private readonly Dictionary<int, string> _processNames = [];
    private MMDevice? _device;

    public event Action? Changed;

    public NAudioMixer()
    {
        _enumerator.RegisterEndpointNotificationCallback(this);
        BindDefaultDevice();
    }

    public IReadOnlyList<AudioOutput> Outputs()
    {
        // La predeterminada se pregunta a Windows cada vez: el dispositivo guardado puede haberse
        // vuelto a crear en otro hilo (tras un cambio de salida) y leerlo desde aquí falla.
        string? current = null;
        try
        {
            using MMDevice defaultDevice = _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            current = defaultDevice.ID;
        }
        catch (Exception)
        {
            // Sin salida predeterminada (ningún altavoz conectado).
        }

        var outputs = new List<AudioOutput>();
        try
        {
            foreach (MMDevice device in _enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
            {
                try
                {
                    outputs.Add(new AudioOutput(device.ID, device.FriendlyName, device.ID == current));
                }
                catch (Exception)
                {
                    // Se desconectó mientras se leía: se salta.
                }
                finally
                {
                    device.Dispose();
                }
            }
        }
        catch (Exception)
        {
        }
        return [.. outputs.OrderBy(o => o.Name, StringComparer.CurrentCultureIgnoreCase)];
    }

    public bool SetDefaultOutput(string id) => PolicyConfig.SetDefaultOutput(id);

    public IReadOnlyList<AudioSessionInfo> Sessions()
    {
        lock (_lock)
        {
            var sessions = new List<AudioSessionInfo>();
            try
            {
                if (_device is null)
                    return sessions;
                AudioSessionManager manager = _device.AudioSessionManager;
                manager.RefreshSessions();
                SessionCollection all = manager.Sessions;
                for (int i = 0; i < all.Count; i++)
                {
                    AudioSessionControl session = all[i];
                    if (session.State == NAudio.CoreAudioApi.Interfaces.AudioSessionState.AudioSessionStateExpired)
                        continue; // la app ya se cerró
                    int pid = (int)session.GetProcessID;
                    sessions.Add(new AudioSessionInfo(
                        session.GetSessionInstanceIdentifier,
                        pid,
                        session.IsSystemSoundsSession ? "" : ProcessName(pid),
                        session.DisplayName ?? "",
                        session.SimpleAudioVolume.Volume,
                        session.SimpleAudioVolume.Mute,
                        session.IsSystemSoundsSession));
                }
            }
            catch (Exception)
            {
                // El dispositivo desapareció mientras se leía: se devuelve lo que haya.
            }
            return sessions;
        }
    }

    public void SetVolume(IEnumerable<string> sessionIds, float volume) =>
        ForEach(sessionIds, s => s.SimpleAudioVolume.Volume = Math.Clamp(volume, 0f, 1f));

    public void SetMuted(IEnumerable<string> sessionIds, bool muted) =>
        ForEach(sessionIds, s => s.SimpleAudioVolume.Mute = muted);

    private void ForEach(IEnumerable<string> sessionIds, Action<AudioSessionControl> change)
    {
        var ids = sessionIds.ToHashSet();
        lock (_lock)
        {
            try
            {
                if (_device is null)
                    return;
                SessionCollection all = _device.AudioSessionManager.Sessions;
                for (int i = 0; i < all.Count; i++)
                {
                    if (ids.Contains(all[i].GetSessionInstanceIdentifier))
                        change(all[i]);
                }
            }
            catch (Exception)
            {
            }
        }
    }

    // Nombre del ejecutable de un proceso ("chrome"); se guarda para no preguntarlo cada vez.
    private string ProcessName(int pid)
    {
        if (_processNames.TryGetValue(pid, out string? name))
            return name;
        try
        {
            using var process = Process.GetProcessById(pid);
            name = process.ProcessName;
        }
        catch (Exception)
        {
            name = ""; // ya se cerró, o no se puede leer (proceso de otro usuario)
        }
        _processNames[pid] = name;
        return name;
    }

    private void BindDefaultDevice()
    {
        lock (_lock)
        {
            if (_device is not null)
            {
                try
                {
                    _device.AudioSessionManager.OnSessionCreated -= OnSessionCreated;
                    _device.Dispose();
                }
                catch (Exception)
                {
                }
                _device = null;
            }
            try
            {
                _device = _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                _device.AudioSessionManager.OnSessionCreated += OnSessionCreated;
            }
            catch (Exception)
            {
                _device = null; // sin altavoces conectados
            }
        }
        Changed?.Invoke();
    }

    private void OnSessionCreated(object sender, IAudioSessionControl newSession) => Changed?.Invoke();

    // --- Avisos de Windows sobre dispositivos (llegan en otro hilo; no se llama a la API dentro) ---

    public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
    {
        if (flow == DataFlow.Render && role == Role.Multimedia)
            Task.Run(BindDefaultDevice);
    }

    public void OnDeviceStateChanged(string deviceId, DeviceState newState) => Changed?.Invoke();

    public void OnDeviceAdded(string pwstrDeviceId) => Changed?.Invoke();

    public void OnDeviceRemoved(string deviceId) => Changed?.Invoke();

    public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key) { }

    public void Dispose()
    {
        try
        {
            _enumerator.UnregisterEndpointNotificationCallback(this);
        }
        catch (Exception)
        {
        }
        lock (_lock)
        {
            if (_device is not null)
            {
                try
                {
                    _device.AudioSessionManager.OnSessionCreated -= OnSessionCreated;
                    _device.Dispose();
                }
                catch (Exception)
                {
                }
            }
        }
        _enumerator.Dispose();
    }
}
