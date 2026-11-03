using System.ComponentModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Nox.Core.Pets;

namespace Nox.App.ViewModels;

/// <summary>
/// Pestaña "Mascota": qué mascota y qué juguete se ven (se guardan en settings.json), el juego en sí
/// y sus cuidados (hambre y popós, con <see cref="PetCare"/>). La vista hace avanzar el juego solo
/// mientras se ve; los cuidados usan un único temporizador hasta el próximo cambio, sin sondeo.
/// </summary>
public partial class PetViewModel : ObservableObject, IDisposable
{
    private readonly NotchViewModel _options;
    private readonly DispatcherTimer _careTimer = new();
    private PetCare? _care;

    public PetViewModel(NotchViewModel options)
    {
        _options = options;
        _options.PropertyChanged += OnOptionsChanged;
        Playground.Toy = options.PetToy;
        _careTimer.Tick += (_, _) =>
        {
            _careTimer.Stop();
            RefreshCare();
        };
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

    /// <summary>Lleva 4 h sin comer: bocadillo «¡Tengo hambre!» y boca abierta en el notch cerrado.</summary>
    [ObservableProperty]
    private bool _isHungry;

    /// <summary>Popós en el suelo del escenario (de 0 a 3).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPoops))]
    [NotifyCanExecuteChangedFor(nameof(CleanCommand))]
    private int _poops;

    public bool HasPoops => Poops > 0;

    /// <summary>Le acaba de entrar hambre: el notch se abre en la pestaña Mascota.</summary>
    public event Action? HungerStarted;

    [RelayCommand]
    private void Choose(PetKind kind) => _options.Pet = kind;

    [RelayCommand]
    private void ChooseToy(PetToy toy) => _options.PetToy = toy;

    /// <summary>
    /// Empieza a contar el hambre y los popós desde lo guardado (si el PC estuvo apagado, se pone al día).
    /// Llamar una vez, con las opciones ya cargadas.
    /// </summary>
    /// <param name="fast">Modo de prueba (--fake): las horas son segundos.</param>
    public void StartCare(bool fast)
    {
        if (_care is not null)
            return;
        _care = fast
            ? new PetCare(_options.PetCare, DateTime.UtcNow, TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(5))
            : new PetCare(_options.PetCare, DateTime.UtcNow);
        // Tras suspender el PC el temporizador puede ir tarde: al volver se recalcula.
        SystemEvents.PowerModeChanged += OnPowerModeChanged;
        RefreshCare();
    }

    /// <summary>«Dar comida»: aparece el cuenco, la mascota va a comer y se le quita el hambre.</summary>
    [RelayCommand]
    private void Feed()
    {
        if (_care is null)
            return;
        _care.Feed(DateTime.UtcNow);
        Playground.PlaceFood();
        RefreshCare();
    }

    /// <summary>«Limpiar»: quita los popós del suelo.</summary>
    [RelayCommand(CanExecute = nameof(HasPoops))]
    private void Clean()
    {
        if (_care is null)
            return;
        _care.Clean(DateTime.UtcNow);
        RefreshCare();
    }

    // Pone al día hambre y popós, lo guarda y programa el temporizador para el próximo cambio.
    private void RefreshCare()
    {
        if (_care is null)
            return;
        _careTimer.Stop();
        DateTime now = DateTime.UtcNow;
        _care.Update(now);

        bool hungry = _care.IsHungry(now);
        bool becameHungry = hungry && !IsHungry;
        IsHungry = hungry;
        Poops = _care.Poops;
        Playground.IsHungry = hungry;
        Playground.Poops = _care.Poops;
        if (_options.PetCare != _care.State)
            _options.PetCare = _care.State; // se guarda en settings.json

        if (_care.NextChange(now) is DateTime next)
        {
            // Un pelín después de la hora justa, para no despertar un instante antes y no ver nada.
            TimeSpan wait = next - now + TimeSpan.FromMilliseconds(50);
            _careTimer.Interval = wait > TimeSpan.Zero ? wait : TimeSpan.FromMilliseconds(50);
            _careTimer.Start();
        }

        if (becameHungry)
            HungerStarted?.Invoke();
    }

    private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.Resume)
            System.Windows.Application.Current?.Dispatcher.BeginInvoke(RefreshCare);
    }

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

    public void Dispose()
    {
        _careTimer.Stop();
        _options.PropertyChanged -= OnOptionsChanged;
        SystemEvents.PowerModeChanged -= OnPowerModeChanged;
    }
}
