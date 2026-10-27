using Nox.Core.Models;
using Nox.Core.Settings;

namespace Nox.Core.Tests.Settings;

public sealed class JsonSettingsStoreTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "NOXTests", Guid.NewGuid().ToString("N"));

    private string SettingsPath => Path.Combine(_folder, "settings.json");

    [Fact]
    public void NoFile_GivesDefaults()
    {
        var settings = new JsonSettingsStore(SettingsPath).Load();

        Assert.Equal(new AppSettings(), settings);
        Assert.Equal(AppTheme.System, settings.Theme);
        Assert.True(settings.LyricsEnabled);
    }

    [Fact]
    public void SavedSettings_AreLoadedBack()
    {
        var store = new JsonSettingsStore(SettingsPath);
        var saved = new AppSettings
        {
            Theme = AppTheme.Light,
            Style = NotchStyle.Capsule,
            OnlyOnHover = true,
            ReserveTopStrip = true,
            LyricsEnabled = false,
            PreferredSourceId = "Chrome",
        };

        store.Save(saved);

        Assert.Equal(saved, new JsonSettingsStore(SettingsPath).Load());
    }

    [Fact]
    public void File_IsReadableText()
    {
        new JsonSettingsStore(SettingsPath).Save(new AppSettings { Theme = AppTheme.Dark });

        Assert.Contains("\"Theme\": \"Dark\"", File.ReadAllText(SettingsPath));
    }

    [Fact]
    public void DamagedFile_GivesDefaults_AndIsKeptAside()
    {
        Directory.CreateDirectory(_folder);
        File.WriteAllText(SettingsPath, "{ esto no es JSON");

        var settings = new JsonSettingsStore(SettingsPath).Load();

        Assert.Equal(new AppSettings(), settings);
        Assert.True(File.Exists(SettingsPath + ".bad"));
    }

    [Fact]
    public void MissingFields_KeepTheirDefaults()
    {
        Directory.CreateDirectory(_folder);
        File.WriteAllText(SettingsPath, """{ "Theme": "Light" }""");

        var settings = new JsonSettingsStore(SettingsPath).Load();

        Assert.Equal(AppTheme.Light, settings.Theme);
        Assert.True(settings.LyricsEnabled);
    }

    public void Dispose()
    {
        if (Directory.Exists(_folder))
            Directory.Delete(_folder, recursive: true);
    }
}
