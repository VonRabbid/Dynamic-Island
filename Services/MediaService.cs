using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using DynamicIsland.Services.Media;
using Windows.Media.Control;
using Brush = System.Windows.Media.Brush;

namespace DynamicIsland.Services;

public partial class MediaService : ObservableObject, IDisposable
{
    private readonly SmtcSessionManager _sessionManager;
    private readonly TimelineTracker _timelineTracker;
    private bool _isDisposed;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _artist = string.Empty;

    [ObservableProperty]
    private bool _isPlaying;

    [ObservableProperty]
    private ImageSource? _thumbnail;

    [ObservableProperty]
    private bool _hasMedia;

    [ObservableProperty]
    private TimeSpan _position = TimeSpan.Zero;

    [ObservableProperty]
    private TimeSpan _duration = TimeSpan.Zero;

    [ObservableProperty]
    private double _progressPercentage;

    [ObservableProperty]
    private bool _isSeeking;

    [ObservableProperty]
    private string _positionText = "0:00";

    [ObservableProperty]
    private string _durationText = "0:00";

    [ObservableProperty]
    private string _sourceAppKind = "Generic";

    [ObservableProperty]
    private string _sourceAppName = string.Empty;

    [ObservableProperty]
    private string _sourceAppIconGeometry = string.Empty;

    [ObservableProperty]
    private Brush _sourceAppBrush;

    [ObservableProperty]
    private bool _isDedicatedMusicPlayer;

    [ObservableProperty]
    private bool _isShuffleActive;

    [ObservableProperty]
    private bool _isRepeatActive;

    public static MediaService Instance { get; } = new();

    public MediaService()
    {
        _sourceAppBrush = SourceAppInfo.CreateDefault().Brush;

        _timelineTracker = new TimelineTracker();
        _timelineTracker.PropertyChanged += OnTimelineTrackerPropertyChanged;

        _sessionManager = new SmtcSessionManager();
        _sessionManager.SessionChanged += OnSessionChanged;
        _sessionManager.PlaybackInfoChanged += OnPlaybackInfoChanged;
        _sessionManager.TimelineChanged += OnTimelineChanged;
        _sessionManager.MediaPropertiesChanged += OnMediaPropertiesChanged;

        _ = InitializeAsync();
    }

    public async Task InitializeAsync()
    {
        await _sessionManager.InitializeAsync();
    }

