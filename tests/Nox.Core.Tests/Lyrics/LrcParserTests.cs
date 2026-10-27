using Nox.Core.Lyrics;

namespace Nox.Core.Tests.Lyrics;

public class LrcParserTests
{
    private static TimeSpan Ms(int milliseconds) => TimeSpan.FromMilliseconds(milliseconds);

    [Fact]
    public void SimpleLines_AreParsedInOrder()
    {
        var lines = LrcParser.Parse("[00:01.00] Hola\n[00:03.50] Mundo");

        Assert.Equal(2, lines.Count);
        Assert.Equal(Ms(1000), lines[0].Start);
        Assert.Equal("Hola", lines[0].Text);
        Assert.Equal(Ms(3500), lines[1].Start);
        Assert.Equal("Mundo", lines[1].Text);
    }

    [Theory]
    [InlineData("[01:02] x", 62_000)]
    [InlineData("[01:02.4] x", 62_400)]
    [InlineData("[01:02.45] x", 62_450)]
    [InlineData("[01:02.456] x", 62_456)]
    [InlineData("[01:02:45] x", 62_450)]
    [InlineData("[100:00.00] x", 6_000_000)]
    public void TimeFormats_AreUnderstood(string lrc, int expectedMs)
    {
        Assert.Equal(Ms(expectedMs), LrcParser.Parse(lrc)[0].Start);
    }

    [Fact]
    public void SeveralTagsOnOneLine_RepeatTheText_AndAreSorted()
    {
        var lines = LrcParser.Parse("[00:20.00][00:05.00] Estribillo\n[00:10.00] Estrofa");

        Assert.Equal(["Estribillo", "Estrofa", "Estribillo"], lines.Select(l => l.Text));
        Assert.Equal([Ms(5000), Ms(10_000), Ms(20_000)], lines.Select(l => l.Start));
    }

    [Fact]
    public void MetadataAndUntimedText_AreIgnored()
    {
        var lines = LrcParser.Parse("[ar:Artista]\n[ti:Título]\nTexto suelto\n[00:01.00] Línea");

        Assert.Single(lines);
        Assert.Equal("Línea", lines[0].Text);
    }

    [Fact]
    public void EmptyTimedLines_AreKept_AsPauses()
    {
        var lines = LrcParser.Parse("[00:01.00] Canto\n[00:05.00]\n[00:09.00] Sigo");

        Assert.Equal(3, lines.Count);
        Assert.Equal("", lines[1].Text);
    }

    [Fact]
    public void WindowsLineEndings_AreHandled()
    {
        var lines = LrcParser.Parse("[00:01.00] Uno\r\n[00:02.00] Dos\r\n");

        Assert.Equal(["Uno", "Dos"], lines.Select(l => l.Text));
    }

    [Fact]
    public void OffsetTag_MovesLinesEarlier_WithoutGoingNegative()
    {
        var lines = LrcParser.Parse("[offset:+500]\n[00:00.20] A\n[00:02.00] B");

        Assert.Equal(TimeSpan.Zero, lines[0].Start);
        Assert.Equal(Ms(1500), lines[1].Start);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("solo texto sin tiempos")]
    public void NothingTimed_ReturnsEmpty(string? lrc)
    {
        Assert.Empty(LrcParser.Parse(lrc));
    }
}
