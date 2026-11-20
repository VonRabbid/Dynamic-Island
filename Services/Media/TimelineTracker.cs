using System;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Windows.Media.Control;

namespace DynamicIsland.Services.Media;

public partial class TimelineTracker : ObservableObject, IDisposable
{
    private readonly DispatcherTimer _timelineTimer;
    private bool _isDisposed;

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

    public TimelineTracker()
    {
        _timelineTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timelineTimer.Tick += OnTimelineTimerTick;
    }

    public void UpdateTimeline(GlobalSystemMediaTransportControlsSessionTimelineProperties? timeline)
    {
        if (IsSeeking) return;

        if (timeline == null)
        {
            Clear();
            return;
        }

        Position = timeline.Position;
        Duration = timeline.EndTime;
        UpdateTimelineTexts();
    }

    public void OnPlaybackStateChanged(bool isPlaying)
    {
        if (isPlaying)
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

    public void SetSuspended(bool suspended, bool isPlaying)
    {
        if (suspended)
        {
            if (_timelineTimer.IsEnabled)
                _timelineTimer.Stop();
        }
        else
        {
            if (isPlaying && !_timelineTimer.IsEnabled)
                _timelineTimer.Start();
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

    public void UpdateTimelineTexts()
    {
        PositionText = FormatTimeSpan(Position);
        DurationText = FormatTimeSpan(Duration);
        ProgressPercentage = Duration.TotalSeconds > 0
            ? Math.Clamp((Position.TotalSeconds / Duration.TotalSeconds) * 100.0, 0.0, 100.0)
            : 0.0;
    }

    public void Clear()
    {
        _timelineTimer.Stop();
        Position = TimeSpan.Zero;
        Duration = TimeSpan.Zero;
        ProgressPercentage = 0;
        PositionText = "0:00";
        DurationText = "0:00";
    }

    private void OnTimelineTimerTick(object? sender, EventArgs e)
    {
        if (IsSeeking) return;

        if (Duration.TotalSeconds > 0 && Position < Duration)
        {
            Position = Position.Add(TimeSpan.FromSeconds(1));
            UpdateTimelineTexts();
        }
    }

    private static string FormatTimeSpan(TimeSpan ts)
    {
        if (ts < TimeSpan.Zero) ts = TimeSpan.Zero;
        return ts.TotalHours >= 1
            ? ts.ToString(@"h\:mm\:ss", System.Globalization.CultureInfo.InvariantCulture)
            : ts.ToString(@"m\:ss", System.Globalization.CultureInfo.InvariantCulture);
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        _timelineTimer.Stop();
        _timelineTimer.Tick -= OnTimelineTimerTick;
        GC.SuppressFinalize(this);
    }
}
