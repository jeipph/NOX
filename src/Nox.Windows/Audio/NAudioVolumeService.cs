using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using Nox.Core.Abstractions;

namespace Nox.Windows.Audio;

/// <summary>
/// Volumen de los altavoces o auriculares predeterminados (NAudio sobre la API de audio de Windows).
/// Todo por avisos: Windows avisa si cambia el volumen o si cambias de dispositivo de salida.
/// </summary>
public sealed class NAudioVolumeService : IVolumeService, IMMNotificationClient, IDisposable
{
    private readonly MMDeviceEnumerator _enumerator = new();
    private readonly Lock _lock = new();
    private MMDevice? _device;

    public event Action? Changed;

    public NAudioVolumeService()
    {
        _enumerator.RegisterEndpointNotificationCallback(this);
        BindDefaultDevice();
    }

    public float Level
    {
        get
        {
            lock (_lock)
                return Try(() => _device?.AudioEndpointVolume.MasterVolumeLevelScalar ?? 0f);
        }
    }

    public bool IsMuted
    {
        get
        {
            lock (_lock)
                return Try(() => _device?.AudioEndpointVolume.Mute ?? false);
        }
    }

    public void SetLevel(float level)
    {
        lock (_lock)
            Try(() =>
            {
                if (_device is not null)
                    _device.AudioEndpointVolume.MasterVolumeLevelScalar = Math.Clamp(level, 0f, 1f);
                return true;
            });
    }

    public void SetMuted(bool muted)
    {
        lock (_lock)
            Try(() =>
            {
                if (_device is not null)
                    _device.AudioEndpointVolume.Mute = muted;
                return true;
            });
    }

    private void BindDefaultDevice()
    {
        lock (_lock)
        {
            Unbind();
            try
            {
                _device = _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                _device.AudioEndpointVolume.OnVolumeNotification += OnVolumeNotification;
            }
            catch (Exception)
            {
                _device = null; // Sin altavoces conectados.
            }
        }
        Changed?.Invoke();
    }

    private void Unbind()
    {
        if (_device is null)
            return;

        try
        {
            _device.AudioEndpointVolume.OnVolumeNotification -= OnVolumeNotification;
            _device.Dispose();
        }
        catch (Exception)
        {
        }
        _device = null;
    }

    private void OnVolumeNotification(AudioVolumeNotificationData data) => Changed?.Invoke();

    // El dispositivo puede desaparecer en cualquier momento (desconectar auriculares).
    private static T Try<T>(Func<T> read)
    {
        try
        {
            return read();
        }
        catch (Exception)
        {
            return default!;
        }
    }

    // --- Avisos de Windows sobre dispositivos ---

    public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
    {
        // Windows pide no llamar a la API de audio dentro de este aviso: se hace en otro hilo.
        if (flow == DataFlow.Render && role == Role.Multimedia)
            Task.Run(BindDefaultDevice);
    }

    public void OnDeviceStateChanged(string deviceId, DeviceState newState) { }

    public void OnDeviceAdded(string pwstrDeviceId) { }

    public void OnDeviceRemoved(string deviceId) { }

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
            Unbind();
        _enumerator.Dispose();
    }
}
