using Nox.Core.Lyrics;

namespace Nox.Core.Tests.Lyrics;

public class ArtistNamesTests
{
    [Theory]
    [InlineData("Tame Impala", "Tame Impala")]
    [InlineData("Gnarls Barkley, CeeLo Green y Danger Mouse", "Gnarls Barkley")]
    [InlineData("Aaron Smith y Krono", "Aaron Smith")]
    [InlineData("Drake feat. Rihanna", "Drake")]
    [InlineData("Daft Punk & Pharrell Williams", "Daft Punk")]
    [InlineData("  Cuco  ", "Cuco")]
    public void Primary_TakesTheFirstArtist(string artists, string expected)
    {
        Assert.Equal(expected, ArtistNames.Primary(artists));
    }
}
