namespace Nox.Core.Lyrics;

public static class ArtistNames
{
    // YouTube Music junta artistas así: "Gnarls Barkley, CeeLo Green y Danger Mouse".
    private static readonly string[] Separators =
        [", ", " & ", " y ", " and ", " x ", " feat. ", " feat ", " ft. ", " ft ", " con ", " with "];

    /// <summary>El primer artista de una lista ("A, B y C" → "A").</summary>
    public static string Primary(string artists)
    {
        string result = artists.Trim();
        foreach (string separator in Separators)
        {
            int index = result.IndexOf(separator, StringComparison.OrdinalIgnoreCase);
            if (index > 0)
                result = result[..index];
        }
        return result.Trim();
    }
}
