namespace Nox.Core.Hotkeys;

/// <summary>Teclas que se mantienen pulsadas en un atajo. Mismos valores que usa Windows (MOD_*).</summary>
[Flags]
public enum HotkeyModifiers
{
    None = 0,
    Alt = 0x1,
    Ctrl = 0x2,
    Shift = 0x4,
    Win = 0x8,
}

/// <summary>Lo que hace cada atajo.</summary>
public enum HotkeyAction
{
    ToggleNotch,
    PlayPause,
    LyricsEarlier,
    LyricsLater,
    LyricsReset,
}

/// <summary>
/// Un atajo de teclado ("Ctrl+Alt+N"): teclas modificadoras más una tecla.
/// <paramref name="Key"/> es el código de tecla de Windows (VK_*), para no depender de WPF.
/// </summary>
public readonly record struct Hotkey(HotkeyModifiers Modifiers, int Key)
{
    // Nombre ↔ código de tecla. Solo las teclas que tiene sentido usar en un atajo.
    private static readonly Dictionary<string, int> Keys = BuildKeys();
    private static readonly Dictionary<int, string> Names = Keys
        .GroupBy(k => k.Value).ToDictionary(g => g.Key, g => g.First().Key);

    /// <summary>
    /// Tiene que llevar Ctrl, Alt o Win: con solo Mayús (o nada) taparía letras normales al escribir.
    /// </summary>
    public bool IsValid => Names.ContainsKey(Key) && (Modifiers & (HotkeyModifiers.Ctrl | HotkeyModifiers.Alt | HotkeyModifiers.Win)) != 0;

    /// <summary>"Ctrl+Alt+N". Mayús se escribe "Mayús" (como en los teclados en español).</summary>
    public override string ToString()
    {
        var parts = new List<string>();
        if (Modifiers.HasFlag(HotkeyModifiers.Ctrl)) parts.Add("Ctrl");
        if (Modifiers.HasFlag(HotkeyModifiers.Alt)) parts.Add("Alt");
        if (Modifiers.HasFlag(HotkeyModifiers.Shift)) parts.Add("Mayús");
        if (Modifiers.HasFlag(HotkeyModifiers.Win)) parts.Add("Win");
        parts.Add(Names.TryGetValue(Key, out string? name) ? name : $"Tecla {Key}");
        return string.Join("+", parts);
    }

    /// <summary>Lee "Ctrl+Alt+N" (sin distinguir mayúsculas; acepta "Shift" y "Mayús"). Null si no es válido.</summary>
    public static Hotkey? Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;
        string[] parts = text.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        // "Ctrl+Alt++" → la última tecla es "+".
        if (text.TrimEnd().EndsWith("++", StringComparison.Ordinal))
            parts = [.. parts, "+"];
        if (parts.Length == 0)
            return null;

        var modifiers = HotkeyModifiers.None;
        foreach (string part in parts[..^1])
        {
            HotkeyModifiers? modifier = part.ToLowerInvariant() switch
            {
                "ctrl" or "control" => HotkeyModifiers.Ctrl,
                "alt" => HotkeyModifiers.Alt,
                "shift" or "mayús" or "mayus" => HotkeyModifiers.Shift,
                "win" or "windows" => HotkeyModifiers.Win,
                _ => null,
            };
            if (modifier is null)
                return null;
            modifiers |= modifier.Value;
        }

        if (!Keys.TryGetValue(parts[^1], out int key))
            return null;
        var hotkey = new Hotkey(modifiers, key);
        return hotkey.IsValid ? hotkey : null;
    }

    /// <summary>¿Este código de tecla puede ser la tecla final de un atajo?</summary>
    public static bool IsUsableKey(int key) => Names.ContainsKey(key);

    private static Dictionary<string, int> BuildKeys()
    {
        var keys = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (char c = 'A'; c <= 'Z'; c++)
            keys[c.ToString()] = c;
        for (char c = '0'; c <= '9'; c++)
            keys[c.ToString()] = c;
        for (int f = 1; f <= 12; f++)
            keys[$"F{f}"] = 0x70 + f - 1;
        // El primer nombre de cada tecla es el que se muestra; los demás solo se aceptan al leer.
        keys["Espacio"] = 0x20;
        keys["Space"] = 0x20;
        keys["Izquierda"] = 0x25;
        keys["Arriba"] = 0x26;
        keys["Derecha"] = 0x27;
        keys["Abajo"] = 0x28;
        keys["Inicio"] = 0x24;
        keys["Fin"] = 0x23;
        keys["Re Pág"] = 0x21;
        keys["Av Pág"] = 0x22;
        keys["Left"] = 0x25;
        keys["Up"] = 0x26;
        keys["Right"] = 0x27;
        keys["Down"] = 0x28;
        keys["Home"] = 0x24;
        keys["End"] = 0x23;
        keys["PageUp"] = 0x21;
        keys["PageDown"] = 0x22;
        keys[","] = 0xBC;
        keys["."] = 0xBE;
        keys["-"] = 0xBD;
        keys["+"] = 0xBB;
        return keys;
    }
}
