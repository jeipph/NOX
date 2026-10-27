namespace NotchBar.Core.Text;

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
}
