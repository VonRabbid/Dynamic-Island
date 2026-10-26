namespace DynamicIsland.Models;

public class MonitorInfo
{
    public string DeviceName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    public override string ToString() => DisplayName;
}
