using NotchBar.Core.Text;

namespace NotchBar.Core.Notes;

/// <summary>Una nota guardada en disco.</summary>
public sealed record NoteFile(string Path, DateTime SavedAt, string Preview);

/// <summary>
/// Notas rápidas como archivos Markdown <c>yyyy-MM-dd_HHmmss.md</c> en una carpeta
/// (por defecto <c>Documentos\NotchBar\notes</c>). Archivos normales: se pueden abrir con cualquier editor.
/// </summary>
public sealed class NotesStore
{
    public const int PreviewLength = 60;

    public static string DefaultFolder { get; } = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "NotchBar", "notes");

    public string Folder { get; set; } = DefaultFolder;

    /// <summary>Guarda la nota y devuelve la ruta del archivo. Nunca sobrescribe otra nota.</summary>
    public string Save(string text, DateTime now)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("La nota está vacía.", nameof(text));

        Directory.CreateDirectory(Folder);
        string name = now.ToString("yyyy-MM-dd_HHmmss");
        string path = System.IO.Path.Combine(Folder, name + ".md");
        for (int copy = 2; File.Exists(path); copy++)
            path = System.IO.Path.Combine(Folder, $"{name}-{copy}.md");

        // Escritura atómica: si algo falla a mitad, no queda una nota a medias.
        string temporary = path + ".tmp";
        File.WriteAllText(temporary, text.Trim() + Environment.NewLine);
        File.Move(temporary, path);
        return path;
    }

    /// <summary>Las últimas notas (también las creadas fuera de la app), la más reciente primero.</summary>
    public IReadOnlyList<NoteFile> Recent(int count = 10)
    {
        if (!Directory.Exists(Folder))
            return [];

        return new DirectoryInfo(Folder)
            .EnumerateFiles("*.md")
            .OrderByDescending(f => f.LastWriteTime)
            .ThenByDescending(f => f.Name, StringComparer.Ordinal)
            .Take(count)
            .Select(f => new NoteFile(f.FullName, f.LastWriteTime, PreviewOf(f.FullName)))
            .ToList();
    }

    private static string PreviewOf(string path)
    {
        try
        {
            string? firstLine = File.ReadLines(path).FirstOrDefault(l => !string.IsNullOrWhiteSpace(l));
            return firstLine is null ? "(vacía)" : TextShortener.OneLine(firstLine.TrimStart('#', ' '), PreviewLength);
        }
        catch (IOException)
        {
            return "(no se pudo leer)";
        }
    }
}
