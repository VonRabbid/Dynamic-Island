using System;
using System.Windows;
using System.Windows.Media;
using DynamicIsland.Services;
using DynamicIsland.Views;
using Application = System.Windows.Application;

namespace DynamicIsland;

public partial class App : Application, IDisposable
{
    private TrayService? _trayService;

    public static bool IsShuttingDown { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Aceleración por hardware completa en pipeline de renderizado D3D de WPF
        RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.Default;

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
        KeyboardHookService.Instance.Dispose();
        PowerStatusService.Instance.Dispose();
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Dispose();
        base.OnExit(e);
    }

    public void Dispose()
    {
        _trayService?.Dispose();
        KeyboardHookService.Instance.Dispose();
        PowerStatusService.Instance.Dispose();
        GC.SuppressFinalize(this);
    }
}
