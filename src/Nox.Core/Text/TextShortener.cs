namespace Nox.Core.Text;

public static class TextShortener
{
    /// <summary>Recorta el texto a <paramref name="maxLength"/> caracteres como mucho, terminando en "…".</summary>
    public static string Shorten(string text, int maxLength)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxLength, 1);

        text = text.Trim();
        if (text.Length <= maxLength)
            return text;

        return text[..(maxLength - 1)].TrimEnd() + "…";
    }

    /// <summary>Vista previa en una sola línea: saltos de línea y espacios repetidos se convierten en un espacio.</summary>
    public static string OneLine(string text, int maxLength) =>
        Shorten(string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)), maxLength);
}
