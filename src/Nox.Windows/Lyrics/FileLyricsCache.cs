using System.Text.Json;
using Nox.Core.Abstractions;
using Nox.Core.Models;

namespace Nox.Windows.Lyrics;

/// <summary>
/// Guarda cada letra en <c>%LOCALAPPDATA%\NOX\lyrics\&lt;clave&gt;.json</c>.
/// La escritura es atómica: primero un .tmp y luego se renombra.
/// </summary>
public sealed class FileLyricsCache : ILyricsCache
{
    private readonly string _folder;

    public FileLyricsCache(string? folder = null)
    {
        _folder = folder ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NOX", "lyrics");
    }

    public async Task<LyricsResult?> LoadAsync(string key, CancellationToken cancellationToken)
    {
        string path = PathFor(key);
        if (!File.Exists(path))
            return null;

        try
        {
            await using FileStream stream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<LyricsResult>(stream, cancellationToken: cancellationToken);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            // Archivo dañado: se ignora y se vuelve a buscar.
            return null;
        }
    }

    public async Task SaveAsync(string key, LyricsResult lyrics, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_folder);
        string path = PathFor(key);
        string temporary = path + ".tmp";

        await using (FileStream stream = File.Create(temporary))
            await JsonSerializer.SerializeAsync(stream, lyrics, cancellationToken: cancellationToken);

        File.Move(temporary, path, overwrite: true);
    }

    private string PathFor(string key) => Path.Combine(_folder, key + ".json");
}
