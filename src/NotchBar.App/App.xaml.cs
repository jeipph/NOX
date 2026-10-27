using System.IO;
using System.Windows;
using H.NotifyIcon;
using Microsoft.Extensions.DependencyInjection;
using NotchBar.App.Services;
using NotchBar.App.ViewModels;
using NotchBar.App.Windows;
using NotchBar.Core.Abstractions;
using NotchBar.Core.Clipboard;
using NotchBar.Core.Lyrics;
using NotchBar.Core.Media;
using NotchBar.Core.Notes;
using NotchBar.Core.Settings;
using NotchBar.Windows.Audio;
using NotchBar.Windows.Clipboard;
using NotchBar.Windows.Lyrics;
using NotchBar.Windows.Media;
using NotchBar.Windows.Shell;

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

        // Un error no previsto en la interfaz se muestra y se apunta, en vez de perderse en silencio.
        DispatcherUnhandledException += (_, args) =>
        {
            args.Handled = true;
            ReportFatal(args.Exception);
        };

        // --fake: canción de mentira para probar la interfaz sin reproductor.
        bool useFakeMedia = e.Args.Contains("--fake", StringComparer.OrdinalIgnoreCase);
        _services = ConfigureServices(useFakeMedia);

        _trayIcon = (TaskbarIcon)FindResource("TrayIcon");
        _trayIcon.DataContext = _services.GetRequiredService<NotchViewModel>();
        _trayIcon.ForceCreate();

        _ = StartAsync(_services);
    }

    // Primero las opciones guardadas (tema incluido, para no ver un parpadeo de colores),
    // luego la ventana y por último la música.
    private async Task StartAsync(IServiceProvider services)
    {
        try
        {
            await services.GetRequiredService<SettingsCoordinator>().LoadAsync();
            services.GetRequiredService<NotchWindow>().Show();
            await services.GetRequiredService<NotchContentViewModel>().StartAsync();
        }
        catch (Exception error)
        {
            ReportFatal(error);
        }
    }

    // Guarda el error en %LOCALAPPDATA%\NotchBar\logs\error.log (sin datos personales: solo el error técnico),
    // lo muestra y cierra la app.
    private void ReportFatal(Exception error)
    {
        Exception cause = error.InnerException ?? error;
        try
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NotchBar", "logs");
            Directory.CreateDirectory(folder);
            File.AppendAllText(Path.Combine(folder, "error.log"), $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}{Environment.NewLine}{error}{Environment.NewLine}{Environment.NewLine}");
        }
        catch (Exception)
        {
        }

        MessageBox.Show($"NotchBar encontró un error y se va a cerrar:\n\n{cause.GetType().Name}: {cause.Message}\n\nDetalles en %LOCALAPPDATA%\\NotchBar\\logs\\error.log",
            "NotchBar", MessageBoxButton.OK, MessageBoxImage.Error);
        Shutdown();
    }

    private static ServiceProvider ConfigureServices(bool useFakeMedia)
    {
        var services = new ServiceCollection();

        // --fake: todo de mentira (canción y letra), sin reproductor ni internet,
        // y con sus propias opciones para no tocar las de verdad.
        if (useFakeMedia)
        {
            services.AddSingleton<IMediaService, FakeMediaService>(_ => new FakeMediaService());
            services.AddSingleton<ILyricsSource, FakeLyricsSource>();
            services.AddSingleton<ILyricsCache, MemoryLyricsCache>();
            services.AddSingleton<ISettingsStore>(_ =>
                new JsonSettingsStore(Path.Combine(Path.GetTempPath(), "NotchBar-fake-settings.json")));
        }
        else
        {
            services.AddSingleton<IMediaService, SmtcMediaService>();
            services.AddSingleton<ILyricsSource, LrclibClient>();
            services.AddSingleton<ILyricsCache>(_ => new FileLyricsCache());
            services.AddSingleton<ISettingsStore>(_ => new JsonSettingsStore());
        }
        services.AddSingleton<ILyricsService, LyricsService>();
        services.AddSingleton<IAutoStart, AutoStart>();
        services.AddSingleton<IVolumeService, NAudioVolumeService>();
        services.AddSingleton<ClipboardMonitor>();
        services.AddSingleton<IClipboardService>(s => s.GetRequiredService<ClipboardMonitor>());
        services.AddSingleton(_ => new PinnedClipsStore());
        services.AddSingleton<NotesStore>();
        services.AddSingleton<ThemeService>();
        services.AddSingleton<SettingsCoordinator>();

        services.AddSingleton<NotchViewModel>();
        services.AddSingleton<NowPlayingViewModel>();
        services.AddSingleton<LyricsViewModel>();
        services.AddSingleton<VolumeViewModel>();
        services.AddSingleton<ClipboardViewModel>();
        services.AddSingleton<NotesViewModel>();
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
