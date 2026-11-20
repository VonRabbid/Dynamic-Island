using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Media.Control;

namespace DynamicIsland.Services.Media;

public static class MediaPlaybackController
{
    public static async Task PlayPauseAsync(GlobalSystemMediaTransportControlsSession? session)
    {
        if (session == null) return;

        try
        {
            await session.TryTogglePlayPauseAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MediaPlaybackController] PlayPause error: {ex.Message}");
        }
    }

    public static async Task NextAsync(GlobalSystemMediaTransportControlsSession? session)
    {
        if (session == null) return;

        try
        {
            await session.TrySkipNextAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MediaPlaybackController] Next error: {ex.Message}");
        }
    }

    public static async Task PreviousAsync(GlobalSystemMediaTransportControlsSession? session)
    {
        if (session == null) return;

        try
        {
            await session.TrySkipPreviousAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MediaPlaybackController] Previous error: {ex.Message}");
        }
    }

    public static async Task<bool> ToggleShuffleAsync(GlobalSystemMediaTransportControlsSession? session, bool isCurrentlyShuffle)
    {
        if (session == null) return isCurrentlyShuffle;

        try
        {
            bool target = !isCurrentlyShuffle;
            await session.TryChangeShuffleActiveAsync(target);
            return target;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MediaPlaybackController] Shuffle error: {ex.Message}");
            return isCurrentlyShuffle;
        }
    }

    public static async Task<bool> ToggleRepeatAsync(GlobalSystemMediaTransportControlsSession? session, bool isCurrentlyRepeat)
    {
        if (session == null) return isCurrentlyRepeat;

        try
        {
            var target = isCurrentlyRepeat
                ? Windows.Media.MediaPlaybackAutoRepeatMode.None
                : Windows.Media.MediaPlaybackAutoRepeatMode.List;

            await session.TryChangeAutoRepeatModeAsync(target);
            return target != Windows.Media.MediaPlaybackAutoRepeatMode.None;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MediaPlaybackController] Repeat error: {ex.Message}");
            return isCurrentlyRepeat;
        }
    }

    public static async Task<TimeSpan> SeekToAsync(GlobalSystemMediaTransportControlsSession? session, TimeSpan duration, double percentage)
    {
        if (session == null || duration.TotalSeconds <= 0)
        {
            return TimeSpan.Zero;
        }

        try
        {
            double clamped = Math.Clamp(percentage, 0.0, 100.0);
            var targetTicks = (long)(duration.Ticks * (clamped / 100.0));
            var newPosition = TimeSpan.FromTicks(targetTicks);

            await session.TryChangePlaybackPositionAsync(targetTicks);
            return newPosition;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MediaPlaybackController] SeekTo error: {ex.Message}");
            return TimeSpan.Zero;
        }
    }
}
