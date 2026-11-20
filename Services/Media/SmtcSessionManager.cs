using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Media.Control;

namespace DynamicIsland.Services.Media;

public sealed class SmtcSessionManager : IDisposable
{
    private GlobalSystemMediaTransportControlsSessionManager? _sessionManager;
    private GlobalSystemMediaTransportControlsSession? _currentSession;
    private bool _isDisposed;

    public GlobalSystemMediaTransportControlsSession? CurrentSession => _currentSession;

    public event Action? SessionChanged;
    public event Action<GlobalSystemMediaTransportControlsSessionPlaybackInfo?>? PlaybackInfoChanged;
    public event Action<GlobalSystemMediaTransportControlsSessionTimelineProperties?>? TimelineChanged;
    public event Action<GlobalSystemMediaTransportControlsSessionMediaProperties?>? MediaPropertiesChanged;

    public async Task InitializeAsync()
    {
        try
        {
            _sessionManager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            if (_sessionManager != null)
            {
                _sessionManager.CurrentSessionChanged += OnSessionManagerCurrentSessionChanged;
                UpdateCurrentSession();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SmtcSessionManager] Error inicializando GSMTC: {ex.Message}");
        }
    }

    private void OnSessionManagerCurrentSessionChanged(GlobalSystemMediaTransportControlsSessionManager sender, CurrentSessionChangedEventArgs args)
    {
        UpdateCurrentSession();
    }

    public void UpdateCurrentSession()
    {
        DetachCurrentSession();

        _currentSession = _sessionManager?.GetCurrentSession();

        if (_currentSession != null)
        {
            AttachCurrentSession(_currentSession);
        }

        SessionChanged?.Invoke();
    }

    private void AttachCurrentSession(GlobalSystemMediaTransportControlsSession session)
    {
        session.MediaPropertiesChanged += OnMediaPropertiesChanged;
        session.PlaybackInfoChanged += OnPlaybackInfoChanged;
        session.TimelinePropertiesChanged += OnTimelinePropertiesChanged;
    }

    private void DetachCurrentSession()
    {
        if (_currentSession != null)
        {
            _currentSession.MediaPropertiesChanged -= OnMediaPropertiesChanged;
            _currentSession.PlaybackInfoChanged -= OnPlaybackInfoChanged;
            _currentSession.TimelinePropertiesChanged -= OnTimelinePropertiesChanged;
            _currentSession = null;
        }
    }

    private void OnPlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args)
    {
        PlaybackInfoChanged?.Invoke(sender.GetPlaybackInfo());
    }

    private void OnTimelinePropertiesChanged(GlobalSystemMediaTransportControlsSession sender, TimelinePropertiesChangedEventArgs args)
    {
        TimelineChanged?.Invoke(sender.GetTimelineProperties());
    }

    private async void OnMediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args)
    {
        try
        {
            var props = await sender.TryGetMediaPropertiesAsync();
            MediaPropertiesChanged?.Invoke(props);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SmtcSessionManager] Error en MediaPropertiesChanged: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        if (_sessionManager != null)
        {
            _sessionManager.CurrentSessionChanged -= OnSessionManagerCurrentSessionChanged;
            _sessionManager = null;
        }

        DetachCurrentSession();
        GC.SuppressFinalize(this);
    }
}
