using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Nox.Core.Pets;
using Nox.Core.Shell;
using Nox.Windows.Shell;

namespace Nox.App.Windows;

/// <summary>
/// Aviso de descanso: la mascota corre y juega por la parte de abajo de la pantalla, encima de todo,
/// con un mensaje. No roba el teclado; se cierra con «Ya descansé», ×, «5 min más» o Esc.
/// Se puede mover arrastrando la tarjeta.
/// </summary>
public partial class BreakWindow : Window
{
    private readonly PetKind _kind;
    private readonly PetToy _toy;
    private readonly EscapeKeyHook _escapeKey = new();
    private nint _hwnd;
    private PixelRect _workArea;

    public BreakWindow(PetKind kind, PetToy toy, string workedFor)
    {
        InitializeComponent();
        _kind = kind;
        _toy = toy;
        Message.Text = $"Llevas {workedFor} con el PC. Levántate, estírate, mira algo lejano 20 segundos y bebe agua.";
        _escapeKey.EscapePressed += () => Dispatcher.BeginInvoke(() => Finish(BreakChoice.Done));
        Closed += (_, _) => _escapeKey.Dispose();
    }

    /// <summary>Qué eligió la persona (o Esc = «ya descansé»).</summary>
    public event Action<BreakChoice>? Chosen;

    private bool _finished;

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        _hwnd = new WindowInteropHelper(this).Handle;
        WindowHelper.MakeOverlay(_hwnd); // no aparece en Alt+Tab ni quita el foco

        // Todo el ancho de la pantalla, abajo, justo encima de la barra de tareas.
        // Escalado del monitor principal: la ventana puede nacer en el otro monitor y luego pasar a este.
        double screenScale = WindowHelper.GetPrimaryMonitorScale();
        var (_, work) = WindowHelper.GetPrimaryMonitor();
        _workArea = work;
        HwndSource.FromHwnd(_hwnd).AddHook(WndProc);
        double scale = (double)FindResource("Pet.BreakScale");
        Width = work.Width / screenScale;
        double stageHeight = PetPlayground.StageHeight * scale;
        Measure(new Size(Width, double.PositiveInfinity));
        Height = stageHeight + ((FrameworkElement)Content).DesiredSize.Height;

        int pixelHeight = (int)Math.Ceiling(Height * screenScale);
        WindowHelper.PlaceTopmost(_hwnd, new PixelRect(work.Left, work.Bottom - pixelHeight, work.Right, work.Bottom));

        Stage.Scale = scale;
        Stage.Kind = _kind;
        Stage.Playground = new PetPlayground(stageWidth: Width / scale) { Toy = _toy };
        _escapeKey.Start();
    }

    // Mientras se arrastra, Windows pregunta adónde va: se corrige para que no se salga de la pantalla.
    private nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        if (WindowHelper.KeepMovingInside(msg, lParam, _workArea))
        {
            handled = true;
            return 1;
        }
        return 0;
    }

    // Arrastrar la tarjeta mueve el aviso (los botones no: ellos se quedan el clic).
    private void OnCardMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
            DragMove();
    }

    private void OnDoneClick(object sender, RoutedEventArgs e) => Finish(BreakChoice.Done);

    private void OnSnoozeClick(object sender, RoutedEventArgs e) => Finish(BreakChoice.Snooze);

    /// <summary>Cerrar sin elegir (por ejemplo, porque la persona ya se fue a descansar).</summary>
    public void CloseQuietly()
    {
        _finished = true;
        Close();
    }

    private void Finish(BreakChoice choice)
    {
        if (_finished)
            return;
        _finished = true;
        Chosen?.Invoke(choice);
        Close();
    }
}

public enum BreakChoice
{
    Done,
    Snooze,
}
