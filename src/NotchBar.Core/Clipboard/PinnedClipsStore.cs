using System.Text.Json;

namespace NotchBar.Core.Clipboard;

/// <summary>
/// Guarda los textos fijados del portapapeles en <c>%APPDATA%\NotchBar\pinned-clips.json</c>.
/// Solo los fijados: el resto del historial nunca se escribe en disco.
/// </summary>
public sealed class PinnedClipsStore
{
    private readonly string _path;

    public PinnedClipsStore(string? path = null)
    {
        _path = path ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NotchBar", "pinned-clips.json");
    }

    public IReadOnlyList<string> Load()
    {
        try
        {
            return File.Exists(_path)
                ? JsonSerializer.Deserialize<List<string>>(File.ReadAllText(_path)) ?? []
                : [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public void Save(IEnumerable<string> texts)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        string temporary = _path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(texts.ToList()));
        File.Move(temporary, _path, overwrite: true);
    }
}
