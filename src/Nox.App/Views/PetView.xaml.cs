using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Nox.App.ViewModels;

namespace Nox.App.Views;

/// <summary>Pestaña Mascota: elegir mascota y juguete, y el escenario donde juega.</summary>
public partial class PetView : UserControl
{
    private PetViewModel? _viewModel;

    public PetView()
    {
        InitializeComponent();
        Stage.Scale = (double)FindResource("Pet.Scale");
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_viewModel is not null)
            _viewModel.PropertyChanged -= OnViewModelChanged;
        _viewModel = e.NewValue as PetViewModel;
        if (_viewModel is null)
            return;
        _viewModel.PropertyChanged += OnViewModelChanged;
        Stage.Kind = _viewModel.Kind;
        Stage.Playground = _viewModel.Playground;
    }

    private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(PetViewModel.Kind) or nameof(PetViewModel.Toy) && _viewModel is not null)
        {
            Stage.Kind = _viewModel.Kind;
            Stage.Draw();
        }
    }
}
