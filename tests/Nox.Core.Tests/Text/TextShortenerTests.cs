using Nox.Core.Text;

namespace Nox.Core.Tests.Text;

public class TextShortenerTests
{
    [Fact]
    public void ShortText_IsUnchanged()
    {
        Assert.Equal("Hola", TextShortener.Shorten("Hola", 24));
    }

    [Fact]
    public void LongText_EndsWithEllipsisAndFits()
    {
        string result = TextShortener.Shorten("Una canción con un título larguísimo de verdad", 24);
        Assert.Equal(24, result.Length);
        Assert.EndsWith("…", result);
    }

    [Fact]
    public void SpaceBeforeEllipsis_IsRemoved()
    {
        Assert.Equal("abc…", TextShortener.Shorten("abc defgh", 5));
    }

    [Fact]
    public void SurroundingSpaces_AreTrimmed()
    {
        Assert.Equal("Hola", TextShortener.Shorten("  Hola  ", 24));
    }

    [Fact]
    public void InvalidMaxLength_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => TextShortener.Shorten("Hola", 0));
    }
}
