using NotchBar.Core.Clipboard;
using NotchBar.Core.Text;

namespace NotchBar.Core.Tests.Clipboard;

public sealed class PinnedClipsStoreTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "NotchBarTests", Guid.NewGuid().ToString("N"));

    private string FilePath => Path.Combine(_folder, "pinned-clips.json");

    [Fact]
    public void NoFile_IsEmpty()
    {
        Assert.Empty(new PinnedClipsStore(FilePath).Load());
    }

    [Fact]
    public void Saved_AreLoadedBack_InOrder_WithLineBreaks()
    {
        new PinnedClipsStore(FilePath).Save(["dirección\ncalle 1", "correo@ejemplo.com"]);

        Assert.Equal(["dirección\ncalle 1", "correo@ejemplo.com"], new PinnedClipsStore(FilePath).Load());
    }

    [Fact]
    public void DamagedFile_IsEmpty()
    {
        Directory.CreateDirectory(_folder);
        File.WriteAllText(FilePath, "[ roto");

        Assert.Empty(new PinnedClipsStore(FilePath).Load());
    }

    [Fact]
    public void OneLine_JoinsLinesAndShortens()
    {
        Assert.Equal("uno dos tres", TextShortener.OneLine("uno\r\n  dos\ttres  ", 50));
        Assert.Equal("uno d…", TextShortener.OneLine("uno\ndos tres", 6));
    }

    public void Dispose()
    {
        if (Directory.Exists(_folder))
            Directory.Delete(_folder, recursive: true);
    }
}
