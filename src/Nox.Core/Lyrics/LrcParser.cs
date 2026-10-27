using System.Globalization;
using System.Text.RegularExpressions;
using Nox.Core.Models;

namespace Nox.Core.Lyrics;

/// <summary>
/// Lee letras en formato LRC: <c>[01:23.45] texto</c>.
/// Admite varias marcas en una línea (<c>[00:10.00][00:50.00] estribillo</c>),
/// ignora metadatos como <c>[ar:Artista]</c> y aplica <c>[offset:+500]</c> (milisegundos).
/// </summary>
public static partial class LrcParser
{
    // [mm:ss], [mm:ss.x], [mm:ss.xx], [mm:ss.xxx] (también con ":" antes de las fracciones)
    [GeneratedRegex(@"\[(\d{1,3}):(\d{1,2})(?:[.:](\d{1,3}))?\]")]
    private static partial Regex TimeTag();

    [GeneratedRegex(@"^\[offset:\s*([+-]?\d+)\s*\]$", RegexOptions.IgnoreCase)]
    private static partial Regex OffsetTag();

    public static IReadOnlyList<LyricLine> Parse(string? lrc)
    {
        if (string.IsNullOrWhiteSpace(lrc))
            return [];

        var lines = new List<LyricLine>();
        TimeSpan offset = TimeSpan.Zero;

        foreach (string rawLine in lrc.Split('\n'))
        {
            string line = rawLine.Trim();

            Match offsetMatch = OffsetTag().Match(line);
            if (offsetMatch.Success)
            {
                // En LRC, un offset positivo hace que la letra salga antes.
                offset = TimeSpan.FromMilliseconds(int.Parse(offsetMatch.Groups[1].Value, CultureInfo.InvariantCulture));
                continue;
            }

            var starts = new List<TimeSpan>();
            int textStart = 0;
            Match tag = TimeTag().Match(line);
            while (tag.Success && tag.Index == textStart)
            {
                starts.Add(ToTime(tag));
                textStart = tag.Index + tag.Length;
                tag = tag.NextMatch();
            }

            if (starts.Count == 0)
                continue; // metadatos ([ar:], [ti:]...) o texto sin tiempo

            string text = line[textStart..].Trim();
            foreach (TimeSpan start in starts)
                lines.Add(new LyricLine(start, text));
        }

        return lines
            .Select(l => l with { Start = Max(TimeSpan.Zero, l.Start - offset) })
            .OrderBy(l => l.Start)
            .ToList();
    }

    private static TimeSpan ToTime(Match tag)
    {
        int minutes = int.Parse(tag.Groups[1].Value, CultureInfo.InvariantCulture);
        int seconds = int.Parse(tag.Groups[2].Value, CultureInfo.InvariantCulture);
        int milliseconds = 0;

        string fraction = tag.Groups[3].Value;
        if (fraction.Length > 0)
        {
            // ".4" = 400 ms, ".45" = 450 ms, ".456" = 456 ms
            milliseconds = int.Parse(fraction.PadRight(3, '0'), CultureInfo.InvariantCulture);
        }

        return new TimeSpan(0, 0, minutes, seconds, milliseconds);
    }

    private static TimeSpan Max(TimeSpan a, TimeSpan b) => a > b ? a : b;
}
