using System;
using System.Collections.Generic;
using DynamicIsland.Models;
using Forms = System.Windows.Forms;

namespace DynamicIsland.Services;

public static class ScreenService
{
    public static List<MonitorInfo> GetMonitors()
    {
        var list = new List<MonitorInfo>();
        var screens = Forms.Screen.AllScreens;

        for (int i = 0; i < screens.Length; i++)
        {
            var s = screens[i];
            string friendlyName = s.Primary ? $"Monitor {i + 1} (Principal)" : $"Monitor {i + 1}";
            list.Add(new MonitorInfo
            {
                DeviceName = s.DeviceName,
                DisplayName = $"{friendlyName} - {s.Bounds.Width}x{s.Bounds.Height}",
                IsPrimary = s.Primary,
                Width = s.Bounds.Width,
                Height = s.Bounds.Height
            });
        }

        return list;
    }

    public static Forms.Screen GetScreen(string? deviceName)
    {
        var screens = Forms.Screen.AllScreens;
        if (screens.Length == 0)
            return Forms.Screen.PrimaryScreen!;

        if (string.IsNullOrEmpty(deviceName) || deviceName == "Primary")
        {
            return Forms.Screen.PrimaryScreen ?? screens[0];
        }

        foreach (var s in screens)
        {
            if (string.Equals(s.DeviceName, deviceName, StringComparison.OrdinalIgnoreCase))
                return s;
        }

        return Forms.Screen.PrimaryScreen ?? screens[0];
    }
}
