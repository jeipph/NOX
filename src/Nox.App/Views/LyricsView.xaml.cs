using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Nox.App.ViewModels;

namespace Nox.App.Views;

public partial class LyricsView : UserControl
{
    private LyricsViewModel? _viewModel;

    public LyricsView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_viewModel is not null)
            _viewModel.PropertyChanged -= OnViewModelChanged;

        _viewModel = e.NewValue as LyricsViewModel;
        if (_viewModel is not null)
            _viewModel.PropertyChanged += OnViewModelChanged;
    }

    // Al cambiar de línea, la nueva sube un poco y aparece (desplazamiento suave).
    private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(LyricsViewModel.CurrentLine) || !IsVisible)
            return;

        var duration = new Duration((TimeSpan)FindResource("Duration.Expand"));
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        double distance = (double)FindResource("Lyric.SlideDistance");

        var transform = (TranslateTransform)CurrentLineText.RenderTransform;
        transform.BeginAnimation(TranslateTransform.YProperty,
            new DoubleAnimation(distance, 0, duration) { EasingFunction = ease });
        CurrentLineText.BeginAnimation(OpacityProperty,
            new DoubleAnimation(0, 1, duration) { EasingFunction = ease });
    }
}
