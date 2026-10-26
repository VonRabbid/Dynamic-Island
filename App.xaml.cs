using System;
using System.Windows;
using DynamicIsland.Services;
using DynamicIsland.Views;
using Application = System.Windows.Application;

namespace DynamicIsland;

public partial class App : Application
{
    private TrayService? _trayService;

    public static bool IsShuttingDown { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _trayService = new TrayService();
        _trayService.OnSettingsRequested = () =>
        {
            Dispatcher.InvokeAsync(() =>
            {
                var settingsWin = new SettingsWindow();
                settingsWin.ShowDialog();
            });
        };
        _trayService.OnResetPositionRequested = () =>
        {
            Dispatcher.InvokeAsync(() =>
            {
                if (MainWindow is MainWindow mw)
                {
                    mw.ResetPosition();
                }
            });
        };
        _trayService.OnExitRequested = () =>
        {
            Dispatcher.InvokeAsync(ShutdownApp);
        };
        _trayService.Initialize();
    }

    public void ShutdownApp()
    {
        IsShuttingDown = true;
        _trayService?.Dispose();
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayService?.Dispose();
        base.OnExit(e);
    }
}
