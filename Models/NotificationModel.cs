using System;
using System.Text;

namespace DynamicIsland.Models;

public class NotificationModel
{
    public string AppName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;

    public NotificationModel()
    {
    }

    public NotificationModel(string appName, string title, string message)
    {
        string cleanApp = Sanitize(appName, 60);
        string cleanTitle = Sanitize(title, 100);
        string cleanMessage = Sanitize(message, 250);

        AppName = string.IsNullOrWhiteSpace(cleanApp) ? "Sistema" : cleanApp;
        Title = string.IsNullOrWhiteSpace(cleanTitle) ? AppName : cleanTitle;
        Message = cleanMessage.Length > 80 ? cleanMessage[..77] + "..." : cleanMessage;
        Timestamp = DateTime.Now;
    }

    /// <summary>
    /// Sanitiza cadenas externas eliminando caracteres de control ASCII no imprimibles,
    /// secuencias de anulación bidireccional (Bidi overrides) y truncando longitud para mitigar DoS.
    /// </summary>
    public static string Sanitize(string? input, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var sb = new StringBuilder(Math.Min(input.Length, maxLength));
        foreach (char c in input)
        {
            if (sb.Length >= maxLength)
                break;

            // Filtrar caracteres de control ASCII (0x00 a 0x1F) excepto espacios y saltos permitidos
            if (c < 0x20 && c != '\n' && c != '\r' && c != '\t')
                continue;

            // Filtrar caracteres bidireccionales maliciosos (Unicode Bidi Overrides & Isolate)
            if (c is '\u202A' or '\u202B' or '\u202C' or '\u202D' or '\u202E'
                or '\u2066' or '\u2067' or '\u2068' or '\u2069' or '\u200E' or '\u200F')
                continue;

            // Filtrar caracteres nulos y reemplazo corrupto
            if (c is '\u007F' or '\uFFFD')
                continue;

            sb.Append(c);
        }

        return sb.ToString().Trim();
    }
}
