namespace DynamicIsland.Models;

public class AppSettings
{
    public double VerticalOffset { get; set; }
    public int NotificationDuration { get; set; } = 5;
    public bool EnableMouseWheelVolume { get; set; } = true;
    public double IslandScale { get; set; } = 1.0;
    public double IslandOpacity { get; set; } = 0.85;
    public string SelectedMonitorDeviceName { get; set; } = "Primary";
    public bool EnableCapsLockAlert { get; set; } = true;
    public bool EnablePowerAlert { get; set; } = true;
    public bool UseCustomPosition { get; set; }
    public double CustomPositionX { get; set; } = -1;
    public double CustomPositionY { get; set; } = -1;
}
