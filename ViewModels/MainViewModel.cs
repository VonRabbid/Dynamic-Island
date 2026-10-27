using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicIsland.Models;
using DynamicIsland.Services;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;

namespace DynamicIsland.ViewModels;

public enum IslandState
{
    Idle,
    MusicPlaying,
    Expanded,
    Notification,
    PowerAlert,
    CapsLockAlert
}

public partial class MainViewModel : ObservableObject
{
    private DispatcherTimer? _notificationTimer;
    private DispatcherTimer? _powerAlertTimer;
    private DispatcherTimer? _capsLockTimer;

    [ObservableProperty]
    private IslandState _currentState = IslandState.Idle;

    [ObservableProperty]
    private NotificationModel? _currentNotification;

    [ObservableProperty]
    private bool _isNotificationActive;

    [ObservableProperty]
    private PowerAlertInfo? _currentPowerAlert;

    [ObservableProperty]
    private bool _isPowerAlertActive;

    [ObservableProperty]
    private bool _isCapsLockAlertActive;

    [ObservableProperty]
    private bool _isCapsLockOn;

    [ObservableProperty]
    private bool _isHovered;

    [ObservableProperty]
    private Brush _islandBackgroundBrush = new SolidColorBrush(Color.FromArgb(0xD9, 0x0D, 0x0D, 0x12));

    public MediaService Media { get; }
    public NotificationService Notifications { get; }

    public event EventHandler? NotificationPulsed;

    public MainViewModel() : this(MediaService.Instance, NotificationService.Instance)
    {
    }

    public MainViewModel(MediaService mediaService, NotificationService notificationService)
    {
        Media = mediaService;
        Notifications = notificationService;

        UpdateOpacity(SettingsService.Instance.CurrentSettings.IslandOpacity);

        Media.PropertyChanged += OnMediaPropertyChanged;
        Notifications.OnNotificationReceived += OnNotificationReceived;

        PowerStatusService.Instance.PowerAlertTriggered += OnPowerAlertTriggered;
        KeyboardHookService.Instance.CapsLockChanged += OnCapsLockChanged;

        UpdateState();
    }

    private void OnMediaPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(Media.IsPlaying) or nameof(Media.HasMedia) or nameof(Media.Title))
        {
            App.Current?.Dispatcher?.InvokeAsync(UpdateState);
        }
    }

    private void OnNotificationReceived(object? sender, NotificationModel notification)
    {
        App.Current?.Dispatcher?.InvokeAsync(() => HandleIncomingNotification(notification));
    }

    private void OnPowerAlertTriggered(object? sender, PowerAlertInfo alert)
    {
        App.Current?.Dispatcher?.InvokeAsync(() =>
        {
            CurrentPowerAlert = alert;
            IsPowerAlertActive = true;

            _powerAlertTimer?.Stop();
            _powerAlertTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3)
            };
            _powerAlertTimer.Tick += (s, e) =>
            {
                _powerAlertTimer?.Stop();
                IsPowerAlertActive = false;
                CurrentPowerAlert = null;
                UpdateState();
            };
            _powerAlertTimer.Start();

            UpdateState();
        });
    }

    private void OnCapsLockChanged(object? sender, bool isCapsLock)
    {
        App.Current?.Dispatcher?.InvokeAsync(() =>
        {
            IsCapsLockOn = isCapsLock;
            IsCapsLockAlertActive = true;

            _capsLockTimer?.Stop();
            _capsLockTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(1500)
            };
            _capsLockTimer.Tick += (s, e) =>
            {
                _capsLockTimer?.Stop();
                IsCapsLockAlertActive = false;
                UpdateState();
            };
            _capsLockTimer.Start();

            UpdateState();
        });
    }

    public void HandleIncomingNotification(NotificationModel notification)
    {
        bool wasAlreadyNotification = IsNotificationActive;
        CurrentNotification = notification;
        IsNotificationActive = true;

        if (wasAlreadyNotification)
        {
            NotificationPulsed?.Invoke(this, EventArgs.Empty);
        }

        _notificationTimer?.Stop();
        int duration = SettingsService.Instance.CurrentSettings.NotificationDuration;
        if (duration < 2) duration = 5;

        _notificationTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(duration)
        };
        _notificationTimer.Tick += (s, e) =>
        {
            _notificationTimer?.Stop();
            DismissNotification();
        };
        _notificationTimer.Start();

        UpdateState();
    }

    [RelayCommand]
    public void DismissNotification()
    {
        _notificationTimer?.Stop();
        IsNotificationActive = false;
        CurrentNotification = null;
        UpdateState();
    }

    partial void OnIsHoveredChanged(bool value) => UpdateState();

    public void UpdateState()
    {
        // Regla de prioridad: Notificación externa > PowerAlert > CapsLockAlert > Expanded > MusicPlaying > Idle
        if (IsNotificationActive && CurrentNotification != null)
        {
            CurrentState = IslandState.Notification;
        }
        else if (IsPowerAlertActive && CurrentPowerAlert != null)
        {
            CurrentState = IslandState.PowerAlert;
        }
        else if (IsCapsLockAlertActive)
        {
            CurrentState = IslandState.CapsLockAlert;
        }
        else if (IsHovered && Media.HasMedia)
        {
            CurrentState = IslandState.Expanded;
        }
        else if (Media.HasMedia && (Media.IsPlaying || !string.IsNullOrWhiteSpace(Media.Title)))
        {
            CurrentState = IslandState.MusicPlaying;
        }
        else
        {
            CurrentState = IslandState.Idle;
        }
    }

    [RelayCommand]
    private void ToggleExpand()
    {
        if (IsNotificationActive)
        {
            DismissNotification();
            return;
        }
        if (IsPowerAlertActive)
        {
            _powerAlertTimer?.Stop();
            IsPowerAlertActive = false;
            CurrentPowerAlert = null;
            UpdateState();
            return;
        }
        if (IsCapsLockAlertActive)
        {
            _capsLockTimer?.Stop();
            IsCapsLockAlertActive = false;
            UpdateState();
            return;
        }
    }

    [RelayCommand]
    private void SetHovered(bool hovered)
    {
        IsHovered = hovered;
    }

    public void UpdateOpacity(double opacity)
    {
        if (double.IsNaN(opacity) || opacity < 0.40 || opacity > 1.00)
            opacity = 0.85;

        byte alpha = (byte)Math.Clamp((int)(opacity * 255.0), 102, 255);
        var brush = new SolidColorBrush(Color.FromArgb(alpha, 0x0D, 0x0D, 0x12));
        brush.Freeze();
        IslandBackgroundBrush = brush;
    }

    [RelayCommand]
    private async Task PlayPauseAsync()
    {
        await Media.PlayPauseAsync();
    }

    [RelayCommand]
    private async Task NextAsync()
    {
        await Media.NextAsync();
    }

    [RelayCommand]
    private async Task PreviousAsync()
    {
        await Media.PreviousAsync();
    }
}
