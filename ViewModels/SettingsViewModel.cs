using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicIsland.Models;
using DynamicIsland.Services;

namespace DynamicIsland.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsService _settingsService;
    private bool _isInitializing = true;

    [ObservableProperty]
    private IReadOnlyList<MonitorInfo> _monitors = Array.Empty<MonitorInfo>();

    [ObservableProperty]
    private MonitorInfo? _selectedMonitor;

    [ObservableProperty]
    private double _verticalOffset;

    [ObservableProperty]
    private string _offsetValueText = "0 px";

    [ObservableProperty]
    private int _notificationDuration = 5;

    [ObservableProperty]
    private string _durationValueText = "5 s";

    [ObservableProperty]
    private double _islandScale = 1.0;

    [ObservableProperty]
    private string _scaleValueText = "100%";

    [ObservableProperty]
    private double _islandOpacity = 0.85;

    [ObservableProperty]
    private string _opacityValueText = "85%";

    [ObservableProperty]
    private bool _enableMouseWheelVolume = true;

    [ObservableProperty]
    private bool _enableCapsLockAlert = true;

    [ObservableProperty]
    private bool _enablePowerAlert = true;

    public event Action? RequestClose;

    public SettingsViewModel() : this(SettingsService.Instance)
    {
    }

    public SettingsViewModel(SettingsService settingsService)
    {
        _settingsService = settingsService;
        LoadSettings();
    }

    public void LoadSettings()
    {
        _isInitializing = true;

        var monitors = ScreenService.GetMonitors();
        Monitors = monitors;

        var currentSettings = _settingsService.CurrentSettings;
        string currentDevice = currentSettings.SelectedMonitorDeviceName;

        MonitorInfo? matchedMonitor = monitors.FirstOrDefault(m =>
            string.Equals(m.DeviceName, currentDevice, StringComparison.OrdinalIgnoreCase));

        if (matchedMonitor == null && currentDevice == "Primary")
        {
            matchedMonitor = monitors.FirstOrDefault(m => m.IsPrimary);
        }

        SelectedMonitor = matchedMonitor ?? monitors.FirstOrDefault();

        VerticalOffset = currentSettings.VerticalOffset;
        OffsetValueText = $"{(int)Math.Round(currentSettings.VerticalOffset)} px";

        NotificationDuration = currentSettings.NotificationDuration;
        DurationValueText = $"{currentSettings.NotificationDuration} s";

        IslandScale = currentSettings.IslandScale;
        ScaleValueText = $"{Math.Round(currentSettings.IslandScale * 100)}%";

        IslandOpacity = currentSettings.IslandOpacity;
        OpacityValueText = $"{Math.Round(currentSettings.IslandOpacity * 100)}%";

        EnableMouseWheelVolume = currentSettings.EnableMouseWheelVolume;
        EnableCapsLockAlert = currentSettings.EnableCapsLockAlert;
        EnablePowerAlert = currentSettings.EnablePowerAlert;

        _isInitializing = false;
    }

    partial void OnSelectedMonitorChanged(MonitorInfo? value)
    {
        if (_isInitializing || value == null) return;
        _settingsService.CurrentSettings.SelectedMonitorDeviceName = value.DeviceName;
        _settingsService.NotifyChanged();
    }

    partial void OnVerticalOffsetChanged(double value)
    {
        int rounded = (int)Math.Round(value);
        OffsetValueText = $"{rounded} px";

        if (_isInitializing) return;
        _settingsService.CurrentSettings.VerticalOffset = rounded;
        _settingsService.NotifyChanged();
    }

    partial void OnNotificationDurationChanged(int value)
    {
        DurationValueText = $"{value} s";

        if (_isInitializing) return;
        _settingsService.CurrentSettings.NotificationDuration = value;
    }

    partial void OnIslandScaleChanged(double value)
    {
        double rounded = Math.Round(value, 2);
        ScaleValueText = $"{Math.Round(rounded * 100)}%";

        if (_isInitializing) return;
        _settingsService.CurrentSettings.IslandScale = rounded;
        _settingsService.NotifyChanged();
    }

    partial void OnIslandOpacityChanged(double value)
    {
        double rounded = Math.Round(value, 2);
        OpacityValueText = $"{Math.Round(rounded * 100)}%";

        if (_isInitializing) return;
        _settingsService.CurrentSettings.IslandOpacity = rounded;
        _settingsService.NotifyChanged();
    }

    partial void OnEnableMouseWheelVolumeChanged(bool value)
    {
        if (_isInitializing) return;
        _settingsService.CurrentSettings.EnableMouseWheelVolume = value;
    }

    partial void OnEnableCapsLockAlertChanged(bool value)
    {
        if (_isInitializing) return;
        _settingsService.CurrentSettings.EnableCapsLockAlert = value;
    }

    partial void OnEnablePowerAlertChanged(bool value)
    {
        if (_isInitializing) return;
        _settingsService.CurrentSettings.EnablePowerAlert = value;
    }

    [RelayCommand]
    private void ResetDefaults()
    {
        var defaults = new AppSettings();
        _settingsService.CurrentSettings.SelectedMonitorDeviceName = defaults.SelectedMonitorDeviceName;
        _settingsService.CurrentSettings.VerticalOffset = defaults.VerticalOffset;
        _settingsService.CurrentSettings.NotificationDuration = defaults.NotificationDuration;
        _settingsService.CurrentSettings.IslandScale = defaults.IslandScale;
        _settingsService.CurrentSettings.IslandOpacity = defaults.IslandOpacity;
        _settingsService.CurrentSettings.EnableMouseWheelVolume = defaults.EnableMouseWheelVolume;
        _settingsService.CurrentSettings.EnableCapsLockAlert = defaults.EnableCapsLockAlert;
        _settingsService.CurrentSettings.EnablePowerAlert = defaults.EnablePowerAlert;

        LoadSettings();
        _settingsService.Save();
    }

    [RelayCommand]
    private void SaveAndClose()
    {
        _settingsService.Save();
        RequestClose?.Invoke();
    }

    [RelayCommand]
    private void Close()
    {
        _settingsService.Save();
        RequestClose?.Invoke();
    }
}
