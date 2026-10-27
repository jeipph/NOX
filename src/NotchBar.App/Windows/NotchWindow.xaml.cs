using System.Windows;

namespace NotchBar.App.Windows;

public partial class NotchWindow : Window
{
    public NotchWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => PlaceAtTopCenter();
    }

    // Centra la ventana en el borde superior del monitor principal.
    // El ajuste por DPI y por monitor llega en M2.
    private void PlaceAtTopCenter()
    {
        Left = (SystemParameters.PrimaryScreenWidth - ActualWidth) / 2;
        Top = 0;
    }
}
