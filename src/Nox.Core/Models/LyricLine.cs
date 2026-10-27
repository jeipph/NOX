namespace Nox.Core.Models;

/// <summary>Una línea de letra con el momento en que empieza a cantarse.</summary>
public sealed record LyricLine(TimeSpan Start, string Text);