    private void OnTimelineTrackerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(TimelineTracker.Position):
                Position = _timelineTracker.Position;
                break;
            case nameof(TimelineTracker.Duration):
                Duration = _timelineTracker.Duration;
                break;
            case nameof(TimelineTracker.ProgressPercentage):
                ProgressPercentage = _timelineTracker.ProgressPercentage;
                break;
            case nameof(TimelineTracker.PositionText):
                PositionText = _timelineTracker.PositionText;
                break;
            case nameof(TimelineTracker.DurationText):
                DurationText = _timelineTracker.DurationText;
                break;
            case nameof(TimelineTracker.IsSeeking):
                if (IsSeeking != _timelineTracker.IsSeeking)
                {
                    IsSeeking = _timelineTracker.IsSeeking;
                }
                break;
        }
    }

    partial void OnIsSeekingChanged(bool value)
    {
        if (_timelineTracker.IsSeeking != value)
        {
            _timelineTracker.IsSeeking = value;
        }
    }

    private void OnSessionChanged()
    {
        App.Current?.Dispatcher?.InvokeAsync(async () =>
        {
            var session = _sessionManager.CurrentSession;
            if (session != null)
            {
                DetectSourceApp(session.SourceAppUserModelId);
                UpdatePlaybackInfo(session.GetPlaybackInfo());
                _timelineTracker.UpdateTimeline(session.GetTimelineProperties());
                var props = await session.TryGetMediaPropertiesAsync();
                await UpdateMediaPropertiesAsync(props);
            }
            else
            {
                ClearMediaInfo();
            }
        });
    }

    private void OnPlaybackInfoChanged(GlobalSystemMediaTransportControlsSessionPlaybackInfo? playbackInfo)
    {
        App.Current?.Dispatcher?.InvokeAsync(() => UpdatePlaybackInfo(playbackInfo));
    }

    private void OnTimelineChanged(GlobalSystemMediaTransportControlsSessionTimelineProperties? timeline)
    {
        App.Current?.Dispatcher?.InvokeAsync(() => _timelineTracker.UpdateTimeline(timeline));
    }

    private void OnMediaPropertiesChanged(GlobalSystemMediaTransportControlsSessionMediaProperties? props)
    {
        App.Current?.Dispatcher?.InvokeAsync(async () =>
        {
            var session = _sessionManager.CurrentSession;
            DetectSourceApp(session?.SourceAppUserModelId);
            await UpdateMediaPropertiesAsync(props);
        });
    }

    private void UpdatePlaybackInfo(GlobalSystemMediaTransportControlsSessionPlaybackInfo? playbackInfo)
    {
        if (playbackInfo == null)
        {
            IsPlaying = false;
            IsShuffleActive = false;
            IsRepeatActive = false;
            _timelineTracker.OnPlaybackStateChanged(false);
            return;
        }

        IsPlaying = playbackInfo.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
        IsShuffleActive = playbackInfo.IsShuffleActive == true;
        IsRepeatActive = playbackInfo.AutoRepeatMode.HasValue && playbackInfo.AutoRepeatMode.Value != Windows.Media.MediaPlaybackAutoRepeatMode.None;

        _timelineTracker.OnPlaybackStateChanged(IsPlaying);
    }

    private async Task UpdateMediaPropertiesAsync(GlobalSystemMediaTransportControlsSessionMediaProperties? props)
    {
        if (props == null)
        {
            ClearMediaInfo();
            return;
        }

        Title = props.Title ?? string.Empty;
        Artist = props.Artist ?? string.Empty;
        HasMedia = !string.IsNullOrWhiteSpace(Title) || !string.IsNullOrWhiteSpace(Artist);

        Thumbnail = await ThumbnailLoader.LoadThumbnailAsync(props.Thumbnail);
    }

    private void DetectSourceApp(string? appId)
    {
        var appInfo = SourceAppDetector.Detect(appId);
        IsDedicatedMusicPlayer = appInfo.IsDedicatedMusicPlayer;
        SourceAppKind = appInfo.AppKind;
        SourceAppName = appInfo.AppName;
        SourceAppIconGeometry = appInfo.IconGeometry;
        SourceAppBrush = appInfo.Brush;
    }

    public void PreviewSeek(double percentage)
    {
        _timelineTracker.PreviewSeek(percentage);
    }

    public void SetSuspended(bool suspended)
    {
        _timelineTracker.SetSuspended(suspended, IsPlaying);
    }

    public async Task SeekToAsync(double percentage)
    {
        try
        {
            var newPos = await MediaPlaybackController.SeekToAsync(_sessionManager.CurrentSession, Duration, percentage);
            if (newPos > TimeSpan.Zero)
            {
                _timelineTracker.Position = newPos;
                _timelineTracker.UpdateTimelineTexts();
            }
        }
        finally
        {
            IsSeeking = false;
        }
    }

    public async Task PlayPauseAsync()
    {
        await MediaPlaybackController.PlayPauseAsync(_sessionManager.CurrentSession);
    }

    public async Task NextAsync()
    {
        await MediaPlaybackController.NextAsync(_sessionManager.CurrentSession);
    }

    public async Task PreviousAsync()
    {
        await MediaPlaybackController.PreviousAsync(_sessionManager.CurrentSession);
    }

    public async Task ToggleShuffleAsync()
    {
        IsShuffleActive = await MediaPlaybackController.ToggleShuffleAsync(_sessionManager.CurrentSession, IsShuffleActive);
    }

    public async Task ToggleRepeatAsync()
    {
        IsRepeatActive = await MediaPlaybackController.ToggleRepeatAsync(_sessionManager.CurrentSession, IsRepeatActive);
    }

    private void ClearMediaInfo()
    {
        _timelineTracker.Clear();
        Title = string.Empty;
        Artist = string.Empty;
        IsPlaying = false;
        IsShuffleActive = false;
        IsRepeatActive = false;
        IsDedicatedMusicPlayer = false;
        Thumbnail = null;
        HasMedia = false;

        var defaultApp = SourceAppInfo.CreateDefault();
        SourceAppKind = defaultApp.AppKind;
        SourceAppName = defaultApp.AppName;
        SourceAppIconGeometry = defaultApp.IconGeometry;
        SourceAppBrush = defaultApp.Brush;
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        _sessionManager.Dispose();
        _timelineTracker.Dispose();
        GC.SuppressFinalize(this);
    }
}
