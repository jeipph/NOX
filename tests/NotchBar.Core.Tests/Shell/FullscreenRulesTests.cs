using NotchBar.Core.Shell;

namespace NotchBar.Core.Tests.Shell;

public class FullscreenRulesTests
{
    private static readonly PixelRect Monitor = new(0, 0, 1920, 1080);

    [Fact]
    public void WindowExactlyCoveringMonitor_IsFullscreen()
    {
        Assert.True(FullscreenRules.IsFullscreen(Monitor, Monitor, "Chrome_WidgetWin_1"));
    }

    [Fact]
    public void WindowLargerThanMonitor_IsFullscreen()
    {
        var window = new PixelRect(-8, -8, 1928, 1088);
        Assert.True(FullscreenRules.IsFullscreen(window, Monitor, "UnityWndClass"));
    }

    [Fact]
    public void MaximizedWindowAboveTaskbar_IsNotFullscreen()
    {
        var window = new PixelRect(-8, -8, 1928, 1040);
        Assert.False(FullscreenRules.IsFullscreen(window, Monitor, "Chrome_WidgetWin_1"));
    }

    [Fact]
    public void SmallWindow_IsNotFullscreen()
    {
        var window = new PixelRect(100, 100, 900, 700);
        Assert.False(FullscreenRules.IsFullscreen(window, Monitor, "Notepad"));
    }

    [Theory]
    [InlineData("Progman")]
    [InlineData("WorkerW")]
    [InlineData("Shell_TrayWnd")]
    public void DesktopOrTaskbar_IsNeverFullscreen(string windowClass)
    {
        Assert.False(FullscreenRules.IsFullscreen(Monitor, Monitor, windowClass));
    }

    [Fact]
    public void PixelRect_ComputesSize()
    {
        var rect = new PixelRect(10, 20, 110, 70);
        Assert.Equal(100, rect.Width);
        Assert.Equal(50, rect.Height);
    }
}
