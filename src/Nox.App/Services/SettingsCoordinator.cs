using System.ComponentModel;
using Nox.App.ViewModels;
using Nox.Core.Abstractions;
using Nox.Core.Settings;

namespace Nox.App.Services;

/// <summary>
/// Une las opciones del menú con el disco, el tema y el arranque con Windows:
/// al arrancar las carga y las aplica; cuando cambian, las guarda.
/// </summary>
public sealed class SettingsCoordinator(
    ISettingsStore store,
    NotchViewModel options,
    IMediaService media,
    ThemeService theme,
    IAutoStart autoStart) : IDisposable
{
    public async Task LoadAsync()
    {
        var settings = store.Load();
        // Una carpeta de notas elegida dentro de las carpetas viejas de NotchBar sigue a su traslado.
        settings = settings with { NotesFolder = LegacyData.UpdatePath(settings.NotesFolder, LegacyData.DefaultMoves()) };
        options.Apply(settings);
        // El registro de Windows es quien sabe si de verdad arranca con Windows.
        options.StartWithWindows = autoStart.IsEnabled;
        theme.Apply(options.Theme);

        options.PropertyChanged += OnOptionsChanged;
        await media.SetPreferredSourceAsync(settings.PreferredSourceId);
        media.SourcesChanged += Save;
    }

    private void OnOptionsChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(NotchViewModel.Theme):
                theme.Apply(options.Theme);
                break;
            case nameof(NotchViewModel.StartWithWindows):
                SetAutoStart(options.StartWithWindows);
                return; // No va en settings.json.
        }
        Save();
    }

    private void SetAutoStart(bool enabled)
    {
        try
        {
            autoStart.SetEnabled(enabled);
        }
        catch (Exception)
        {
            // Sin permiso para tocar el registro: se deja la casilla como está en realidad.
            options.StartWithWindows = autoStart.IsEnabled;
        }
    }

    private void Save()
    {
        try
        {
            store.Save(options.ToSettings(media.PreferredSourceId));
        }
        catch (Exception)
        {
            // No se pudo escribir (disco lleno, carpeta bloqueada): se intentará en el siguiente cambio.
        }
    }

    public void Dispose()
    {
        options.PropertyChanged -= OnOptionsChanged;
        media.SourcesChanged -= Save;
    }
}
