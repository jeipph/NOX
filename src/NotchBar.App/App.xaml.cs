using System.Windows;
using H.NotifyIcon;
using Microsoft.Extensions.DependencyInjection;
using NotchBar.App.Windows;

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

        _services = ConfigureServices();

        _trayIcon = (TaskbarIcon)FindResource("TrayIcon");
        _trayIcon.ForceCreate();

        _services.GetRequiredService<NotchWindow>().Show();
    }

    private static ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();
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
