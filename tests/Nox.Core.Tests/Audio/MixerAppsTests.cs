using Nox.Core.Abstractions;
using Nox.Core.Audio;

namespace Nox.Core.Tests.Audio;

public sealed class MixerAppsTests
{
    private static AudioSessionInfo Session(string id, string process, float volume = 1, bool muted = false,
        string display = "", bool system = false, int pid = 1) =>
        new(id, pid, process, display, volume, muted, system);

    [Fact]
    public void SessionsOfTheSameApp_AreJoined()
    {
        var apps = MixerApps.From([Session("a", "chrome", 0.4f), Session("b", "chrome", 0.8f), Session("c", "Spotify")]);

        MixerApp chrome = Assert.Single(apps, a => a.Key == "chrome");
        Assert.Equal(["a", "b"], chrome.SessionIds);
        Assert.Equal(0.8f, chrome.Volume);
        Assert.Equal(2, apps.Count);
    }

    [Fact]
    public void Muted_OnlyIfEverySessionIs()
    {
        var apps = MixerApps.From([Session("a", "chrome", muted: true), Session("b", "chrome", muted: false)]);

        Assert.False(apps[0].IsMuted);
    }

    [Fact]
    public void Nox_IsLeftOut_SystemSoundsGoLast()
    {
        var apps = MixerApps.From([
            Session("s", "", system: true),
            Session("n", "NOX"),
            Session("z", "zoom"),
            Session("a", "audacity"),
        ]);

        Assert.Equal(["Audacity", "Zoom", MixerApps.SystemSoundsName], apps.Select(a => a.Name));
    }

    [Fact]
    public void DisplayName_IsPreferred_ButNotResourceStrings()
    {
        var apps = MixerApps.From([
            Session("a", "chrome", display: "Google Chrome"),
            Session("b", "musicbee", display: "@%SystemRoot%\\algo.dll,-100"),
        ]);

        Assert.Equal(["Google Chrome", "Musicbee"], apps.Select(a => a.Name));
    }

    [Fact]
    public void UnknownProcess_IsGroupedByProcessId()
    {
        var apps = MixerApps.From([Session("a", "", pid: 10), Session("b", "", pid: 11)]);

        Assert.Equal(2, apps.Count);
        Assert.All(apps, a => Assert.Equal("App desconocida", a.Name));
    }

    [Fact]
    public void NextOutput_CyclesFromTheDefault()
    {
        AudioOutput[] outputs = [new("1", "Altavoces", false), new("2", "Auriculares", true), new("3", "HDMI", false)];

        Assert.Equal("3", MixerApps.NextOutput(outputs)!.Id);
        // Desde la última se vuelve a la primera.
        Assert.Equal("1", MixerApps.NextOutput([outputs[0], outputs[1] with { IsDefault = false }, outputs[2] with { IsDefault = true }])!.Id);
        Assert.Null(MixerApps.NextOutput([outputs[0]]));
    }
}
