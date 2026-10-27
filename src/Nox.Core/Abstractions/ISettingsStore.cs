using Nox.Core.Models;

namespace Nox.Core.Abstractions;

public interface ISettingsStore
{
    /// <summary>Lee las opciones; si no hay archivo o está dañado, devuelve los valores por defecto.</summary>
    AppSettings Load();

    void Save(AppSettings settings);
}

/// <summary>Arrancar la app al iniciar sesión en Windows.</summary>
public interface IAutoStart
{
    bool IsEnabled { get; }

    void SetEnabled(bool enabled);
}
