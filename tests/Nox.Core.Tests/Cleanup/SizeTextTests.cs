using System.Globalization;
using Nox.Core.Abstractions;
using Nox.Core.Cleanup;

namespace Nox.Core.Tests.Cleanup;

public sealed class SizeTextTests
{
    private static readonly CultureInfo Spanish = CultureInfo.GetCultureInfo("es-ES");

    [Theory]
    [InlineData(0, "0,0 MB")]
    [InlineData(1024 * 1024, "1,0 MB")]
    [InlineData(1024 * 1024 * 25 / 2, "12,5 MB")]
    [InlineData(-5, "0,0 MB")]
    public void Megabytes_WithOneDecimal_BelowAHundred(long bytes, string expected) =>
        Assert.Equal(expected, SizeText.Megabytes(bytes, Spanish));

    [Fact]
    public void Megabytes_BigSizes_WithoutDecimals() =>
        Assert.Equal("16.288 MB", SizeText.Megabytes(16288L * 1024 * 1024, Spanish));

    [Fact]
    public void MemoryStatus_Used_IsTotalMinusAvailable()
    {
        Assert.Equal(6, new MemoryStatus(16, 10).UsedBytes);
        Assert.Equal(0, new MemoryStatus(4, 10).UsedBytes);
    }
}
