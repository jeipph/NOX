using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NotchBar.Core.Models;

namespace NotchBar.App.ViewModels;

// Opciones de aspecto del notch, elegidas desde el menú de la bandeja.
// De momento viven en memoria; en M9 (Ajustes) se guardarán en disco.
public partial class NotchViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotchStyle), nameof(IsCapsuleStyle), nameof(IsIndicatorStyle))]
    private NotchStyle _style = NotchStyle.Notch;

    /// <summary>Si es true, contraído no se ve nada hasta pasar el ratón por arriba.</summary>
    [ObservableProperty]
    private bool _onlyOnHover;

    /// <summary>Si es true, las ventanas maximizadas empiezan debajo del notch.</summary>
    [ObservableProperty]
    private bool _reserveTopStrip;

    public bool IsNotchStyle => Style == NotchStyle.Notch;
    public bool IsCapsuleStyle => Style == NotchStyle.Capsule;
    public bool IsIndicatorStyle => Style == NotchStyle.Indicator;

    [RelayCommand]
    private void SetStyle(NotchStyle style) => Style = style;
}
