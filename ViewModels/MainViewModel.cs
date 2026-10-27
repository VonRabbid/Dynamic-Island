using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicIsland.Helpers;
using DynamicIsland.Models;
using DynamicIsland.Services;
using BitmapSource = System.Windows.Media.Imaging.BitmapSource;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;

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
    private Brush _islandBackgroundBrush = new SolidColorBrush(Color.FromArgb(0xB8, 0x0E, 0x0E, 0x14));

    [ObservableProperty]
    private Brush _accentBrush = new SolidColorBrush(Color.FromRgb(0x38, 0xBD, 0xF8));

    [ObservableProperty]
    private Color _backgroundStartColor = Color.FromArgb(0x35, 0x38, 0xBD, 0xF8);

    [ObservableProperty]
    private Color _backgroundEndColor = Color.FromArgb(0xB8, 0x0E, 0x0E, 0x14);

    public event EventHandler? ColorsChanged;

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

        if (e.PropertyName is nameof(Media.Thumbnail) or nameof(Media.HasMedia))
        {
            App.Current?.Dispatcher?.InvokeAsync(UpdateExtractedColors);
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

    public void UpdateExtractedColors()
    {
        double opacity = SettingsService.Instance.CurrentSettings.IslandOpacity;
        ExtractedColors colors;
        if (Media.HasMedia && Media.Thumbnail is BitmapSource bmp)
        {
            colors = ColorExtractor.ExtractFromBitmap(bmp, opacity);
        }
        else
        {
            colors = ColorExtractor.GetDefaultColors(opacity);
        }

        AccentBrush = colors.AccentBrush;
        BackgroundStartColor = colors.BackgroundStartColor;
        BackgroundEndColor = colors.BackgroundEndColor;
        IslandBackgroundBrush = colors.DynamicBackgroundBrush;

        ColorsChanged?.Invoke(this, EventArgs.Empty);
    }

    public void UpdateOpacity(double opacity)
    {
        UpdateExtractedColors();
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
