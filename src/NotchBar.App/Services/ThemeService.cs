using System.Windows;
using Microsoft.Win32;
using NotchBar.Core.Models;
using NotchBar.Windows.Shell;

namespace NotchBar.App.Services;

/// <summary>
/// Cambia los colores en vivo reemplazando el diccionario de colores (Tokens.Dark / Tokens.Light).
/// Como las vistas usan DynamicResource, todo se repinta solo.
/// </summary>
public sealed class ThemeService : IDisposable
{
    private static readonly Uri DarkColors = new("pack://application:,,,/NotchBar.App;component/Themes/Tokens.Dark.xaml");
    private static readonly Uri LightColors = new("pack://application:,,,/NotchBar.App;component/Themes/Tokens.Light.xaml");

    private AppTheme _theme = AppTheme.System;
    private bool? _isLight;

    public ThemeService()
    {
        // Solo importa con el tema "Sistema": si cambias el modo de Windows, la app lo sigue.
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
    }

    public void Apply(AppTheme theme)
    {
        _theme = theme;
        Update();
    }

    private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (_theme == AppTheme.System && e.Category == UserPreferenceCategory.General)
            Application.Current.Dispatcher.BeginInvoke(Update);
    }

    private void Update()
    {
        bool isLight = _theme switch
        {
            AppTheme.Light => true,
            AppTheme.Dark => false,
            _ => SystemTheme.AppsUseLightTheme(),
        };
        if (isLight == _isLight)
            return;
        _isLight = isLight;

        var dictionaries = Application.Current.Resources.MergedDictionaries;
        var replacement = new ResourceDictionary { Source = isLight ? LightColors : DarkColors };
        int index = IndexOfColors(dictionaries);
        if (index >= 0)
            dictionaries[index] = replacement;
        else
            dictionaries.Add(replacement);
    }

    private static int IndexOfColors(IList<ResourceDictionary> dictionaries)
    {
        for (int i = 0; i < dictionaries.Count; i++)
        {
            string? source = dictionaries[i].Source?.OriginalString;
            if (source is not null && (source.EndsWith("Tokens.Dark.xaml") || source.EndsWith("Tokens.Light.xaml")))
                return i;
        }
        return -1;
    }

    public void Dispose() => SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
}
