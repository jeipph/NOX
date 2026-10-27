namespace Nox.Core.Models;

/// <summary>Un reproductor que está publicando música (navegador, MusicBee...).</summary>
/// <param name="Id">Identificador que da Windows (AppUserModelId).</param>
/// <param name="Name">Nombre legible para mostrar.</param>
public sealed record MediaSource(string Id, string Name);
