namespace Nox.Core.Settings;

/// <summary>
/// La app se llamó "NotchBar" hasta el 2026-10-27. Al arrancar, sus carpetas de datos
/// (ajustes, fijados, caché de letras, notas) se trasladan a las de NOX, una sola vez.
/// </summary>
public static class LegacyData
{
    public const string OldName = "NotchBar";
    public const string NewName = "NOX";

    /// <summary>Las carpetas de datos con el nombre viejo y su destino.</summary>
    public static IReadOnlyList<(string From, string To)> DefaultMoves()
    {
        string[] roots =
        [
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        ];
        return roots.Select(root => (Path.Combine(root, OldName), Path.Combine(root, NewName))).ToList();
    }

    /// <summary>
    /// Mueve cada carpeta si existe la vieja y todavía no la nueva (nunca mezcla ni sobrescribe).
    /// Devuelve las carpetas movidas. Si una falla (archivo en uso), se sigue con las demás.
    /// </summary>
    public static IReadOnlyList<string> MoveFolders(IEnumerable<(string From, string To)> moves)
    {
        var moved = new List<string>();
        foreach (var (from, to) in moves)
        {
            if (!Directory.Exists(from) || Directory.Exists(to))
                continue;

            try
            {
                Directory.Move(from, to);
                moved.Add(to);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
        return moved;
    }

    /// <summary>Corrige una ruta guardada que apuntaba dentro de una carpeta vieja (por ejemplo, la de notas).</summary>
    public static string? UpdatePath(string? path, IEnumerable<(string From, string To)> moves)
    {
        if (path is null)
            return null;

        foreach (var (from, to) in moves)
        {
            if (path.Equals(from, StringComparison.OrdinalIgnoreCase))
                return to;
            if (path.StartsWith(from + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                return to + path[from.Length..];
        }
        return path;
    }
}
