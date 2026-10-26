using System;

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
        AppName = string.IsNullOrWhiteSpace(appName) ? "Sistema" : appName;
        Title = string.IsNullOrWhiteSpace(title) ? AppName : title;
        
        string rawMessage = message ?? string.Empty;
        Message = rawMessage.Length > 80 ? rawMessage[..77] + "..." : rawMessage;
        Timestamp = DateTime.Now;
    }
}
