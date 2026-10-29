namespace Nox.Core.Hotkeys;

/// <summary>
/// Los atajos de teclado de NOX tal como se guardan en settings.json ("Ctrl+Alt+N"; "" = sin atajo).
/// Por defecto Ctrl+Alt con teclas que no escriben nada con AltGr en los teclados español y latinoamericano
/// (AltGr equivale a Ctrl+Alt: Ctrl+Alt+Q taparía la @).
/// </summary>
public sealed record HotkeySettings
{
    public bool Enabled { get; init; } = true;

    public string ToggleNotch { get; init; } = "Ctrl+Alt+N";

    // Ctrl+Alt+Espacio estaba ocupado por otra app en el PC de la persona (probado 2026-10-29).
    public string PlayPause { get; init; } = "Ctrl+Alt+P";

    public string LyricsEarlier { get; init; } = "Ctrl+Alt+,";

    public string LyricsLater { get; init; } = "Ctrl+Alt+.";

    public string LyricsReset { get; init; } = "Ctrl+Alt+0";

    public static IReadOnlyList<HotkeyAction> Actions { get; } = Enum.GetValues<HotkeyAction>();

    /// <summary>Nombre para mostrar en la ventana de atajos.</summary>
    public static string Describe(HotkeyAction action) => action switch
    {
        HotkeyAction.ToggleNotch => "Abrir o cerrar el notch",
        HotkeyAction.PlayPause => "Reproducir / pausar",
        HotkeyAction.LyricsEarlier => "Letra 0,5 s antes",
        HotkeyAction.LyricsLater => "Letra 0,5 s después",
        _ => "Letra sin desfase",
    };

    public string Get(HotkeyAction action) => action switch
    {
        HotkeyAction.ToggleNotch => ToggleNotch,
        HotkeyAction.PlayPause => PlayPause,
        HotkeyAction.LyricsEarlier => LyricsEarlier,
        HotkeyAction.LyricsLater => LyricsLater,
        _ => LyricsReset,
    };

    public HotkeySettings With(HotkeyAction action, string value) => action switch
    {
        HotkeyAction.ToggleNotch => this with { ToggleNotch = value },
        HotkeyAction.PlayPause => this with { PlayPause = value },
        HotkeyAction.LyricsEarlier => this with { LyricsEarlier = value },
        HotkeyAction.LyricsLater => this with { LyricsLater = value },
        _ => this with { LyricsReset = value },
    };

    /// <summary>
    /// Los atajos que se pueden registrar. Los que no se entienden o están repetidos dentro de NOX
    /// se devuelven aparte con el motivo.
    /// </summary>
    public (IReadOnlyDictionary<HotkeyAction, Hotkey> Usable, IReadOnlyDictionary<HotkeyAction, string> Problems) Check()
    {
        var usable = new Dictionary<HotkeyAction, Hotkey>();
        var problems = new Dictionary<HotkeyAction, string>();
        foreach (HotkeyAction action in Actions)
        {
            string text = Get(action);
            if (string.IsNullOrWhiteSpace(text))
                continue; // sin atajo: no es un problema
            if (Hotkey.Parse(text) is not { } hotkey)
            {
                problems[action] = "No se entiende (usa Ctrl, Alt o Win más una tecla)";
                continue;
            }
            HotkeyAction[] same = [.. usable.Where(u => u.Value == hotkey).Select(u => u.Key)];
            if (same.Length > 0)
            {
                problems[action] = $"Repetido con «{Describe(same[0])}»";
                continue;
            }
            usable[action] = hotkey;
        }
        return (usable, problems);
    }
}
