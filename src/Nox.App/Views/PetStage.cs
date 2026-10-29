using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Nox.App.Services;
using Nox.Core.Pets;

namespace Nox.App.Views;

/// <summary>
/// Escenario donde juega una mascota (pestaña Mascota y aviso de descanso). Solo anima mientras
/// se ve: con el notch cerrado o la ventana oculta, el temporizador está parado y no gasta CPU.
/// Al pasar el ratón cerca, la mascota se gira hacia él (y si dormía, se despierta).
/// </summary>
public sealed class PetStage : Canvas
{
    private readonly DispatcherTimer _timer = new();
    private readonly Stopwatch _clock = new();
    private readonly Image _pet = new();
    private readonly Image _toy = new();
    private readonly ScaleTransform _petFlip = new();
    private readonly ScaleTransform _toyFlip = new();
    private readonly Rectangle _ground = new();
    private PetPlayground? _playground;

    public PetStage()
    {
        ClipToBounds = true;
        // Fondo casi invisible: así el ratón se detecta en todo el escenario.
        ReactsToMouse = true;
        RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.NearestNeighbor);
        _ground.SetResourceReference(Shape.FillProperty, "Brush.Border");
        _ground.SetResourceReference(HeightProperty, "Size.PetGround");
        _pet.RenderTransformOrigin = _toy.RenderTransformOrigin = new Point(0.5, 0.5);
        _pet.RenderTransform = _petFlip;
        _toy.RenderTransform = _toyFlip;
        System.Windows.Automation.AutomationProperties.SetName(_pet, "Mascota");
        Children.Add(_ground);
        Children.Add(_toy);
        Children.Add(_pet);

        _timer.Tick += (_, _) => Tick();
        IsVisibleChanged += (_, _) => UpdateRunning();
        Loaded += (_, _) =>
        {
            _timer.Interval = (TimeSpan)FindResource("Pet.FrameInterval");
            Layout();
        };
        MouseMove += OnMouseMove;
    }

    /// <summary>Cuántos píxeles de pantalla mide cada píxel del dibujo.</summary>
    public double Scale { get; set; } = 4;

    public PetKind Kind { get; set; }

    /// <summary>Sin línea de suelo (la ventana de descanso flota sobre el escritorio).</summary>
    public bool ShowGround { get; set; } = true;

    /// <summary>
    /// False: el ratón atraviesa el escenario (en el aviso de descanso no debe tapar lo que hay debajo).
    /// </summary>
    public bool ReactsToMouse
    {
        get => Background is not null;
        set
        {
            if (value)
                SetResourceReference(BackgroundProperty, "Brush.HitArea");
            else
                Background = null;
        }
    }

    public PetPlayground? Playground
    {
        get => _playground;
        set
        {
            _playground = value;
            Layout();
        }
    }

    private void Layout()
    {
        if (_playground is null)
            return;
        Width = _playground.StageWidth * Scale;
        Height = PetPlayground.StageHeight * Scale;
        _ground.Width = Width;
        _ground.Visibility = ShowGround ? Visibility.Visible : Visibility.Collapsed;
        SetTop(_ground, Height - 1);
        _pet.Width = PetSprites.Width * Scale;
        _pet.Height = PetSprites.Height * Scale;
        _toy.Width = _toy.Height = PetSprites.ToySize * Scale;
        Draw();
    }

    private void UpdateRunning()
    {
        if (IsVisible)
        {
            _clock.Restart();
            _timer.Start();
        }
        else
        {
            _timer.Stop();
            _clock.Stop();
        }
    }

    private void Tick()
    {
        // Se avanza el tiempo real transcurrido: si el PC va lento, la mascota no va a cámara lenta.
        double seconds = _clock.Elapsed.TotalSeconds;
        _clock.Restart();
        _playground?.Step(seconds);
        Draw();
    }

    /// <summary>Vuelve a dibujar ya (por ejemplo, al cambiar de mascota con la animación parada).</summary>
    public void Draw()
    {
        if (_playground is not { } game)
            return;

        _pet.Source = PetBitmaps.Pet(Kind, game.Pose);
        _petFlip.ScaleX = game.FacingRight ? 1 : -1;
        SetLeft(_pet, Math.Round(game.PetX) * Scale);
        SetTop(_pet, (PetPlayground.StageHeight - PetSprites.Height - Math.Round(game.PetY)) * Scale);

        _toy.Source = PetBitmaps.Toy(game.Toy, game.ToyFrame);
        _toyFlip.ScaleX = game.ToyFacingRight ? 1 : -1;
        SetLeft(_toy, Math.Round(game.ToyX) * Scale);
        SetTop(_toy, (PetPlayground.StageHeight - PetSprites.ToySize - Math.Round(game.ToyY)) * Scale);
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (_playground is null)
            return;
        Point p = e.GetPosition(this);
        double x = p.X / Scale;
        // Solo si el ratón pasa cerca de la mascota, no en cualquier parte del escenario.
        if (Math.Abs(x - (_playground.PetX + PetSprites.Width / 2.0)) < PetSprites.Width * 1.5)
            _playground.Poke(x);
    }
}
