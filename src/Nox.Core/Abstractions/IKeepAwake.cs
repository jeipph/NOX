namespace Nox.Core.Abstractions;

/// <summary>Evita que el PC se suspenda y que se apague la pantalla mientras está activado.</summary>
public interface IKeepAwake
{
    bool IsEnabled { get; }

    /// <summary>Llamar siempre desde el mismo hilo (el de la interfaz): Windows lo asocia al hilo.</summary>
    void SetEnabled(bool enabled);
}
