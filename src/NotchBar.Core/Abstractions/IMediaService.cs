using NotchBar.Core.Models;

namespace NotchBar.Core.Abstractions;

/// <summary>Lee lo que suena en el PC y permite controlarlo.</summary>
public interface IMediaService
{
    /// <summary>Se dispara cuando cambia la canción, la pausa o la posición. Null = nada sonando.
    /// Puede llegar desde un hilo que no es el de la interfaz.</summary>
    event Action<MediaInfo?>? MediaChanged;

    /// <summary>Se dispara cuando se abre o se cierra un reproductor. Puede llegar desde otro hilo.</summary>
    event Action? SourcesChanged;

    MediaInfo? Current { get; }

    /// <summary>Reproductores abiertos ahora mismo (sin repetidos).</summary>
    IReadOnlyList<MediaSource> Sources { get; }

    /// <summary>Reproductor elegido por la persona, o null si es automático.</summary>
    string? PreferredSourceId { get; }

    Task StartAsync();

    /// <summary>Fija el reproductor que se muestra; null vuelve al modo automático.</summary>
    Task SetPreferredSourceAsync(string? sourceId);

    Task TogglePlayPauseAsync();

    Task NextAsync();

    Task PreviousAsync();
}
