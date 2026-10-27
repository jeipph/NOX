namespace NotchBar.Core.Abstractions;

/// <summary>Portapapeles de Windows (solo texto).</summary>
public interface IClipboardService
{
    /// <summary>Se copió un texto en cualquier app. No llega si viene de un gestor de contraseñas.</summary>
    event Action<string>? TextCopied;

    /// <summary>Pone un texto en el portapapeles. Devuelve false si estaba bloqueado por otra app.</summary>
    bool SetText(string text);
}
