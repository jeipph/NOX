using System.Security.Cryptography;
using System.Text;
using Nox.Core.Models;

namespace Nox.Core.Lyrics;

public static class LyricsKey
{
    /// <summary>
    /// Nombre de archivo para la caché: resumen (hash) de "artista|título|duración".
    /// Así el nombre no lleva caracteres raros y no se ve qué canción es mirando la carpeta.
    /// </summary>
    public static string For(MediaInfo song)
    {
        string text = $"{song.Artist.Trim().ToLowerInvariant()}|{song.Title.Trim().ToLowerInvariant()}|{Math.Round(song.Duration.TotalSeconds)}";
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexStringLower(hash.AsSpan(0, 16));
    }
}
