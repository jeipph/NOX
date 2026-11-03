using System.Globalization;

namespace Nox.Core.Cleanup;

/// <summary>Tamaños en megabytes para mostrarlos («12,3 MB»).</summary>
public static class SizeText
{
    private const double BytesPerMegabyte = 1024 * 1024;

    /// <summary>Con un decimal por debajo de 100 MB y sin decimales a partir de ahí.</summary>
    public static string Megabytes(long bytes, IFormatProvider? culture = null)
    {
        culture ??= CultureInfo.CurrentCulture;
        double megabytes = Math.Max(bytes, 0) / BytesPerMegabyte;
        return megabytes < 100
            ? megabytes.ToString("0.0", culture) + " MB"
            : megabytes.ToString("#,0", culture) + " MB";
    }
}
