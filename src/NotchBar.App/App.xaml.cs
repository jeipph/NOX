using System.Windows;
using H.NotifyIcon;
using Microsoft.Extensions.DependencyInjection;
using NotchBar.App.ViewModels;
using NotchBar.App.Windows;
using NotchBar.Core.Abstractions;
using NotchBar.Core.Lyrics;
using NotchBar.Core.Media;
using NotchBar.Windows.Lyrics;
using NotchBar.Windows.Media;

namespace NotchBar.App;

public partial class App : Application
{
    // Evita abrir dos NotchBar a la vez.
    private Mutex? _singleInstance;
    private TaskbarIcon? _trayIcon;
    private ServiceProvider? _services;

    protected override void OnStartup(StartupEventArgs e)
    {
        _singleInstance = new Mutex(true, @"Local\NotchBar.SingleInstance", out bool isFirst);
        if (!isFirst)
        {
            Shutdown();
            return;
        }

        base.OnStartup(e);

        // --fake: canción de mentira para probar la interfaz sin reproductor.
        bool useFakeMedia = e.Args.Contains("--fake", StringComparer.OrdinalIgnoreCase);
        _services = ConfigureServices(useFakeMedia);

        _trayIcon = (TaskbarIcon)FindResource("TrayIcon");
        _trayIcon.DataContext = _services.GetRequiredService<NotchViewModel>();
        _trayIcon.ForceCreate();

        _services.GetRequiredService<NotchWindow>().Show();
        _ = _services.GetRequiredService<NotchContentViewModel>().StartAsync();
    }

    private static ServiceProvider ConfigureServices(bool useFakeMedia)
    {
        var services = new ServiceCollection();

        // --fake: todo de mentira (canción y letra), sin reproductor ni internet.
        if (useFakeMedia)
        {
            services.AddSingleton<IMediaService, FakeMediaService>(_ => new FakeMediaService());
            services.AddSingleton<ILyricsSource, FakeLyricsSource>();
            services.AddSingleton<ILyricsCache, MemoryLyricsCache>();
        }
        else
        {
            services.AddSingleton<IMediaService, SmtcMediaService>();
            services.AddSingleton<ILyricsSource, LrclibClient>();
            services.AddSingleton<ILyricsCache>(_ => new FileLyricsCache());
        }
        services.AddSingleton<ILyricsService, LyricsService>();

        services.AddSingleton<NotchViewModel>();
        services.AddSingleton<NowPlayingViewModel>();
        services.AddSingleton<LyricsViewModel>();
        services.AddSingleton<NotchContentViewModel>();
        services.AddSingleton<NotchWindow>();
        return services.BuildServiceProvider();
    }

    private void OnExitClick(object sender, RoutedEventArgs e) => Shutdown();

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIcon?.Dispose();
        _services?.Dispose();
        _singleInstance?.Dispose();
        base.OnExit(e);
    }
}
