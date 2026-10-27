using NotchBar.Core.Media;

namespace NotchBar.Core.Tests.Media;

public class SourceNamesTests
{
    [Theory]
    [InlineData("Chrome", "Chrome")]
    [InlineData("chrome.exe", "Chrome")]
    [InlineData("MSEdge", "Edge")]
    [InlineData("Spotify.exe", "Spotify")]
    [InlineData("SpotifyAB.SpotifyMusic_zpdnekdrzrea0!Spotify", "Spotify")]
    [InlineData("MusicBee.exe", "MusicBee")]
    [InlineData(@"C:\Program Files\foobar2000\foobar2000.exe", "foobar2000")]
    [InlineData("Microsoft.ZuneMusic_8wekyb3d8bbwe!Microsoft.ZuneMusic", "Reproductor multimedia")]
    [InlineData("Chrome._crx_cinhimbnkkaeohfgghhklpknlkffjgod", "App de Chrome")]
    public void KnownPlayers_GetReadableNames(string id, string expected)
    {
        Assert.Equal(expected, SourceNames.Friendly(id));
    }

    [Theory]
    [InlineData(@"D:\Apps\MiReproductor.exe", "MiReproductor")]
    [InlineData("Empresa.MiApp_abc123!App", "MiApp")]
    public void UnknownPlayers_UseTheCleanedId(string id, string expected)
    {
        Assert.Equal(expected, SourceNames.Friendly(id));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_IsUnknown(string? id)
    {
        Assert.Equal("Desconocido", SourceNames.Friendly(id));
    }
}
