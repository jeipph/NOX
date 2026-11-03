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
/// Al pasar el ratón cerca, la mascota se gira hacia él (y si dormía, se despierta); al hacer clic
/// encima, maúlla o ladra. También dibuja el bocadillo de texto, el cuenco y los popós.
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
    private readonly Image _bowl = new() { IsHitTestVisible = false };
    private readonly Image[] _poops = [.. Enumerable.Range(0, PetCare.MaxPoops).Select(_ => new Image { IsHitTestVisible = false })];
    private readonly Border _bubble = new() { IsHitTestVisible = false, Visibility = Visibility.Collapsed };
    private readonly TextBlock _bubbleText = new();
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

        // El bocadillo usa los colores del tema (fondo, borde y texto), no los del dibujo.
        _bubble.SetResourceReference(Border.BackgroundProperty, "Brush.Surface");
        _bubble.SetResourceReference(Border.BorderBrushProperty, "Brush.Border");
        _bubble.SetResourceReference(Border.CornerRadiusProperty, "Radius.PetBubble");
        _bubble.SetResourceReference(Border.PaddingProperty, "Padding.PetBubble");
        _bubble.BorderThickness = new Thickness(1);
        _bubbleText.SetResourceReference(TextBlock.FontFamilyProperty, "Font.Main");
        _bubbleText.SetResourceReference(TextBlock.FontSizeProperty, "FontSize.Micro");
        _bubbleText.SetResourceReference(TextBlock.FontWeightProperty, "FontWeight.Title");
        _bubbleText.SetResourceReference(TextBlock.ForegroundProperty, "Brush.Text");
        _bubble.Child = _bubbleText;
        // Los lectores de pantalla leen el bocadillo cuando cambia.
        System.Windows.Automation.AutomationProperties.SetLiveSetting(_bubbleText, System.Windows.Automation.AutomationLiveSetting.Polite);

        // Detrás de la mascota: suelo y popós; delante: el cuenco (mete el hocico) y el bocadillo.
        Children.Add(_ground);
        foreach (Image poop in _poops)
            Children.Add(poop);
        Children.Add(_toy);
        Children.Add(_pet);
        Children.Add(_bowl);
        Children.Add(_bubble);
        _pet.MouseLeftButtonDown += OnPetClicked;

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
        _bowl.Width = PetSprites.BowlWidth * Scale;
        _bowl.Height = PetSprites.BowlHeight * Scale;
        foreach (Image poop in _poops)
        {
            poop.Width = PetSprites.PoopWidth * Scale;
            poop.Height = PetSprites.PoopHeight * Scale;
            poop.Source = PetBitmaps.Poop();
        }
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

        _bowl.Visibility = game.HasBowl ? Visibility.Visible : Visibility.Collapsed;
        if (game.HasBowl)
        {
            _bowl.Source = PetBitmaps.Bowl(game.BowlFull);
            SetLeft(_bowl, game.BowlX * Scale);
            SetTop(_bowl, (PetPlayground.StageHeight - PetSprites.BowlHeight) * Scale);
        }

        for (int i = 0; i < _poops.Length; i++)
        {
            _poops[i].Visibility = i < game.Poops ? Visibility.Visible : Visibility.Collapsed;
            SetLeft(_poops[i], game.PoopX(i) * Scale);
            SetTop(_poops[i], (PetPlayground.StageHeight - PetSprites.PoopHeight) * Scale);
        }

        DrawBubble(game);
    }

    // Sobre la cabeza (que está a la derecha del dibujo si mira a la derecha), sin salirse del escenario.
    private void DrawBubble(PetPlayground game)
    {
        string text = PetSpeech.Text(Kind, game.Bubble);
        if (text.Length == 0)
        {
            _bubble.Visibility = Visibility.Collapsed;
            return;
        }
        if (_bubbleText.Text != text)
            _bubbleText.Text = text;
        _bubble.Visibility = Visibility.Visible;
        _bubble.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        Size size = _bubble.DesiredSize;

        double headCenter = Math.Round(game.PetX) + (game.FacingRight ? PetSprites.Width - 4.5 : 4.5);
        double petTop = (PetPlayground.StageHeight - PetSprites.Height - Math.Round(game.PetY)) * Scale;
        double gap = (double)FindResource("Space.PetBubble");
        SetLeft(_bubble, Math.Clamp(headCenter * Scale - size.Width / 2, 0, Math.Max(Width - size.Width, 0)));
        SetTop(_bubble, Math.Max(petTop - size.Height - gap, 0));
    }

    // Clic sobre la mascota: maúlla o ladra.
    private void OnPetClicked(object sender, MouseButtonEventArgs e)
    {
        if (_playground is null)
            return;
        _playground.Touch(e.GetPosition(this).X / Scale);
        e.Handled = true;
        Draw();
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
