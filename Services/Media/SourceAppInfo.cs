using System;
using System.Windows.Media;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;

namespace DynamicIsland.Services.Media;

public sealed class SourceAppInfo
{
    public string AppKind { get; }
    public string AppName { get; }
    public string IconGeometry { get; }
    public Brush Brush { get; }
    public bool IsDedicatedMusicPlayer { get; }

    public SourceAppInfo(string appKind, string appName, string iconGeometry, Brush brush, bool isDedicatedMusicPlayer)
    {
        AppKind = appKind;
        AppName = appName;
        IconGeometry = iconGeometry;
        Brush = brush;
        IsDedicatedMusicPlayer = isDedicatedMusicPlayer;
    }

    public static SourceAppInfo CreateDefault()
    {
        var brush = new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xB0));
        brush.Freeze();
        return new SourceAppInfo("Generic", string.Empty, string.Empty, brush, false);
    }
}

public static class SourceAppDetector
{
    public static SourceAppInfo Detect(string? appId)
    {
        if (string.IsNullOrWhiteSpace(appId))
        {
            return SourceAppInfo.CreateDefault();
        }

        // 1. Reproductores de música dedicados
        if (appId.Contains("Spotify", StringComparison.OrdinalIgnoreCase))
        {
            var brush = new SolidColorBrush(Color.FromRgb(0x1E, 0xD7, 0x60));
            brush.Freeze();
            return new SourceAppInfo(
                "Spotify",
                "Spotify",
                "M12 2C6.477 2 2 6.477 2 12s4.477 10 10 10 10-4.477 10-10S17.523 2 12 2zm4.586 14.424c-.18.295-.563.387-.857.207-2.35-1.435-5.308-1.76-8.794-.963-.335.077-.67-.133-.746-.468-.077-.334.132-.67.467-.746 3.808-.87 7.076-.496 9.723 1.115.294.18.386.562.207.855zm1.224-2.72c-.226.367-.706.482-1.072.256-2.687-1.652-6.785-2.131-9.965-1.166-.413.127-.85-.106-.976-.518-.127-.413.106-.85.518-.977 3.632-1.102 8.147-.568 11.239 1.332.366.226.482.706.256 1.073zm.106-2.827C14.693 8.94 8.563 8.74 5.013 9.818c-.495.15-1.022-.132-1.172-.627-.15-.494.133-1.022.627-1.172 4.085-1.24 10.846-1.012 14.673 1.258.445.264.59.84.327 1.285-.264.444-.84.59-1.285.325z",
                brush,
                true);
        }

        if (appId.Contains("AppleMusic", StringComparison.OrdinalIgnoreCase) || appId.Contains("AppleInc.AppleMusic", StringComparison.OrdinalIgnoreCase))
        {
            var brush = new SolidColorBrush(Color.FromRgb(0xFC, 0x3C, 0x44));
            brush.Freeze();
            return new SourceAppInfo(
                "AppleMusic",
                "Apple Music",
                "M12 3v10.55c-.59-.34-1.27-.55-2-.55-2.21 0-4 1.79-4 4s1.79 4 4 4 4-1.79 4-4V7h4V3h-6z",
                brush,
                true);
        }

        if (appId.Contains("Deezer", StringComparison.OrdinalIgnoreCase))
        {
            var brush = new SolidColorBrush(Color.FromRgb(0xA2, 0x38, 0xFF));
            brush.Freeze();
            return new SourceAppInfo(
                "Deezer",
                "Deezer",
                "M2 14h3v6H2v-6zm5-4h3v10H7V10zm5-4h3v14h-3V6zm5-4h3v18h-3V2z",
                brush,
                true);
        }

        if (appId.Contains("TIDAL", StringComparison.OrdinalIgnoreCase) || appId.Contains("Tidal", StringComparison.OrdinalIgnoreCase))
        {
            var brush = new SolidColorBrush(Color.FromRgb(0x00, 0xE5, 0xFF));
            brush.Freeze();
            return new SourceAppInfo(
                "Tidal",
                "TIDAL",
                "M12 2l4 4-4 4-4-4 4-4zm-6 6l4 4-4 4-4-4 4-4zm12 0l4 4-4 4-4-4 4-4zm-6 6l4 4-4 4-4-4 4-4z",
                brush,
                true);
        }

        if (appId.Contains("ZuneMusic", StringComparison.OrdinalIgnoreCase) || appId.Contains("WindowsMediaPlayer", StringComparison.OrdinalIgnoreCase))
        {
            var brush = new SolidColorBrush(Color.FromRgb(0xF2, 0x71, 0x1C));
            brush.Freeze();
            return new SourceAppInfo(
                "MediaPlayer",
                "Windows Media Player",
                "M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-2 14.5v-9l6 4.5-6 4.5z",
                brush,
                true);
        }

        // 2. Navegadores web
        if (appId.Contains("Brave", StringComparison.OrdinalIgnoreCase))
        {
            var brush = new SolidColorBrush(Color.FromRgb(0xFB, 0x54, 0x2B));
            brush.Freeze();
            return new SourceAppInfo(
                "Brave",
                "Brave Browser",
                "M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-1 17.93c-3.95-.49-7-3.85-7-7.93 0-.62.08-1.21.21-1.79L9 15v1c0 1.1.9 2 2 2v1.93zm6.9-2.54c-.26-.81-1-1.39-1.9-1.39h-1v-3c0-.55-.45-1-1-1H8v-2h2c.55 0 1-.45 1-1V7h2c1.1 0 2-.9 2-2v-.41c2.93 1.19 5 4.06 5 7.41 0 2.08-.8 3.97-2.1 5.39z",
                brush,
                false);
        }

        if (appId.Contains("Vivaldi", StringComparison.OrdinalIgnoreCase))
        {
            var brush = new SolidColorBrush(Color.FromRgb(0xEF, 0x39, 0x39));
            brush.Freeze();
            return new SourceAppInfo(
                "Vivaldi",
                "Vivaldi Browser",
                "M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-1.5 15L6 7h3l3 7.5L15 7h3l-4.5 10h-3z",
                brush,
                false);
        }

        if (appId.Contains("Opera", StringComparison.OrdinalIgnoreCase))
        {
            var brush = new SolidColorBrush(Color.FromRgb(0xFF, 0x1B, 0x2D));
            brush.Freeze();
            return new SourceAppInfo(
                "Opera",
                "Opera Browser",
                "M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm0 17c-2.76 0-5-2.24-5-5V10c0-2.76 2.24-5 5-5s5 2.24 5 5v4c0 2.76-2.24 5-5 5z",
                brush,
                false);
        }

        if (appId.Contains("Chrome", StringComparison.OrdinalIgnoreCase))
        {
            var brush = new SolidColorBrush(Color.FromRgb(0xEA, 0x43, 0x35));
            brush.Freeze();
            return new SourceAppInfo(
                "Chrome",
                "Google Chrome / YouTube",
                "M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm0 3c2.6 0 4.87 1.45 6.04 3.58H12a4 4 0 0 0-3.46 2L6.11 6.3A7.02 7.02 0 0 1 12 5zm-4 7c0-2.21 1.79-4 4-4 .67 0 1.3.17 1.85.46L11.42 13H5.06A6.95 6.95 0 0 1 8 12zm1.07 3.73l2.43-4.21A4.01 4.01 0 0 0 12 16c.73 0 1.41-.2 2-.54l2.43 4.22A6.97 6.97 0 0 1 12 21a6.99 6.99 0 0 1-2.93-5.27zm3.93-1.73a2 2 0 1 1 0-4 2 2 0 0 1 0 4zm5.95-.73A6.99 6.99 0 0 1 17.89 19l-2.43-4.22c.34-.54.54-1.18.54-1.87 0-.58-.15-1.13-.4-1.61h4.94c.26.83.41 1.7.41 2.61z",
                brush,
                false);
        }

        if (appId.Contains("Edge", StringComparison.OrdinalIgnoreCase) || appId.Contains("MSEdge", StringComparison.OrdinalIgnoreCase))
        {
            var brush = new SolidColorBrush(Color.FromRgb(0x00, 0x78, 0xD7));
            brush.Freeze();
            return new SourceAppInfo(
                "Edge",
                "Microsoft Edge",
                "M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-1 17.93c-3.95-.49-7-3.85-7-7.93 0-.62.08-1.21.21-1.79L9 15v1c0 1.1.9 2 2 2v1.93zm6.9-2.54c-.26-.81-1-1.39-1.9-1.39h-1v-3c0-.55-.45-1-1-1H8v-2h2c.55 0 1-.45 1-1V7h2c1.1 0 2-.9 2-2v-.41c2.93 1.19 5 4.06 5 7.41 0 2.08-.8 3.97-2.1 5.39z",
                brush,
                false);
        }

        if (appId.Contains("Firefox", StringComparison.OrdinalIgnoreCase))
        {
            var brush = new SolidColorBrush(Color.FromRgb(0xFF, 0x71, 0x39));
            brush.Freeze();
            return new SourceAppInfo(
                "Firefox",
                "Mozilla Firefox",
                "M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-1 17.93c-3.95-.49-7-3.85-7-7.93 0-.62.08-1.21.21-1.79L9 15v1c0 1.1.9 2 2 2v1.93zm6.9-2.54c-.26-.81-1-1.39-1.9-1.39h-1v-3c0-.55-.45-1-1-1H8v-2h2c.55 0 1-.45 1-1V7h2c1.1 0 2-.9 2-2v-.41c2.93 1.19 5 4.06 5 7.41 0 2.08-.8 3.97-2.1 5.39z",
                brush,
                false);
        }

        // Genérico fallback
        var genericBrush = new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xB0));
        genericBrush.Freeze();
        return new SourceAppInfo(
            "Player",
            !string.IsNullOrWhiteSpace(appId) ? appId : "Reproductor",
            "M12 3v10.55c-.59-.34-1.27-.55-2-.55-2.21 0-4 1.79-4 4s1.79 4 4 4 4-1.79 4-4V7h4V3h-6z",
            genericBrush,
            false);
    }
}
