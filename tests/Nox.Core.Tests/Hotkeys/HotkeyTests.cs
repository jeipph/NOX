using Nox.Core.Hotkeys;

namespace Nox.Core.Tests.Hotkeys;

public sealed class HotkeyTests
{
    [Theory]
    [InlineData("Ctrl+Alt+N", HotkeyModifiers.Ctrl | HotkeyModifiers.Alt, 'N')]
    [InlineData("ctrl + alt + n", HotkeyModifiers.Ctrl | HotkeyModifiers.Alt, 'N')]
    [InlineData("Ctrl+Mayús+F5", HotkeyModifiers.Ctrl | HotkeyModifiers.Shift, 0x74)]
    [InlineData("Shift+Win+1", HotkeyModifiers.Shift | HotkeyModifiers.Win, '1')]
    [InlineData("Ctrl+Alt+Espacio", HotkeyModifiers.Ctrl | HotkeyModifiers.Alt, 0x20)]
    [InlineData("Ctrl+Alt+,", HotkeyModifiers.Ctrl | HotkeyModifiers.Alt, 0xBC)]
    [InlineData("Ctrl+Alt++", HotkeyModifiers.Ctrl | HotkeyModifiers.Alt, 0xBB)]
    public void Parse_ReadsModifiersAndKey(string text, HotkeyModifiers modifiers, int key) =>
        Assert.Equal(new Hotkey(modifiers, key), Hotkey.Parse(text));

    [Theory]
    [InlineData("")]
    [InlineData("N")]              // sin modificador
    [InlineData("Mayús+N")]        // solo Mayús taparía la N mayúscula al escribir
    [InlineData("Ctrl+Alt+Ñandú")] // tecla desconocida
    [InlineData("Hiper+N")]        // modificador desconocido
    [InlineData(null)]
    public void Parse_RejectsInvalid(string? text) => Assert.Null(Hotkey.Parse(text));

    [Theory]
    [InlineData("ctrl+alt+n", "Ctrl+Alt+N")]
    [InlineData("Shift+Ctrl+space", "Ctrl+Mayús+Espacio")] // orden fijo: Ctrl, Alt, Mayús, Win
    [InlineData("Win+Alt+Left", "Alt+Win+Izquierda")]
    public void ToString_IsNormalized(string text, string expected) =>
        Assert.Equal(expected, Hotkey.Parse(text).ToString());

    [Fact]
    public void Defaults_AreAllUsable()
    {
        var (usable, problems) = new HotkeySettings().Check();

        Assert.Empty(problems);
        Assert.Equal(HotkeySettings.Actions.Count, usable.Count);
    }

    [Fact]
    public void Repeated_IsReported_FirstOneWins()
    {
        var settings = new HotkeySettings().With(HotkeyAction.LyricsReset, "ctrl+alt+n");

        var (usable, problems) = settings.Check();

        Assert.True(usable.ContainsKey(HotkeyAction.ToggleNotch));
        Assert.False(usable.ContainsKey(HotkeyAction.LyricsReset));
        Assert.Contains("Abrir o cerrar el notch", problems[HotkeyAction.LyricsReset]);
    }

    [Fact]
    public void Empty_MeansNoHotkey_NotAProblem()
    {
        var (usable, problems) = new HotkeySettings().With(HotkeyAction.PlayPause, "").Check();

        Assert.False(usable.ContainsKey(HotkeyAction.PlayPause));
        Assert.Empty(problems);
    }

    [Fact]
    public void Unreadable_IsReported() =>
        Assert.True(new HotkeySettings().With(HotkeyAction.PlayPause, "P").Check().Problems.ContainsKey(HotkeyAction.PlayPause));

    [Fact]
    public void With_ChangesOnlyThatAction()
    {
        var changed = new HotkeySettings().With(HotkeyAction.LyricsLater, "Ctrl+Alt+L");

        Assert.Equal("Ctrl+Alt+L", changed.Get(HotkeyAction.LyricsLater));
        Assert.Equal(new HotkeySettings().ToggleNotch, changed.Get(HotkeyAction.ToggleNotch));
    }
}
