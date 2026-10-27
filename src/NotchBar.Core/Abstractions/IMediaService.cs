using NotchBar.Core.Models;

namespace NotchBar.Core.Abstractions;

/// <summary>Lee lo que suena en el PC y permite controlarlo.</summary>
public interface IMediaService
{
    /// <summary>Se dispara cuando cambia la canción, la pausa o la posición. Null = nada sonando.
    /// Puede llegar desde un hilo que no es el de la interfaz.</summary>
    event Action<MediaInfo?>? MediaChanged;

    MediaInfo? Current { get; }

    Task StartAsync();

    Task TogglePlayPauseAsync();

    Task NextAsync();

    Task PreviousAsync();
}
