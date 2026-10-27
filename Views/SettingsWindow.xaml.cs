using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DynamicIsland.Models;
using DynamicIsland.Services;

namespace DynamicIsland.Views;

public partial class SettingsWindow : Window
{
    private bool _isInitializing = true;

    public SettingsWindow()
    {
        InitializeComponent();
        LoadMonitors();
        LoadSettings();
        _isInitializing = false;
    }

    private void LoadMonitors()
    {
        var monitors = ScreenService.GetMonitors();
        MonitorComboBox.ItemsSource = monitors;

        string currentDevice = SettingsService.Instance.CurrentSettings.SelectedMonitorDeviceName;
        int selectedIndex = 0;

        for (int i = 0; i < monitors.Count; i++)
        {
            if (string.Equals(monitors[i].DeviceName, currentDevice, StringComparison.OrdinalIgnoreCase))
            {
                selectedIndex = i;
                break;
            }
            if (currentDevice == "Primary" && monitors[i].IsPrimary)
            {
                selectedIndex = i;
            }
        }

        if (monitors.Count > 0)
        {
            MonitorComboBox.SelectedIndex = selectedIndex;
        }
    }

    private void LoadSettings()
    {
        var settings = SettingsService.Instance.CurrentSettings;

        OffsetSlider.Value = settings.VerticalOffset;
        OffsetValueText.Text = $"{(int)settings.VerticalOffset} px";

        DurationSlider.Value = settings.NotificationDuration;
        DurationValueText.Text = $"{settings.NotificationDuration} s";

        ScaleSlider.Value = settings.IslandScale;
        ScaleValueText.Text = $"{Math.Round(settings.IslandScale * 100)}%";

        OpacitySlider.Value = settings.IslandOpacity;
        OpacityValueText.Text = $"{Math.Round(settings.IslandOpacity * 100)}%";

        VolumeWheelCheckBox.IsChecked = settings.EnableMouseWheelVolume;
        CapsLockCheckBox.IsChecked = settings.EnableCapsLockAlert;
        PowerAlertCheckBox.IsChecked = settings.EnablePowerAlert;
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void MonitorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;

        if (MonitorComboBox.SelectedItem is MonitorInfo monitor)
        {
            SettingsService.Instance.CurrentSettings.SelectedMonitorDeviceName = monitor.DeviceName;
            SettingsService.Instance.NotifyChanged();
        }
    }

    private void OffsetSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isInitializing) return;

        int value = (int)e.NewValue;
        if (OffsetValueText != null)
        {
            OffsetValueText.Text = $"{value} px";
        }

        SettingsService.Instance.CurrentSettings.VerticalOffset = value;
        SettingsService.Instance.NotifyChanged();
    }

    private void DurationSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isInitializing) return;

        int value = (int)e.NewValue;
        if (DurationValueText != null)
        {
            DurationValueText.Text = $"{value} s";
        }

        SettingsService.Instance.CurrentSettings.NotificationDuration = value;
    }

    private void ScaleSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isInitializing) return;

        double value = Math.Round(e.NewValue, 2);
        if (ScaleValueText != null)
        {
            ScaleValueText.Text = $"{Math.Round(value * 100)}%";
        }

        SettingsService.Instance.CurrentSettings.IslandScale = value;
        SettingsService.Instance.NotifyChanged();
    }

    private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isInitializing) return;

        double value = Math.Round(e.NewValue, 2);
        if (OpacityValueText != null)
        {
            OpacityValueText.Text = $"{Math.Round(value * 100)}%";
        }

        SettingsService.Instance.CurrentSettings.IslandOpacity = value;
        SettingsService.Instance.NotifyChanged();
    }

    private void Checkboxes_Changed(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;

        SettingsService.Instance.CurrentSettings.EnableMouseWheelVolume = VolumeWheelCheckBox.IsChecked == true;
        SettingsService.Instance.CurrentSettings.EnableCapsLockAlert = CapsLockCheckBox.IsChecked == true;
        SettingsService.Instance.CurrentSettings.EnablePowerAlert = PowerAlertCheckBox.IsChecked == true;
    }

    private void ResetDefaults_Click(object sender, RoutedEventArgs e)
    {
        var defaults = new AppSettings();
        SettingsService.Instance.CurrentSettings.SelectedMonitorDeviceName = defaults.SelectedMonitorDeviceName;
        SettingsService.Instance.CurrentSettings.VerticalOffset = defaults.VerticalOffset;
        SettingsService.Instance.CurrentSettings.NotificationDuration = defaults.NotificationDuration;
        SettingsService.Instance.CurrentSettings.IslandScale = defaults.IslandScale;
        SettingsService.Instance.CurrentSettings.IslandOpacity = defaults.IslandOpacity;
        SettingsService.Instance.CurrentSettings.EnableMouseWheelVolume = defaults.EnableMouseWheelVolume;
        SettingsService.Instance.CurrentSettings.EnableCapsLockAlert = defaults.EnableCapsLockAlert;
        SettingsService.Instance.CurrentSettings.EnablePowerAlert = defaults.EnablePowerAlert;

        _isInitializing = true;
        LoadMonitors();
        LoadSettings();
        _isInitializing = false;

        SettingsService.Instance.Save();
    }

    private void SaveAndClose_Click(object sender, RoutedEventArgs e)
    {
        SettingsService.Instance.Save();
        Close();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        SettingsService.Instance.Save();
        Close();
    }
}
