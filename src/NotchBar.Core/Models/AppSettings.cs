namespace NotchBar.Core.Models;

public enum AppTheme
{
    /// <summary>Sigue el modo claro/oscuro de Windows.</summary>
    System,
    Dark,
    Light,
}

/// <summary>
/// Opciones que se guardan en <c>%APPDATA%\NotchBar\settings.json</c>.
/// "Iniciar con Windows" no está aquí: se guarda solo en el registro de Windows.
/// </summary>
public sealed record AppSettings
{
    public AppTheme Theme { get; init; } = AppTheme.System;

    public NotchStyle Style { get; init; } = NotchStyle.Notch;

    public bool OnlyOnHover { get; init; }

    public bool ReserveTopStrip { get; init; }

    public bool LyricsEnabled { get; init; } = true;

    /// <summary>Reproductor fijado con la etiqueta del notch; null = automático.</summary>
    public string? PreferredSourceId { get; init; }

    /// <summary>Carpeta de las notas rápidas; null = Documentos\NotchBar\notes.</summary>
    public string? NotesFolder { get; init; }
}
