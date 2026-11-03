using System.Globalization;
using System.Windows.Data;

namespace Nox.App.Converters;

/// <summary>
/// True si el valor enlazado es igual al parámetro (comparados como texto). Sirve para marcar la
/// opción elegida en un menú sin una propiedad por opción: <c>ConverterParameter=40</c>.
/// </summary>
public sealed class EqualsConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        string.Equals(System.Convert.ToString(value, CultureInfo.InvariantCulture),
            System.Convert.ToString(parameter, CultureInfo.InvariantCulture), StringComparison.Ordinal);

    // Solo se usa en un sentido (Mode=OneWay).
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Binding.DoNothing;
}
