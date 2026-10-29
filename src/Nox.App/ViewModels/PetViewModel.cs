using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nox.Core.Pets;

namespace Nox.App.ViewModels;

/// <summary>
/// Pestaña "Mascota": qué mascota y qué juguete se ven (se guardan en settings.json) y el juego en sí.
/// La vista hace avanzar el juego solo mientras se ve.
/// </summary>
public partial class PetViewModel : ObservableObject, IDisposable
{
    private readonly NotchViewModel _options;

    public PetViewModel(NotchViewModel options)
    {
        _options = options;
        _options.PropertyChanged += OnOptionsChanged;
        Playground.Toy = options.PetToy;
    }

    public PetPlayground Playground { get; } = new();

    public PetKind Kind => _options.Pet;

    public PetToy Toy => _options.PetToy;

    public bool IsGrayCat => Kind == PetKind.GrayCat;

    public bool IsTabbyCat => Kind == PetKind.TabbyCat;

    public bool IsBeagle => Kind == PetKind.Beagle;

    public bool IsYarn => Toy == PetToy.Yarn;

    public bool IsBall => Toy == PetToy.Ball;

    public bool IsMouse => Toy == PetToy.Mouse;

    [RelayCommand]
    private void Choose(PetKind kind) => _options.Pet = kind;

    [RelayCommand]
    private void ChooseToy(PetToy toy) => _options.PetToy = toy;

    private void OnOptionsChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(NotchViewModel.Pet):
                OnPropertyChanged(nameof(Kind));
                OnPropertyChanged(nameof(IsGrayCat));
                OnPropertyChanged(nameof(IsTabbyCat));
                OnPropertyChanged(nameof(IsBeagle));
                break;
            case nameof(NotchViewModel.PetToy):
                Playground.Toy = _options.PetToy;
                OnPropertyChanged(nameof(Toy));
                OnPropertyChanged(nameof(IsYarn));
                OnPropertyChanged(nameof(IsBall));
                OnPropertyChanged(nameof(IsMouse));
                break;
        }
    }

    public void Dispose() => _options.PropertyChanged -= OnOptionsChanged;
}
