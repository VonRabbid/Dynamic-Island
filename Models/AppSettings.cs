namespace DynamicIsland.Models;

public class AppSettings
{
    public double VerticalOffset { get; set; }
    public int NotificationDuration { get; set; } = 5;
    public bool EnableMouseWheelVolume { get; set; } = true;
    public double IslandScale { get; set; } = 1.0;
    public string SelectedMonitorDeviceName { get; set; } = "Primary";
    public bool EnableCapsLockAlert { get; set; } = true;
    public bool EnablePowerAlert { get; set; } = true;
}
