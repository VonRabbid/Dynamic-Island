using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicIsland.Models;
using DynamicIsland.Services;

namespace DynamicIsland.ViewModels;

public enum IslandState
{
    Idle,
    MusicPlaying,
    Expanded,
    Notification
}

public partial class MainViewModel : ObservableObject
{
    private DispatcherTimer? _notificationTimer;

    [ObservableProperty]
    private IslandState _currentState = IslandState.Idle;

    [ObservableProperty]
    private NotificationModel? _currentNotification;

    [ObservableProperty]
    private bool _isNotificationActive;

    [ObservableProperty]
    private bool _isHovered;

    [ObservableProperty]
    private bool _isPinned;

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

        Media.PropertyChanged += OnMediaPropertyChanged;
        Notifications.OnNotificationReceived += OnNotificationReceived;

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
        _notificationTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(5)
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

    partial void OnIsPinnedChanged(bool value) => UpdateState();

    public void UpdateState()
    {
        if (IsNotificationActive && CurrentNotification != null)
        {
            CurrentState = IslandState.Notification;
        }
        else if (IsHovered || IsPinned)
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
        IsPinned = !IsPinned;
    }

    [RelayCommand]
    private void SetHovered(bool hovered)
    {
        IsHovered = hovered;
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
