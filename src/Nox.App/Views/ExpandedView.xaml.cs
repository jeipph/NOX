using System.Windows;
using System.Windows.Controls;

namespace Nox.App.Views;

public partial class ExpandedView : UserControl
{
    public ExpandedView()
    {
        InitializeComponent();
    }

    /// <summary>El asa de la fila de pestañas: la ventana del notch escucha aquí el ratón para arrastrarse.</summary>
    public UIElement DragHandle => DragHandleElement;

    /// <summary>Se pulsó × (ocultar el notch).</summary>
    public event Action? CloseClicked;

    private void OnCloseClick(object sender, RoutedEventArgs e) => CloseClicked?.Invoke();
}
