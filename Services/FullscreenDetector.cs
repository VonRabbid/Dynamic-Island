using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Threading;

namespace DynamicIsland.Services;

public class FullscreenDetector : IDisposable
{
    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    private const uint MONITOR_DEFAULTTONEAREST = 2;

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern IntPtr GetShellWindow();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    private readonly DispatcherTimer _checkTimer;
    private bool _lastIsFullscreen;
    private IntPtr _windowHandle = IntPtr.Zero;

    public event EventHandler<bool>? FullscreenChanged;

    public bool IsFullscreen => _lastIsFullscreen;

    public FullscreenDetector()
    {
        _checkTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(600)
        };
        _checkTimer.Tick += OnCheckTimerTick;
    }

    public void SetWindowHandle(IntPtr handle)
    {
        _windowHandle = handle;
    }

    public void Start()
    {
        _checkTimer.Start();
    }

    public void Stop()
    {
        _checkTimer.Stop();
    }

    private void OnCheckTimerTick(object? sender, EventArgs e)
    {
        bool isFullscreen = CheckIsForegroundFullscreen();
        if (isFullscreen != _lastIsFullscreen)
        {
            _lastIsFullscreen = isFullscreen;
            FullscreenChanged?.Invoke(this, isFullscreen);
        }
    }

    private bool CheckIsForegroundFullscreen()
    {
        var fgWnd = GetForegroundWindow();
        if (fgWnd == IntPtr.Zero || fgWnd == GetShellWindow() || fgWnd == _windowHandle)
            return false;

        var sb = new StringBuilder(256);
        GetClassName(fgWnd, sb, sb.Capacity);
        string className = sb.ToString();

        // Excluir ventanas del sistema, escritorio o barra de tareas
        if (className is "Progman" or "WorkerW" or "Shell_TrayWnd" or "Shell_SecondaryTrayWnd"
            or "Windows.UI.Core.CoreWindow" or "DV2ControlHost")
        {
            return false;
        }

        if (!GetWindowRect(fgWnd, out var rect))
            return false;

        // Comprobar si la ventana tiene tamaño válido
        if (rect.Right <= rect.Left || rect.Bottom <= rect.Top)
            return false;

        var hMonitor = MonitorFromWindow(fgWnd, MONITOR_DEFAULTTONEAREST);
        if (hMonitor == IntPtr.Zero)
            return false;

        // Si tenemos el handle de nuestra ventana, sólo ocultar si la app en pantalla completa
        // está en el mismo monitor que la Dynamic Island
        if (_windowHandle != IntPtr.Zero)
        {
            var myMonitor = MonitorFromWindow(_windowHandle, MONITOR_DEFAULTTONEAREST);
            if (myMonitor != IntPtr.Zero && hMonitor != myMonitor)
            {
                return false;
            }
        }

        var mi = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
        if (!GetMonitorInfo(hMonitor, ref mi))
            return false;

        // Verifica si la ventana cubre completamente o excede el monitor
        return rect.Left <= mi.rcMonitor.Left &&
               rect.Top <= mi.rcMonitor.Top &&
               rect.Right >= mi.rcMonitor.Right &&
               rect.Bottom >= mi.rcMonitor.Bottom;
    }

    public void Dispose()
    {
        _checkTimer.Stop();
    }
}
