using System;
using System.Windows;
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
        LoadSettings();
        _isInitializing = false;
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

        VolumeWheelCheckBox.IsChecked = settings.EnableMouseWheelVolume;
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
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

    private void VolumeWheelCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;

        SettingsService.Instance.CurrentSettings.EnableMouseWheelVolume = VolumeWheelCheckBox.IsChecked == true;
    }

    private void ResetDefaults_Click(object sender, RoutedEventArgs e)
    {
        var defaults = new AppSettings();
        SettingsService.Instance.CurrentSettings.VerticalOffset = defaults.VerticalOffset;
        SettingsService.Instance.CurrentSettings.NotificationDuration = defaults.NotificationDuration;
        SettingsService.Instance.CurrentSettings.IslandScale = defaults.IslandScale;
        SettingsService.Instance.CurrentSettings.EnableMouseWheelVolume = defaults.EnableMouseWheelVolume;

        _isInitializing = true;
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
