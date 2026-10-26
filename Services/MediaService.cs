using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Windows.Media.Control;
using Windows.Storage.Streams;

namespace DynamicIsland.Services;

public partial class MediaService : ObservableObject
{
    private GlobalSystemMediaTransportControlsSessionManager? _sessionManager;
    private GlobalSystemMediaTransportControlsSession? _currentSession;
    private readonly DispatcherTimer _timelineTimer;

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
    private string _positionText = "0:00";

    [ObservableProperty]
    private string _durationText = "0:00";

    public static MediaService Instance { get; } = new();

    public MediaService()
    {
        _timelineTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timelineTimer.Tick += OnTimelineTimerTick;

        _ = InitializeAsync();
    }

    public async Task InitializeAsync()
    {
        try
        {
            _sessionManager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            if (_sessionManager != null)
            {
                _sessionManager.CurrentSessionChanged += OnCurrentSessionChanged;
                UpdateCurrentSession();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MediaService] Error inicializando GSMTC: {ex.Message}");
        }
    }

    private void OnCurrentSessionChanged(GlobalSystemMediaTransportControlsSessionManager sender, CurrentSessionChangedEventArgs args)
    {
        App.Current?.Dispatcher?.InvokeAsync(UpdateCurrentSession);
    }

    private void UpdateCurrentSession()
    {
        if (_currentSession != null)
        {
            _currentSession.MediaPropertiesChanged -= OnMediaPropertiesChanged;
            _currentSession.PlaybackInfoChanged -= OnPlaybackInfoChanged;
            _currentSession.TimelinePropertiesChanged -= OnTimelinePropertiesChanged;
        }

        _currentSession = _sessionManager?.GetCurrentSession();

        if (_currentSession != null)
        {
            _currentSession.MediaPropertiesChanged += OnMediaPropertiesChanged;
            _currentSession.PlaybackInfoChanged += OnPlaybackInfoChanged;
            _currentSession.TimelinePropertiesChanged += OnTimelinePropertiesChanged;
            _ = RefreshMediaInfoAsync();
        }
        else
        {
            ClearMediaInfo();
        }
    }

    private void OnPlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args)
    {
        App.Current?.Dispatcher?.InvokeAsync(() => UpdatePlaybackInfo(sender.GetPlaybackInfo()));
    }

    private void OnTimelinePropertiesChanged(GlobalSystemMediaTransportControlsSession sender, TimelinePropertiesChangedEventArgs args)
    {
        App.Current?.Dispatcher?.InvokeAsync(() => UpdateTimeline(sender.GetTimelineProperties()));
    }

    private async void OnMediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args)
    {
        try
        {
            var props = await sender.TryGetMediaPropertiesAsync();
            App.Current?.Dispatcher?.InvokeAsync(async () => await UpdateMediaPropertiesAsync(props));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MediaService] Error en MediaPropertiesChanged: {ex.Message}");
        }
    }

    private async Task RefreshMediaInfoAsync()
    {
        if (_currentSession == null)
        {
            ClearMediaInfo();
            return;
        }

        try
        {
            var playbackInfo = _currentSession.GetPlaybackInfo();
            UpdatePlaybackInfo(playbackInfo);

            var timeline = _currentSession.GetTimelineProperties();
            UpdateTimeline(timeline);

            var props = await _currentSession.TryGetMediaPropertiesAsync();
            await UpdateMediaPropertiesAsync(props);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MediaService] Error al refrescar sesión multimedia: {ex.Message}");
        }
    }

    private void UpdatePlaybackInfo(GlobalSystemMediaTransportControlsSessionPlaybackInfo? playbackInfo)
    {
        if (playbackInfo == null)
        {
            IsPlaying = false;
            _timelineTimer.Stop();
            return;
        }

        IsPlaying = playbackInfo.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;

        if (IsPlaying)
        {
            if (!_timelineTimer.IsEnabled)
                _timelineTimer.Start();
        }
        else
        {
            if (_timelineTimer.IsEnabled)
                _timelineTimer.Stop();
        }
    }

    private void UpdateTimeline(GlobalSystemMediaTransportControlsSessionTimelineProperties? timeline)
    {
        if (timeline == null)
        {
            Position = TimeSpan.Zero;
            Duration = TimeSpan.Zero;
            ProgressPercentage = 0;
            PositionText = "0:00";
            DurationText = "0:00";
            return;
        }

        Position = timeline.Position;
        Duration = timeline.EndTime;
        UpdateTimelineTexts();
    }

    private void OnTimelineTimerTick(object? sender, EventArgs e)
    {
        if (!IsPlaying) return;

        if (Duration.TotalSeconds > 0 && Position < Duration)
        {
            Position = Position.Add(TimeSpan.FromSeconds(1));
            UpdateTimelineTexts();
        }
    }

    private void UpdateTimelineTexts()
    {
        PositionText = FormatTimeSpan(Position);
        DurationText = FormatTimeSpan(Duration);
        ProgressPercentage = Duration.TotalSeconds > 0
            ? Math.Clamp((Position.TotalSeconds / Duration.TotalSeconds) * 100.0, 0.0, 100.0)
            : 0.0;
    }

    private static string FormatTimeSpan(TimeSpan ts)
    {
        if (ts < TimeSpan.Zero) ts = TimeSpan.Zero;
        return ts.TotalHours >= 1
            ? ts.ToString(@"h\:mm\:ss")
            : ts.ToString(@"m\:ss");
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

        Thumbnail = await LoadThumbnailAsync(props.Thumbnail);
    }

    private async Task<ImageSource?> LoadThumbnailAsync(IRandomAccessStreamReference? thumbnailRef)
    {
        if (thumbnailRef == null) return null;

        try
        {
            using var stream = await thumbnailRef.OpenReadAsync();
            if (stream == null || stream.Size == 0) return null;

            using var reader = new DataReader(stream.GetInputStreamAt(0));
            await reader.LoadAsync((uint)stream.Size);
            var bytes = new byte[stream.Size];
            reader.ReadBytes(bytes);

            var bitmap = new BitmapImage();
            using (var ms = new MemoryStream(bytes))
            {
                bitmap.BeginInit();
                bitmap.CreateOptions = BitmapCreateOptions.None;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = ms;
                bitmap.EndInit();
            }
            bitmap.Freeze();
            return bitmap;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MediaService] Error cargando carátula: {ex.Message}");
            return null;
        }
    }

    private void ClearMediaInfo()
    {
        _timelineTimer.Stop();
        Title = string.Empty;
        Artist = string.Empty;
        IsPlaying = false;
        Thumbnail = null;
        HasMedia = false;
        Position = TimeSpan.Zero;
        Duration = TimeSpan.Zero;
        ProgressPercentage = 0;
        PositionText = "0:00";
        DurationText = "0:00";
    }

    public async Task PlayPauseAsync()
    {
        if (_currentSession != null)
        {
            try
            {
                await _currentSession.TryTogglePlayPauseAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MediaService] PlayPause error: {ex.Message}");
            }
        }
    }

    public async Task NextAsync()
    {
        if (_currentSession != null)
        {
            try
            {
                await _currentSession.TrySkipNextAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MediaService] Next error: {ex.Message}");
            }
        }
    }

    public async Task PreviousAsync()
    {
        if (_currentSession != null)
        {
            try
            {
                await _currentSession.TrySkipPreviousAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MediaService] Previous error: {ex.Message}");
            }
        }
    }
}
