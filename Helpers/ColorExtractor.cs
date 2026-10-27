using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using FormatConvertedBitmap = System.Windows.Media.Imaging.FormatConvertedBitmap;
using LinearGradientBrush = System.Windows.Media.LinearGradientBrush;
using PixelFormats = System.Windows.Media.PixelFormats;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;

namespace DynamicIsland.Helpers;

public record ExtractedColors(
    Color AccentColor,
    Brush AccentBrush,
    Color BackgroundStartColor,
    Color BackgroundEndColor,
    Brush DynamicBackgroundBrush
);

public static class ColorExtractor
{
    private static readonly Color DefaultAccent = Color.FromRgb(0x38, 0xBD, 0xF8); // #38BDF8 (Cyan)

    public static ExtractedColors GetDefaultColors(double opacity = 0.85)
    {
        byte baseAlpha = (byte)Math.Clamp((int)(opacity * 255.0), 100, 255);
        byte startAlpha = (byte)Math.Clamp((int)(baseAlpha * 0.35), 40, 140);

        var startColor = Color.FromArgb(startAlpha, 0x38, 0xBD, 0xF8);
        var endColor = Color.FromArgb(baseAlpha, 0x0E, 0x0E, 0x14);

        var accentBrush = new SolidColorBrush(DefaultAccent);
        accentBrush.Freeze();

        var gradientBrush = new LinearGradientBrush(startColor, endColor, 45.0);
        gradientBrush.Freeze();

        return new ExtractedColors(DefaultAccent, accentBrush, startColor, endColor, gradientBrush);
    }

    public static ExtractedColors ExtractFromBitmap(BitmapSource? bitmap, double opacity = 0.85)
    {
        if (bitmap == null)
            return GetDefaultColors(opacity);

        try
        {
            BitmapSource formattedBitmap = bitmap;
            if (bitmap.Format != PixelFormats.Bgra32 && bitmap.Format != PixelFormats.Bgr32)
            {
                formattedBitmap = new FormatConvertedBitmap(bitmap, PixelFormats.Bgra32, null, 0);
            }

            int width = formattedBitmap.PixelWidth;
            int height = formattedBitmap.PixelHeight;

            if (width <= 0 || height <= 0)
                return GetDefaultColors(opacity);

            int stride = width * 4;
            byte[] pixels = new byte[height * stride];
            formattedBitmap.CopyPixels(pixels, stride, 0);

            // Muestreo de píxeles saltando de 3 en 3 para mínimo consumo de CPU
            double bestScore = -1.0;
            Color bestColor = DefaultAccent;

            for (int y = 0; y < height; y += 3)
            {
                int rowOffset = y * stride;
                for (int x = 0; x < width; x += 3)
                {
                    int index = rowOffset + (x * 4);
                    byte b = pixels[index];
                    byte g = pixels[index + 1];
                    byte r = pixels[index + 2];
                    byte a = pixels[index + 3];

                    if (a < 128) continue; // Descartar transparentes

                    // Brillo ponderado por percepción humana
                    double brightness = (0.299 * r + 0.587 * g + 0.114 * b);
                    if (brightness < 30 || brightness > 230) continue; // Descartar muy oscuros o sobreexpuestos

                    // Saturación
                    int max = Math.Max(r, Math.Max(g, b));
                    int min = Math.Min(r, Math.Min(g, b));
                    if (max == 0) continue;

                    double saturation = (double)(max - min) / max;
                    if (saturation < 0.20) continue; // Descartar escala de grises

                    // Puntuación: mayor saturación y brillo equilibrado hacia tonos medios
                    double balance = 1.0 - (Math.Abs(brightness - 128.0) / 128.0);
                    double score = (saturation * 2.5) + balance;

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestColor = Color.FromRgb(r, g, b);
                    }
                }
            }

            if (bestScore < 0)
            {
                return GetDefaultColors(opacity);
            }

            var accentBrush = new SolidColorBrush(bestColor);
            accentBrush.Freeze();

            byte baseAlpha = (byte)Math.Clamp((int)(opacity * 255.0), 100, 255);
            byte startAlpha = (byte)Math.Clamp((int)(baseAlpha * 0.45), 45, 160);

            var startColor = Color.FromArgb(startAlpha, bestColor.R, bestColor.G, bestColor.B);
            var endColor = Color.FromArgb(baseAlpha, 0x0E, 0x0E, 0x14);

            var gradientBrush = new LinearGradientBrush(startColor, endColor, 45.0);
            gradientBrush.Freeze();

            return new ExtractedColors(bestColor, accentBrush, startColor, endColor, gradientBrush);
        }
        catch
        {
            return GetDefaultColors(opacity);
        }
    }
}
