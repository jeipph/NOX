namespace Nox.Core.Pets;

/// <summary>Los intervalos del aviso de descanso que se pueden elegir (en minutos).</summary>
public static class BreakIntervals
{
    /// <summary>Lo menos y lo más que se puede escribir en «Otro…».</summary>
    public const int MinMinutes = 5;
    public const int MaxMinutes = 480;

    /// <summary>Las opciones fijas del menú de la bandeja (además de «Nunca» y «Otro…»).</summary>
    public static IReadOnlyList<int> Presets { get; } = [15, 20, 30, 40, 45, 60, 90, 120];

    /// <summary>True si el valor no es una opción fija ni «Nunca» (lo marca «Otro…»).</summary>
    public static bool IsCustom(int minutes) => minutes > 0 && !Presets.Contains(minutes);

    /// <summary>
    /// Lee los minutos escritos en «Otro…». Acepta espacios alrededor; solo números enteros
    /// entre <see cref="MinMinutes"/> y <see cref="MaxMinutes"/>.
    /// </summary>
    /// <param name="error">Si no vale, el motivo (en español) para mostrarlo.</param>
    public static bool TryParse(string? text, out int minutes, out string error)
    {
        minutes = 0;
        error = "";
        string trimmed = text?.Trim() ?? "";
        if (trimmed.Length == 0)
        {
            error = "Escribe un número de minutos.";
            return false;
        }
        if (!int.TryParse(trimmed, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out int value))
        {
            error = "Solo números enteros, sin letras ni decimales.";
            return false;
        }
        if (value < MinMinutes || value > MaxMinutes)
        {
            error = $"Tiene que estar entre {MinMinutes} y {MaxMinutes} minutos.";
            return false;
        }
        minutes = value;
        return true;
    }
}
