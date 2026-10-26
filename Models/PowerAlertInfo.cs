namespace DynamicIsland.Models;

public class PowerAlertInfo
{
    public bool IsCharging { get; set; }
    public int BatteryPercent { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsLowBattery { get; set; }
}
