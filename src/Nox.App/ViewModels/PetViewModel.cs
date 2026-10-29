using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nox.Core.Pets;

namespace Nox.App.ViewModels;

/// <summary>
/// Pestaña "Mascota": qué mascota se ve (se guarda en settings.json) y el juego en sí.
/// La vista hace avanzar el juego solo mientras se ve.
/// </summary>
public partial class PetViewModel : ObservableObject, IDisposable
{
    private readonly NotchViewModel _options;

    public PetViewModel(NotchViewModel options)
    {
        _options = options;
        _options.PropertyChanged += OnOptionsChanged;
    }

    public PetPlayground Playground { get; } = new();

    public PetKind Kind => _options.Pet;

    public bool IsGrayCat => Kind == PetKind.GrayCat;

    public bool IsTabbyCat => Kind == PetKind.TabbyCat;

    public bool IsBeagle => Kind == PetKind.Beagle;

    [RelayCommand]
    private void Choose(PetKind kind) => _options.Pet = kind;

    private void OnOptionsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(NotchViewModel.Pet))
            return;
        OnPropertyChanged(nameof(Kind));
        OnPropertyChanged(nameof(IsGrayCat));
        OnPropertyChanged(nameof(IsTabbyCat));
        OnPropertyChanged(nameof(IsBeagle));
    }

    public void Dispose() => _options.PropertyChanged -= OnOptionsChanged;
}
