using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Windows.Media.Control;
using Windows.Storage.Streams;

namespace DynamicIsland.Services;

public partial class MediaService : ObservableObject
{
    private GlobalSystemMediaTransportControlsSessionManager? _sessionManager;
    private GlobalSystemMediaTransportControlsSession? _currentSession;

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

    public static MediaService Instance { get; } = new();

    public MediaService()
    {
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
        UpdateCurrentSession();
    }

    private void UpdateCurrentSession()
    {
        if (_currentSession != null)
        {
            _currentSession.MediaPropertiesChanged -= OnMediaPropertiesChanged;
            _currentSession.PlaybackInfoChanged -= OnPlaybackInfoChanged;
        }

        _currentSession = _sessionManager?.GetCurrentSession();

        if (_currentSession != null)
        {
            _currentSession.MediaPropertiesChanged += OnMediaPropertiesChanged;
            _currentSession.PlaybackInfoChanged += OnPlaybackInfoChanged;
            _ = RefreshMediaInfoAsync();
        }
        else
        {
            ClearMediaInfo();
        }
    }

    private void OnPlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args)
    {
        UpdatePlaybackInfo(sender.GetPlaybackInfo());
    }

    private async void OnMediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args)
    {
        try
        {
            var props = await sender.TryGetMediaPropertiesAsync();
            await UpdateMediaPropertiesAsync(props);
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
            return;
        }

        IsPlaying = playbackInfo.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
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
        Title = string.Empty;
        Artist = string.Empty;
        IsPlaying = false;
        Thumbnail = null;
        HasMedia = false;
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
