namespace NotchBar.Core.Media;

/// <summary>Convierte el identificador que da Windows a un reproductor en un nombre legible.</summary>
public static class SourceNames
{
    // Si el identificador contiene la clave (sin distinguir mayúsculas), se usa el nombre conocido.
    private static readonly (string Key, string Name)[] Known =
    [
        ("_crx_", "App de Chrome"),
        ("chrome", "Chrome"),
        ("msedge", "Edge"),
        ("firefox", "Firefox"),
        ("opera", "Opera"),
        ("brave", "Brave"),
        ("spotify", "Spotify"),
        ("musicbee", "MusicBee"),
        ("foobar2000", "foobar2000"),
        ("zunemusic", "Reproductor multimedia"),
        ("vlc", "VLC"),
    ];

    public static string Friendly(string? sourceId)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
            return "Desconocido";

        foreach (var (key, name) in Known)
        {
            if (sourceId.Contains(key, StringComparison.OrdinalIgnoreCase))
                return name;
        }

        // Formas habituales: "C:\ruta\App.exe", "Empresa.App_8wekyb3d8bbwe!App".
        string cleaned = sourceId;
        int slash = cleaned.LastIndexOf('\\');
        if (slash >= 0)
            cleaned = cleaned[(slash + 1)..];
        int bang = cleaned.IndexOf('!');
        if (bang >= 0)
            cleaned = cleaned[..bang];
        int underscore = cleaned.IndexOf('_');
        if (underscore > 0)
            cleaned = cleaned[..underscore];
        if (cleaned.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            cleaned = cleaned[..^4];
        int dot = cleaned.LastIndexOf('.');
        if (dot >= 0 && dot < cleaned.Length - 1)
            cleaned = cleaned[(dot + 1)..];

        return cleaned.Length > 0 ? cleaned : "Desconocido";
    }
}
