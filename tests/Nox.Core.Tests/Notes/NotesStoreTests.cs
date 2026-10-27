using Nox.Core.Notes;

namespace Nox.Core.Tests.Notes;

public sealed class NotesStoreTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 10, 27, 14, 30, 5);
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "NOXTests", Guid.NewGuid().ToString("N"));

    private NotesStore NewStore() => new() { Folder = _folder };

    [Fact]
    public void Save_CreatesDatedMarkdownFile()
    {
        string path = NewStore().Save("Comprar pan", Now);

        Assert.Equal("2026-10-27_143005.md", Path.GetFileName(path));
        Assert.Equal("Comprar pan", File.ReadAllText(path).Trim());
    }

    [Fact]
    public void Save_SameSecond_DoesNotOverwrite()
    {
        var store = NewStore();

        string first = store.Save("una", Now);
        string second = store.Save("otra", Now);

        Assert.NotEqual(first, second);
        Assert.EndsWith("-2.md", second);
        Assert.Equal("una", File.ReadAllText(first).Trim());
    }

    [Fact]
    public void Save_LeavesNoTemporaryFile()
    {
        NewStore().Save("nota", Now);

        Assert.Empty(Directory.GetFiles(_folder, "*.tmp"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  \r\n ")]
    public void Save_EmptyNote_IsRejected(string text)
    {
        Assert.Throws<ArgumentException>(() => NewStore().Save(text, Now));
    }

    [Fact]
    public void Recent_NewestFirst_LimitedAndWithPreview()
    {
        var store = NewStore();
        for (int i = 1; i <= 12; i++)
        {
            string path = store.Save($"# Nota {i}\nmás texto", Now.AddMinutes(i));
            File.SetLastWriteTime(path, Now.AddMinutes(i));
        }

        var recent = store.Recent(10);

        Assert.Equal(10, recent.Count);
        Assert.Equal("Nota 12", recent[0].Preview);
        Assert.Equal("Nota 3", recent[^1].Preview);
    }

    [Fact]
    public void Recent_MissingFolder_IsEmpty()
    {
        Assert.Empty(NewStore().Recent());
    }

    public void Dispose()
    {
        if (Directory.Exists(_folder))
            Directory.Delete(_folder, recursive: true);
    }
}
