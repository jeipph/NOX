using System.IO;
using System.Windows;
using H.NotifyIcon;
using Microsoft.Extensions.DependencyInjection;
using Nox.App.Services;
using Nox.App.ViewModels;
using Nox.App.Windows;
using Nox.Core.Abstractions;
using Nox.Core.Clipboard;
using Nox.Core.Lyrics;
using Nox.Core.Media;
using Nox.Core.Notes;
using Nox.Core.Settings;
using Nox.Windows.Audio;
using Nox.Windows.Clipboard;
using Nox.Windows.Lyrics;
using Nox.Windows.Media;
using Nox.Windows.Shell;

namespace Nox.App;

public partial class App : Application
{
    // Evita abrir dos NOX a la vez.
    private Mutex? _singleInstance;
    private TaskbarIcon? _trayIcon;
    private ServiceProvider? _services;

    protected override void OnStartup(StartupEventArgs e)
    {
        _singleInstance = new Mutex(true, @"Local\NOX.SingleInstance", out bool isFirst);
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
        if (!useFakeMedia)
            MigrateFromNotchBar();
        _services = ConfigureServices(useFakeMedia);

        _trayIcon = (TaskbarIcon)FindResource("TrayIcon");
        _trayIcon.DataContext = _services.GetRequiredService<NotchViewModel>();
        _trayIcon.ForceCreate();

        _ = StartAsync(_services);
    }

    // La app se llamaba NotchBar: sus carpetas de datos y el "Iniciar con Windows" pasan a NOX (una sola vez).
    private static void MigrateFromNotchBar()
    {
        try
        {
            LegacyData.MoveFolders(LegacyData.DefaultMoves());
            AutoStart.MigrateFromOldName(LegacyData.OldName);
        }
        catch (Exception)
        {
            // Si no se puede, NOX arranca con valores nuevos; los datos viejos siguen donde estaban.
        }
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

    // Guarda el error en %LOCALAPPDATA%\NOX\logs\error.log (sin datos personales: solo el error técnico),
    // lo muestra y cierra la app.
    private void ReportFatal(Exception error)
    {
        Exception cause = error.InnerException ?? error;
        try
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NOX", "logs");
            Directory.CreateDirectory(folder);
            File.AppendAllText(Path.Combine(folder, "error.log"), $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}{Environment.NewLine}{error}{Environment.NewLine}{Environment.NewLine}");
        }
        catch (Exception)
        {
        }

        MessageBox.Show($"NOX encontró un error y se va a cerrar:\n\n{cause.GetType().Name}: {cause.Message}\n\nDetalles en %LOCALAPPDATA%\\NOX\\logs\\error.log",
            "NOX", MessageBoxButton.OK, MessageBoxImage.Error);
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
                new JsonSettingsStore(Path.Combine(Path.GetTempPath(), "NOX-fake-settings.json")));
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
        services.AddSingleton(_ => new FocusViewModel(includeTestPreset: useFakeMedia));
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
