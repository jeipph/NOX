using System.Runtime.InteropServices;

namespace Nox.Windows.Audio;

/// <summary>
/// Cambia la salida de audio predeterminada. Windows no tiene una función documentada para esto:
/// se usa la interfaz interna <c>IPolicyConfig</c>, la misma que usa el panel de sonido de Windows
/// y apps como EarTrumpet o SoundSwitch desde Windows 7. Si un día Windows la cambia, la llamada
/// falla y NOX solo lo avisa (riesgo evaluado en M6d, ver docs/06-LICENSES-AND-RISKS.md).
/// Código propio: solo se declara la interfaz.
/// </summary>
internal static class PolicyConfig
{
    // Los tres usos de una salida en Windows: general, multimedia y comunicaciones (llamadas).
    private static readonly int[] Roles = [0, 1, 2];

    /// <returns>False si Windows no lo aceptó.</returns>
    public static bool SetDefaultOutput(string deviceId)
    {
        object? instance = null;
        try
        {
            instance = Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("870AF99C-171D-4F9E-AF0D-E63DF40C2BC9"))!);
            if (instance is not IPolicyConfig policy)
                return false;
            foreach (int role in Roles)
            {
                if (policy.SetDefaultEndpoint(deviceId, role) != 0)
                    return false;
            }
            return true;
        }
        catch (Exception exception) when (exception is COMException or InvalidCastException or ArgumentException or NotSupportedException)
        {
            return false;
        }
        finally
        {
            if (instance is not null && Marshal.IsComObject(instance))
                Marshal.ReleaseComObject(instance);
        }
    }

    // Solo importa el orden de los métodos (la tabla de la interfaz): los que no se usan se
    // declaran con parámetros genéricos para ocupar su sitio.
    [ComImport]
    [Guid("F8679F50-850A-41CF-9C72-430F290290C8")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPolicyConfig
    {
        [PreserveSig] int GetMixFormat(nint a, nint b);
        [PreserveSig] int GetDeviceFormat(nint a, int b, nint c);
        [PreserveSig] int ResetDeviceFormat(nint a);
        [PreserveSig] int SetDeviceFormat(nint a, nint b, nint c);
        [PreserveSig] int GetProcessingPeriod(nint a, int b, nint c, nint d);
        [PreserveSig] int SetProcessingPeriod(nint a, nint b);
        [PreserveSig] int GetShareMode(nint a, nint b);
        [PreserveSig] int SetShareMode(nint a, nint b);
        [PreserveSig] int GetPropertyValue(nint a, nint b, nint c);
        [PreserveSig] int SetPropertyValue(nint a, nint b, nint c);
        [PreserveSig] int SetDefaultEndpoint([MarshalAs(UnmanagedType.LPWStr)] string deviceId, int role);
        [PreserveSig] int SetEndpointVisibility(nint a, int b);
    }
}
