using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Nox.App.ViewModels;
using Nox.Core.Pets;

namespace Nox.App.Views;

/// <summary>
/// Dibuja el juego de la mascota. El temporizador solo corre si la pestaña se ve (notch abierto
/// y pestaña Mascota): con el notch cerrado no gasta CPU, igual que el vinilo.
/// </summary>
public partial class PetView : UserControl
{
    // Las imágenes de cada postura se crean una vez y se reutilizan.
    private static readonly Dictionary<(PetKind, PetPose), BitmapSource> PetFrames = [];
    private static readonly Dictionary<int, BitmapSource> BallFrames = [];

    private readonly DispatcherTimer _timer = new();
    private readonly Stopwatch _clock = new();
    private double _scale = 1;

    public PetView()
    {
        InitializeComponent();
        _timer.Tick += (_, _) => Tick();
        IsVisibleChanged += (_, _) => UpdateRunning();
        Loaded += (_, _) =>
        {
            _scale = (double)FindResource("Pet.Scale");
            _timer.Interval = (TimeSpan)FindResource("Pet.FrameInterval");
            Stage.Width = PetPlayground.StageWidth * _scale;
            Stage.Height = PetPlayground.StageHeight * _scale;
            Ground.Width = Stage.Width;
            Canvas.SetTop(Ground, Stage.Height - Ground.Height);
            PetImage.Width = PetSprites.Width * _scale;
            PetImage.Height = PetSprites.Height * _scale;
            BallImage.Width = BallImage.Height = PetSprites.BallSize * _scale;
            Draw();
        };
    }

    private PetViewModel? ViewModel => DataContext as PetViewModel;

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
        // Se avanza el tiempo real transcurrido: si la PC va lenta, la mascota no va a cámara lenta.
        double seconds = _clock.Elapsed.TotalSeconds;
        _clock.Restart();
        ViewModel?.Playground.Step(seconds);
        Draw();
    }

    private void Draw()
    {
        if (ViewModel is not { } vm)
            return;
        PetPlayground game = vm.Playground;

        PetImage.Source = PetFrame(vm.Kind, game.Pose);
        PetFlip.ScaleX = game.FacingRight ? 1 : -1;
        Canvas.SetLeft(PetImage, Math.Round(game.PetX) * _scale);
        Canvas.SetTop(PetImage, (PetPlayground.StageHeight - PetSprites.Height - Math.Round(game.PetY)) * _scale);

        BallImage.Source = BallFrame(game.BallFrame);
        Canvas.SetLeft(BallImage, Math.Round(game.BallX) * _scale);
        Canvas.SetTop(BallImage, (PetPlayground.StageHeight - PetSprites.BallSize - Math.Round(game.BallY)) * _scale);
    }

    private static BitmapSource PetFrame(PetKind kind, PetPose pose)
    {
        if (!PetFrames.TryGetValue((kind, pose), out BitmapSource? frame))
            PetFrames[(kind, pose)] = frame = ToBitmap(PetSprites.Frame(kind, pose), PetSprites.Palette(kind));
        return frame;
    }

    private static BitmapSource BallFrame(int index)
    {
        if (!BallFrames.TryGetValue(index, out BitmapSource? frame))
            BallFrames[index] = frame = ToBitmap(PetSprites.Ball(index), PetSprites.BallPalette);
        return frame;
    }

    // ARGB en un uint queda en memoria como B, G, R, A: justo el formato Bgra32.
    private static BitmapSource ToBitmap(IReadOnlyList<string> rows, IReadOnlyDictionary<char, uint> palette)
    {
        int width = rows[0].Length;
        uint[] pixels = PetSprites.Render(rows, palette);
        var bitmap = BitmapSource.Create(width, rows.Count, 96, 96, PixelFormats.Bgra32, null, pixels, width * 4);
        bitmap.Freeze();
        return bitmap;
    }
}
