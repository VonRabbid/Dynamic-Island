using System;
using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace DynamicIsland.Services;

public class KeyboardHookService : IDisposable
{
    private const int VK_CAPITAL = 0x14; // Tecla Bloq Mayús (Caps Lock)

    [DllImport("user32.dll")]
    private static extern short GetKeyState(int nVirtKey);

    private readonly DispatcherTimer _pollTimer;
    private bool _lastState;

    public static KeyboardHookService Instance { get; } = new();

    public event EventHandler<bool>? CapsLockChanged;

    public KeyboardHookService()
    {
        _lastState = IsCapsLockActive();

        _pollTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(150)
        };
        _pollTimer.Tick += OnPollTick;
        Start();
    }

    public void Start()
    {
        if (!_pollTimer.IsEnabled)
        {
            _lastState = IsCapsLockActive();
            _pollTimer.Start();
        }
    }

    public void Stop()
    {
        if (_pollTimer.IsEnabled)
        {
            _pollTimer.Stop();
        }
    }

    private void OnPollTick(object? sender, EventArgs e)
    {
        if (!SettingsService.Instance.CurrentSettings.EnableCapsLockAlert)
            return;

        bool currentState = IsCapsLockActive();
        if (currentState != _lastState)
        {
            _lastState = currentState;
            CapsLockChanged?.Invoke(this, currentState);
        }
    }

    private static bool IsCapsLockActive()
    {
        return (GetKeyState(VK_CAPITAL) & 0x0001) != 0;
    }

    public void TriggerTestAlert(bool isCapsLock)
    {
        _lastState = isCapsLock;
        CapsLockChanged?.Invoke(this, isCapsLock);
    }

    public void Dispose()
    {
        _pollTimer.Stop();
        GC.SuppressFinalize(this);
    }
}
