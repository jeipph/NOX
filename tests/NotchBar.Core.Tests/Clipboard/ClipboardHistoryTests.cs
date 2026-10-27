using NotchBar.Core.Clipboard;

namespace NotchBar.Core.Tests.Clipboard;

public class ClipboardHistoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 27, 12, 0, 0, TimeSpan.Zero);

    private static string[] Texts(ClipboardHistory history) => history.Items.Select(e => e.Text).ToArray();

    [Fact]
    public void NewestCopy_GoesOnTop()
    {
        var history = new ClipboardHistory();
        history.Add("uno", Now);
        history.Add("dos", Now);

        Assert.Equal(["dos", "uno"], Texts(history));
    }

    [Fact]
    public void SameTextTwiceInARow_IsIgnored()
    {
        var history = new ClipboardHistory();
        history.Add("uno", Now);

        Assert.False(history.Add("uno", Now));
        Assert.Single(history.Items);
    }

    [Fact]
    public void OlderTextCopiedAgain_MovesToTop_WithoutDuplicate()
    {
        var history = new ClipboardHistory();
        history.Add("uno", Now);
        history.Add("dos", Now);
        history.Add("uno", Now);

        Assert.Equal(["uno", "dos"], Texts(history));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\r\n")]
    public void EmptyText_IsIgnored(string text)
    {
        var history = new ClipboardHistory();

        Assert.False(history.Add(text, Now));
        Assert.Empty(history.Items);
    }

    [Fact]
    public void KeepsAtMostFiftyRecent_DroppingTheOldest()
    {
        var history = new ClipboardHistory();
        for (int i = 1; i <= 60; i++)
            history.Add($"copia {i}", Now);

        Assert.Equal(ClipboardHistory.MaxRecent, history.Items.Count);
        Assert.Equal("copia 60", history.Items[0].Text);
        Assert.Equal("copia 11", history.Items[^1].Text);
    }

    [Fact]
    public void Pinned_GoFirst_AndSurviveTheLimitAndClearing()
    {
        var history = new ClipboardHistory();
        history.Add("importante", Now);
        history.Pin("importante");
        for (int i = 1; i <= 60; i++)
            history.Add($"copia {i}", Now);

        history.ClearRecent();

        Assert.Equal(["importante"], Texts(history));
        Assert.True(history.Items[0].IsPinned);
    }

    [Fact]
    public void CopyingAPinnedText_DoesNotDuplicateIt()
    {
        var history = new ClipboardHistory();
        history.Add("importante", Now);
        history.Pin("importante");

        Assert.False(history.Add("importante", Now));
        Assert.Single(history.Items);
    }

    [Fact]
    public void Unpin_MovesBackToRecent()
    {
        var history = new ClipboardHistory();
        history.Add("a", Now);
        history.Pin("a");
        history.Add("b", Now);

        history.Unpin("a");

        Assert.Equal(["a", "b"], Texts(history));
        Assert.False(history.Items[0].IsPinned);
    }

    [Fact]
    public void PinAndUnpin_RaisePinnedChanged()
    {
        var history = new ClipboardHistory();
        int changes = 0;
        history.PinnedChanged += () => changes++;
        history.Add("a", Now);

        history.Pin("a");
        history.Unpin("a");

        Assert.Equal(2, changes);
    }

    [Fact]
    public void LoadPinned_SkipsEmptyAndRepeated()
    {
        var history = new ClipboardHistory();

        history.LoadPinned(["a", "", "a", "b"], Now);

        Assert.Equal(["a", "b"], Texts(history));
        Assert.All(history.Items, e => Assert.True(e.IsPinned));
    }
}
