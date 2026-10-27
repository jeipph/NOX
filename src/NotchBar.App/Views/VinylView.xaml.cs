using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using NotchBar.App.ViewModels;

namespace NotchBar.App.Views;

/// <summary>
/// El disco solo gira si se ve (notch abierto y pestaña Vinilo) y la música suena.
/// En cualquier otro caso se quita la animación y queda quieto donde estaba: no gasta CPU.
/// </summary>
public partial class VinylView : UserControl
{
    private NowPlayingViewModel? _viewModel;
    private bool _spinning;

    public VinylView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        IsVisibleChanged += (_, _) => UpdateSpin();
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_viewModel is not null)
            _viewModel.PropertyChanged -= OnViewModelChanged;

        _viewModel = e.NewValue as NowPlayingViewModel;
        if (_viewModel is not null)
            _viewModel.PropertyChanged += OnViewModelChanged;

        UpdateSpin();
    }

    private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(NowPlayingViewModel.IsPlaying))
            UpdateSpin();
    }

    private void UpdateSpin()
    {
        bool shouldSpin = IsVisible && _viewModel?.IsPlaying == true;
        if (shouldSpin == _spinning)
            return;
        _spinning = shouldSpin;

        // Mientras gira, Angle devuelve el ángulo animado en ese instante.
        double angle = Rotation.Angle % 360;
        if (shouldSpin)
        {
            var turn = new Duration((TimeSpan)FindResource("Vinyl.TurnDuration"));
            var spin = new DoubleAnimation(angle, angle + 360, turn) { RepeatBehavior = RepeatBehavior.Forever };
            // Un giro lento se ve igual de suave a menos imágenes por segundo, y gasta menos CPU.
            Timeline.SetDesiredFrameRate(spin, (int)FindResource("Vinyl.FrameRate"));
            Rotation.BeginAnimation(RotateTransform.AngleProperty, spin);
        }
        else
        {
            Rotation.BeginAnimation(RotateTransform.AngleProperty, null);
            Rotation.Angle = angle;
        }
    }
}
