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
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;

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
    private Brush _sourceAppBrush = new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xB0));

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
            DetectSourceApp();
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
            DetectSourceApp();
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
            DetectSourceApp();
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
        if (IsSeeking) return;

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
        if (!IsPlaying || IsSeeking) return;

        if (Duration.TotalSeconds > 0 && Position < Duration)
        {
            Position = Position.Add(TimeSpan.FromSeconds(1));
            UpdateTimelineTexts();
        }
    }

    public void PreviewSeek(double percentage)
    {
        if (Duration.TotalSeconds <= 0) return;
        double clamped = Math.Clamp(percentage, 0.0, 100.0);
        var targetTicks = (long)(Duration.Ticks * (clamped / 100.0));
        Position = TimeSpan.FromTicks(targetTicks);
        PositionText = FormatTimeSpan(Position);
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
            ? ts.ToString(@"h\:mm\:ss", System.Globalization.CultureInfo.InvariantCulture)
            : ts.ToString(@"m\:ss", System.Globalization.CultureInfo.InvariantCulture);
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

        try
        {
            Thumbnail = await LoadThumbnailAsync(props.Thumbnail);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MediaService] Error asignando miniatura: {ex.Message}");
            Thumbnail = null;
        }
    }

    public void SetSuspended(bool suspended)
    {
        if (suspended)
        {
            if (_timelineTimer.IsEnabled)
                _timelineTimer.Stop();
        }
        else
        {
            if (IsPlaying && !_timelineTimer.IsEnabled)
                _timelineTimer.Start();
        }
    }

    private static async Task<ImageSource?> LoadThumbnailAsync(IRandomAccessStreamReference? thumbnailRef)
    {
        if (thumbnailRef == null) return null;

        try
        {
            using var stream = await thumbnailRef.OpenReadAsync();
            if (stream == null || stream.Size == 0) return null;

            using var memoryStream = new MemoryStream();
            using (var inputStream = stream.AsStreamForRead())
            {
                await inputStream.CopyToAsync(memoryStream).ConfigureAwait(false);
            }

            if (memoryStream.Length == 0) return null;

            memoryStream.Position = 0;

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.DecodePixelWidth = 80;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = memoryStream;
            bitmap.EndInit();
            bitmap.Freeze();

            return bitmap;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MediaService] Error cargando o decodificando carátula: {ex.Message}");
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
        SourceAppKind = "Generic";
        SourceAppName = string.Empty;
        SourceAppIconGeometry = string.Empty;
        var defaultBrush = new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xB0));
        defaultBrush.Freeze();
        SourceAppBrush = defaultBrush;
    }

    private void DetectSourceApp()
    {
        if (_currentSession == null)
        {
            SourceAppKind = "Generic";
            SourceAppName = string.Empty;
            SourceAppIconGeometry = string.Empty;
            var genericBrush = new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xB0));
            genericBrush.Freeze();
            SourceAppBrush = genericBrush;
            return;
        }

        string appId = _currentSession.SourceAppUserModelId ?? string.Empty;

        if (appId.Contains("Spotify", StringComparison.OrdinalIgnoreCase))
        {
            SourceAppKind = "Spotify";
            SourceAppName = "Spotify";
            SourceAppIconGeometry = "M12 2C6.477 2 2 6.477 2 12s4.477 10 10 10 10-4.477 10-10S17.523 2 12 2zm4.586 14.424c-.18.295-.563.387-.857.207-2.35-1.435-5.308-1.76-8.794-.963-.335.077-.67-.133-.746-.468-.077-.334.132-.67.467-.746 3.808-.87 7.076-.496 9.723 1.115.294.18.386.562.207.855zm1.224-2.72c-.226.367-.706.482-1.072.256-2.687-1.652-6.785-2.131-9.965-1.166-.413.127-.85-.106-.976-.518-.127-.413.106-.85.518-.977 3.632-1.102 8.147-.568 11.239 1.332.366.226.482.706.256 1.073zm.106-2.827C14.693 8.94 8.563 8.74 5.013 9.818c-.495.15-1.022-.132-1.172-.627-.15-.494.133-1.022.627-1.172 4.085-1.24 10.846-1.012 14.673 1.258.445.264.59.84.327 1.285-.264.444-.84.59-1.285.325z";
            var brush = new SolidColorBrush(Color.FromRgb(0x1E, 0xD7, 0x60));
            brush.Freeze();
            SourceAppBrush = brush;
        }
        else if (appId.Contains("Chrome", StringComparison.OrdinalIgnoreCase))
        {
            SourceAppKind = "Chrome";
            SourceAppName = "Google Chrome / YouTube";
            SourceAppIconGeometry = "M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm0 3c2.6 0 4.87 1.45 6.04 3.58H12a4 4 0 0 0-3.46 2L6.11 6.3A7.02 7.02 0 0 1 12 5zm-4 7c0-2.21 1.79-4 4-4 .67 0 1.3.17 1.85.46L11.42 13H5.06A6.95 6.95 0 0 1 8 12zm1.07 3.73l2.43-4.21A4.01 4.01 0 0 0 12 16c.73 0 1.41-.2 2-.54l2.43 4.22A6.97 6.97 0 0 1 12 21a6.99 6.99 0 0 1-2.93-5.27zm3.93-1.73a2 2 0 1 1 0-4 2 2 0 0 1 0 4zm5.95-.73A6.99 6.99 0 0 1 17.89 19l-2.43-4.22c.34-.54.54-1.18.54-1.87 0-.58-.15-1.13-.4-1.61h4.94c.26.83.41 1.7.41 2.61z";
            var brush = new SolidColorBrush(Color.FromRgb(0x42, 0x85, 0xF4));
            brush.Freeze();
            SourceAppBrush = brush;
        }
        else if (appId.Contains("Edge", StringComparison.OrdinalIgnoreCase) || appId.Contains("MSEdge", StringComparison.OrdinalIgnoreCase))
        {
            SourceAppKind = "Edge";
            SourceAppName = "Microsoft Edge";
            SourceAppIconGeometry = "M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-1 17.93c-3.95-.49-7-3.85-7-7.93 0-.62.08-1.21.21-1.79L9 15v1c0 1.1.9 2 2 2v1.93zm6.9-2.54c-.26-.81-1-1.39-1.9-1.39h-1v-3c0-.55-.45-1-1-1H8v-2h2c.55 0 1-.45 1-1V7h2c1.1 0 2-.9 2-2v-.41c2.93 1.19 5 4.06 5 7.41 0 2.08-.8 3.97-2.1 5.39z";
            var brush = new SolidColorBrush(Color.FromRgb(0x00, 0x78, 0xD7));
            brush.Freeze();
            SourceAppBrush = brush;
        }
        else if (appId.Contains("Firefox", StringComparison.OrdinalIgnoreCase))
        {
            SourceAppKind = "Firefox";
            SourceAppName = "Mozilla Firefox";
            SourceAppIconGeometry = "M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-1 17.93c-3.95-.49-7-3.85-7-7.93 0-.62.08-1.21.21-1.79L9 15v1c0 1.1.9 2 2 2v1.93zm6.9-2.54c-.26-.81-1-1.39-1.9-1.39h-1v-3c0-.55-.45-1-1-1H8v-2h2c.55 0 1-.45 1-1V7h2c1.1 0 2-.9 2-2v-.41c2.93 1.19 5 4.06 5 7.41 0 2.08-.8 3.97-2.1 5.39z";
            var brush = new SolidColorBrush(Color.FromRgb(0xFF, 0x71, 0x39));
            brush.Freeze();
            SourceAppBrush = brush;
        }
        else if (appId.Contains("Brave", StringComparison.OrdinalIgnoreCase))
        {
            SourceAppKind = "Brave";
            SourceAppName = "Brave Browser";
            SourceAppIconGeometry = "M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-1 17.93c-3.95-.49-7-3.85-7-7.93 0-.62.08-1.21.21-1.79L9 15v1c0 1.1.9 2 2 2v1.93zm6.9-2.54c-.26-.81-1-1.39-1.9-1.39h-1v-3c0-.55-.45-1-1-1H8v-2h2c.55 0 1-.45 1-1V7h2c1.1 0 2-.9 2-2v-.41c2.93 1.19 5 4.06 5 7.41 0 2.08-.8 3.97-2.1 5.39z";
            var brush = new SolidColorBrush(Color.FromRgb(0xFB, 0x54, 0x2B));
            brush.Freeze();
            SourceAppBrush = brush;
        }
        else
        {
            SourceAppKind = "Player";
            SourceAppName = !string.IsNullOrWhiteSpace(appId) ? appId : "Reproductor";
            SourceAppIconGeometry = "M12 3v10.55c-.59-.34-1.27-.55-2-.55-2.21 0-4 1.79-4 4s1.79 4 4 4 4-1.79 4-4V7h4V3h-6z";
            var brush = new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xB0));
            brush.Freeze();
            SourceAppBrush = brush;
        }
    }

    public async Task SeekToAsync(double percentage)
    {
        if (_currentSession == null || Duration.TotalSeconds <= 0)
        {
            IsSeeking = false;
            return;
        }

        try
        {
            double clamped = Math.Clamp(percentage, 0.0, 100.0);
            var targetTicks = (long)(Duration.Ticks * (clamped / 100.0));
            Position = TimeSpan.FromTicks(targetTicks);
            UpdateTimelineTexts();

            await _currentSession.TryChangePlaybackPositionAsync(targetTicks);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MediaService] Error en SeekToAsync: {ex.Message}");
        }
        finally
        {
            IsSeeking = false;
        }
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
