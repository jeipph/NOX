namespace Nox.Core.Models;

public enum AppTheme
{
    /// <summary>Sigue el modo claro/oscuro de Windows.</summary>
    System,
    Dark,
    Light,
}

/// <summary>
/// Opciones que se guardan en <c>%APPDATA%\NOX\settings.json</c>.
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

    /// <summary>Carpeta de las notas rápidas; null = Documentos\NOX\notes.</summary>
    public string? NotesFolder { get; init; }

    /// <summary>Mascota de la pestaña "Mascota".</summary>
    public Pets.PetKind Pet { get; init; } = Pets.PetKind.GrayCat;

    public Pets.PetToy PetToy { get; init; } = Pets.PetToy.Yarn;

    /// <summary>La mascota sentada a la izquierda del notch cerrado.</summary>
    public bool PetInCollapsed { get; init; } = true;

    /// <summary>Cada cuántos minutos aparece la mascota para recordar descansar; 0 = nunca.</summary>
    public int BreakMinutes { get; init; } = 40;

    /// <summary>Atajos de teclado globales.</summary>
    public Hotkeys.HotkeySettings Hotkeys { get; init; } = new();
}
