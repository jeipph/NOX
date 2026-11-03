namespace Nox.Core.Shell;

/// <summary>
/// Cuentas para colocar el notch en cualquier parte de la pantalla, en píxeles físicos.
/// La posición que se guarda es el <b>centro de la pastilla cerrada</b>, como fracción del área de
/// trabajo (0 = izquierda/arriba, 1 = derecha/abajo): así sigue en su sitio si cambia la resolución.
/// La ventana es más grande que la pastilla (dentro crece al abrirse): en la mitad de arriba la
/// pastilla va pegada arriba de la ventana y crece hacia abajo; en la de abajo, al revés.
/// </summary>
public static class NotchLayout
{
    /// <summary>True si la pastilla, con su centro en esa fracción de alto, debe crecer hacia arriba.</summary>
    public static bool GrowsUp(double fractionY) => fractionY > 0.5;

    /// <summary>Convierte un punto de la pantalla en fracción del área de trabajo (entre 0 y 1).</summary>
    public static (double X, double Y) ToFraction(PixelRect area, double x, double y) =>
        (Fraction(x, area.Left, area.Width), Fraction(y, area.Top, area.Height));

    /// <summary>
    /// Centro de la pastilla cerrada a partir de la fracción guardada, corrido lo justo para que la
    /// pastilla (<paramref name="pillWidth"/> × <paramref name="pillHeight"/>) no se salga del área.
    /// </summary>
    public static (int X, int Y) PillCenter(PixelRect area, double fractionX, double fractionY, int pillWidth, int pillHeight)
    {
        double x = area.Left + Math.Clamp(fractionX, 0, 1) * area.Width;
        double y = area.Top + Math.Clamp(fractionY, 0, 1) * area.Height;
        return (ClampCenter(x, area.Left, area.Right, pillWidth), ClampCenter(y, area.Top, area.Bottom, pillHeight));
    }

    /// <summary>
    /// Esquina de arriba a la izquierda de la ventana para que la pastilla cerrada quede centrada en
    /// <paramref name="pillCenterX"/>, <paramref name="pillCenterY"/>.
    /// </summary>
    /// <param name="edgeGap">Separación entre la pastilla y el borde de la ventana del que cuelga (estilo Cápsula).</param>
    public static (int Left, int Top) WindowForPill(int pillCenterX, int pillCenterY, int windowWidth, int windowHeight,
        int pillHeight, int edgeGap, bool growsUp)
    {
        int left = pillCenterX - windowWidth / 2;
        int halfPill = pillHeight / 2;
        int top = growsUp
            ? pillCenterY + (pillHeight - halfPill) + edgeGap - windowHeight
            : pillCenterY - halfPill - edgeGap;
        return (left, top);
    }

    /// <summary>
    /// Dónde está la pastilla dentro de la ventana colocada en <paramref name="windowLeft"/>,
    /// <paramref name="windowTop"/> (centrada en horizontal, pegada arriba o abajo).
    /// </summary>
    public static PixelRect PillRect(int windowLeft, int windowTop, int windowWidth, int windowHeight,
        int pillWidth, int pillHeight, int edgeGap, bool growsUp)
    {
        int left = windowLeft + (windowWidth - pillWidth) / 2;
        int top = growsUp ? windowTop + windowHeight - edgeGap - pillHeight : windowTop + edgeGap;
        return new PixelRect(left, top, left + pillWidth, top + pillHeight);
    }

    /// <summary>
    /// Cuánto hay que mover <paramref name="rect"/> para que quede dentro de <paramref name="area"/>
    /// (0, 0 si ya cabe). Si es más grande que el área, se alinea arriba a la izquierda.
    /// </summary>
    public static (int Dx, int Dy) ShiftToFit(PixelRect area, PixelRect rect) =>
        (Shift(rect.Left, rect.Right, area.Left, area.Right), Shift(rect.Top, rect.Bottom, area.Top, area.Bottom));

    /// <summary>
    /// Al soltar el notch abierto tras arrastrarlo: centro que tendrá la pastilla cerrada, sin que la
    /// abierta se mueva. Crece hacia arriba si la abierta quedó en la mitad de abajo; entonces la
    /// cerrada comparte el borde de abajo con la abierta, y si no, el de arriba.
    /// </summary>
    public static (int X, int Y, bool GrowsUp) PillCenterAfterDrag(PixelRect area, PixelRect expandedPill, int pillHeight)
    {
        int centerX = expandedPill.Left + expandedPill.Width / 2;
        double middle = expandedPill.Top + expandedPill.Height / 2.0;
        bool growsUp = GrowsUp(Fraction(middle, area.Top, area.Height));
        int halfPill = pillHeight / 2;
        int centerY = growsUp ? expandedPill.Bottom - (pillHeight - halfPill) : expandedPill.Top + halfPill;
        return (centerX, centerY, growsUp);
    }

    private static double Fraction(double value, int start, int length) =>
        length <= 0 ? 0.5 : Math.Clamp((value - start) / length, 0, 1);

    private static int ClampCenter(double center, int min, int max, int size)
    {
        int half = size / 2;
        int low = min + half, high = max - (size - half);
        int rounded = (int)Math.Round(center);
        return high < low ? (min + max) / 2 : Math.Clamp(rounded, low, high);
    }

    private static int Shift(int start, int end, int min, int max)
    {
        if (start < min || end - start > max - min)
            return min - start;
        if (end > max)
            return max - end;
        return 0;
    }
}
