using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Windows.Storage.Streams;

namespace DynamicIsland.Services.Media;

public static class ThumbnailLoader
{
    public static async Task<ImageSource?> LoadThumbnailAsync(IRandomAccessStreamReference? thumbnailRef)
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
            Debug.WriteLine($"[ThumbnailLoader] Error cargando o decodificando carátula: {ex.Message}");
            return null;
        }
    }
}
