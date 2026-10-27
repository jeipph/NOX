using Microsoft.Win32;
using Nox.Core.Abstractions;

namespace Nox.Windows.Shell;

/// <summary>
/// "Iniciar con Windows": un valor en <c>HKCU\...\Run</c>, la lista de programas que arrancan
/// al iniciar sesión. Es del usuario actual, así que no hace falta ser administrador.
/// </summary>
public sealed class AutoStart : IAutoStart
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "NOX";

    public bool IsEnabled
    {
        get
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKey);
            return key?.GetValue(ValueName) is string command && command.Contains(ExePath(), StringComparison.OrdinalIgnoreCase);
        }
    }

    public void SetEnabled(bool enabled)
    {
        using RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKey);
        if (enabled)
            key.SetValue(ValueName, $"\"{ExePath()}\"");
        else
            key.DeleteValue(ValueName, throwOnMissingValue: false);
    }

    /// <summary>Si estaba activado con el nombre viejo de la app, lo cambia al nombre y la ruta actuales.</summary>
    public static void MigrateFromOldName(string oldName)
    {
        using RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKey);
        if (key.GetValue(oldName) is null)
            return;

        key.DeleteValue(oldName, throwOnMissingValue: false);
        key.SetValue(ValueName, $"\"{ExePath()}\"");
    }

    private static string ExePath() => Environment.ProcessPath ?? "";
}
