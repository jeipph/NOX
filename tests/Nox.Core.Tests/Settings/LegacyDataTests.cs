using Nox.Core.Settings;

namespace Nox.Core.Tests.Settings;

public sealed class LegacyDataTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "NOXTests", Guid.NewGuid().ToString("N"));

    private string Old => Path.Combine(_root, "NotchBar");
    private string New => Path.Combine(_root, "NOX");

    [Fact]
    public void OldFolder_IsMoved_WithItsFiles()
    {
        Directory.CreateDirectory(Path.Combine(Old, "notes"));
        File.WriteAllText(Path.Combine(Old, "notes", "nota.md"), "hola");

        var moved = LegacyData.MoveFolders([(Old, New)]);

        Assert.Equal([New], moved);
        Assert.False(Directory.Exists(Old));
        Assert.Equal("hola", File.ReadAllText(Path.Combine(New, "notes", "nota.md")));
    }

    [Fact]
    public void NewFolderAlreadyThere_NothingIsTouched()
    {
        Directory.CreateDirectory(Old);
        Directory.CreateDirectory(New);
        File.WriteAllText(Path.Combine(Old, "settings.json"), "viejo");

        var moved = LegacyData.MoveFolders([(Old, New)]);

        Assert.Empty(moved);
        Assert.True(File.Exists(Path.Combine(Old, "settings.json")));
    }

    [Fact]
    public void NoOldFolder_DoesNothing()
    {
        Assert.Empty(LegacyData.MoveFolders([(Old, New)]));
        Assert.False(Directory.Exists(New));
    }

    [Fact]
    public void UpdatePath_FollowsTheMove()
    {
        var moves = new[] { (Old, New) };

        Assert.Equal(Path.Combine(New, "notes"), LegacyData.UpdatePath(Path.Combine(Old, "notes"), moves));
        Assert.Equal(New, LegacyData.UpdatePath(Old, moves));
        Assert.Equal(@"D:\Mis notas", LegacyData.UpdatePath(@"D:\Mis notas", moves));
        Assert.Null(LegacyData.UpdatePath(null, moves));
    }

    [Fact]
    public void UpdatePath_DoesNotConfuseSimilarNames()
    {
        var moves = new[] { (Old, New) };
        string similar = Path.Combine(_root, "NotchBarExtra", "x");

        Assert.Equal(similar, LegacyData.UpdatePath(similar, moves));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }
}
