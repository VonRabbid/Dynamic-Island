using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using DynamicIsland.Helpers;
using DynamicIsland.Models;
using DynamicIsland.Services;
using DynamicIsland.ViewModels;
using Rectangle = System.Windows.Shapes.Rectangle;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace DynamicIsland;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly FullscreenDetector _fullscreenDetector;
    private DispatcherTimer? _volumeFadeTimer;

    public MainWindow()
    {
        InitializeComponent();

        _viewModel = new MainViewModel();
        DataContext = _viewModel;

        _fullscreenDetector = new FullscreenDetector();
        _fullscreenDetector.FullscreenChanged += OnFullscreenChanged;

        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        _viewModel.Media.PropertyChanged += OnMediaPropertyChanged;
        _viewModel.NotificationPulsed += (s, e) => Dispatcher.InvokeAsync(PlayNotificationPulse);

        SettingsService.Instance.SettingsChanged += OnSettingsChanged;

        Loaded += OnLoaded;
        Microsoft.Win32.SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var hWnd = new WindowInteropHelper(this).Handle;
        WindowHelper.ApplyNoActivateAndToolWindow(this);
        _fullscreenDetector.SetWindowHandle(hWnd);
        _fullscreenDetector.Start();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        CenterOnWorkArea();
        ApplyScale(SettingsService.Instance.CurrentSettings.IslandScale);
        ApplyState(_viewModel.CurrentState, animate: false);
        UpdateEqualizerAnimation(_viewModel.Media.IsPlaying);
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs e)
    {
        Dispatcher.InvokeAsync(CenterOnWorkArea);
    }

    private void OnSettingsChanged(AppSettings settings)
    {
        Dispatcher.InvokeAsync(() =>
        {
            CenterOnWorkArea();
            ApplyScale(settings.IslandScale);
        });
    }

    private void ApplyScale(double scale)
    {
        if (scale < 0.5) scale = 1.0;
        IslandBorder.LayoutTransform = new ScaleTransform(scale, scale);
    }

    public void ResetPosition()
    {
        CenterOnWorkArea();
        IslandBorder.Visibility = Visibility.Visible;
        IslandBorder.Opacity = 1;
        ApplyState(_viewModel.CurrentState, animate: true);
    }

    private void CenterOnWorkArea()
    {
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Left + (workArea.Width - Width) / 2;
        Top = workArea.Top + 6 + SettingsService.Instance.CurrentSettings.VerticalOffset;
    }

    private void OnFullscreenChanged(object? sender, bool isFullscreen)
    {
        Dispatcher.InvokeAsync(() =>
        {
            if (isFullscreen)
            {
                var fadeOut = new DoubleAnimation(0, TimeSpan.FromMilliseconds(200));
                fadeOut.Completed += (s, e) =>
                {
                    IslandBorder.Visibility = Visibility.Collapsed;
                };
                IslandBorder.BeginAnimation(OpacityProperty, fadeOut);
            }
            else
            {
                IslandBorder.Visibility = Visibility.Visible;
                var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(250))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                IslandBorder.BeginAnimation(OpacityProperty, fadeIn);
            }
        });
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!App.IsShuttingDown)
        {
            e.Cancel = true;
            return;
        }

        _fullscreenDetector.Dispose();
        base.OnClosing(e);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.CurrentState))
        {
            Dispatcher.InvokeAsync(() => ApplyState(_viewModel.CurrentState, animate: true));
        }
    }

    private void OnMediaPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(_viewModel.Media.IsPlaying))
        {
            Dispatcher.InvokeAsync(() => UpdateEqualizerAnimation(_viewModel.Media.IsPlaying));
        }
    }

    private void ApplyState(IslandState state, bool animate)
    {
        double targetWidth;
        double targetHeight;
        double targetRadius;
        FrameworkElement targetView;
        var viewsToHide = new List<FrameworkElement>();

        switch (state)
        {
            case IslandState.Notification:
                targetWidth = 360;
                targetHeight = 48;
                targetRadius = 24;
                targetView = NotificationView;
                viewsToHide.Add(IdleView);
                viewsToHide.Add(MusicCompactView);
                viewsToHide.Add(ExpandedView);
                break;

            case IslandState.MusicPlaying:
                targetWidth = 280;
                targetHeight = 42;
                targetRadius = 21;
                targetView = MusicCompactView;
                viewsToHide.Add(IdleView);
                viewsToHide.Add(ExpandedView);
                viewsToHide.Add(NotificationView);
                break;

            case IslandState.Expanded:
                targetWidth = 360;
                targetHeight = 148;
                targetRadius = 26;
                targetView = ExpandedView;
                viewsToHide.Add(IdleView);
                viewsToHide.Add(MusicCompactView);
                viewsToHide.Add(NotificationView);
                break;

            case IslandState.Idle:
            default:
                targetWidth = 130;
                targetHeight = 32;
                targetRadius = 16;
                targetView = IdleView;
                viewsToHide.Add(MusicCompactView);
                viewsToHide.Add(ExpandedView);
                viewsToHide.Add(NotificationView);
                break;
        }

        if (!animate || !IsLoaded)
        {
            IslandBorder.Width = targetWidth;
            IslandBorder.Height = targetHeight;
            IslandBorder.CornerRadius = new CornerRadius(targetRadius);
            targetView.Visibility = Visibility.Visible;
            targetView.Opacity = 1;
            foreach (var view in viewsToHide)
            {
                view.Visibility = Visibility.Collapsed;
                view.Opacity = 0;
            }
            return;
        }

        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        var duration = TimeSpan.FromMilliseconds(320);

        // Animación de ancho y alto
        var widthAnim = new DoubleAnimation(targetWidth, duration) { EasingFunction = ease };
        IslandBorder.BeginAnimation(WidthProperty, widthAnim);

        var heightAnim = new DoubleAnimation(targetHeight, duration) { EasingFunction = ease };
        IslandBorder.BeginAnimation(HeightProperty, heightAnim);

        IslandBorder.CornerRadius = new CornerRadius(targetRadius);

        // Desvanecer vistas previas
        foreach (var view in viewsToHide)
        {
            if (view.Visibility == Visibility.Visible)
            {
                var fadeOut = new DoubleAnimation(0, TimeSpan.FromMilliseconds(140));
                fadeOut.Completed += (s, e) => { view.Visibility = Visibility.Collapsed; };
                view.BeginAnimation(OpacityProperty, fadeOut);
            }
        }

        // Mostrar y desvanecer hacia adentro vista objetivo
        targetView.Visibility = Visibility.Visible;
        var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(240))
        {
            BeginTime = TimeSpan.FromMilliseconds(80),
            EasingFunction = ease
        };
        targetView.BeginAnimation(OpacityProperty, fadeIn);
    }

    private void PlayNotificationPulse()
    {
        var bounce = new DoubleAnimationUsingKeyFrames();
        bounce.KeyFrames.Add(new SplineDoubleKeyFrame(360, KeyTime.FromTimeSpan(TimeSpan.Zero)));
        bounce.KeyFrames.Add(new SplineDoubleKeyFrame(374, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(100))));
        bounce.KeyFrames.Add(new SplineDoubleKeyFrame(360, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(250))));
        IslandBorder.BeginAnimation(WidthProperty, bounce);

        var flash = new DoubleAnimationUsingKeyFrames();
        flash.KeyFrames.Add(new LinearDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
        flash.KeyFrames.Add(new LinearDoubleKeyFrame(0.4, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(100))));
        flash.KeyFrames.Add(new LinearDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(250))));
        NotificationView.BeginAnimation(OpacityProperty, flash);
    }

    private void UpdateEqualizerAnimation(bool isPlaying)
    {
        if (isPlaying)
        {
            StartBarAnimation(Bar1, 4, 14, 450);
            StartBarAnimation(Bar2, 8, 17, 360);
            StartBarAnimation(Bar3, 3, 13, 410);
            StartBarAnimation(Bar4, 6, 16, 330);
        }
        else
        {
            StopBarAnimation(Bar1, 4);
            StopBarAnimation(Bar2, 4);
            StopBarAnimation(Bar3, 4);
            StopBarAnimation(Bar4, 4);
        }
    }

    private void StartBarAnimation(Rectangle bar, double min, double max, int durationMs)
    {
        var anim = new DoubleAnimation(min, max, TimeSpan.FromMilliseconds(durationMs))
        {
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        };
        bar.BeginAnimation(HeightProperty, anim);
    }

    private void StopBarAnimation(Rectangle bar, double idleHeight)
    {
        var anim = new DoubleAnimation(idleHeight, TimeSpan.FromMilliseconds(200));
        bar.BeginAnimation(HeightProperty, anim);
    }

    private void IslandBorder_MouseEnter(object sender, MouseEventArgs e)
    {
        _viewModel.SetHoveredCommand.Execute(true);
    }

    private void IslandBorder_MouseLeave(object sender, MouseEventArgs e)
    {
        _viewModel.SetHoveredCommand.Execute(false);
    }

    private void IslandBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _viewModel.ToggleExpandCommand.Execute(null);
    }

    private void IslandBorder_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (!SettingsService.Instance.CurrentSettings.EnableMouseWheelVolume)
            return;

        e.Handled = true;
        bool isUp = e.Delta > 0;
        if (isUp)
        {
            VolumeHelper.VolumeUp();
        }
        else
        {
            VolumeHelper.VolumeDown();
        }

        ShowVolumeFeedback(isUp);
    }

    private void ShowVolumeFeedback(bool isUp)
    {
        VolumeText.Text = isUp ? "Volumen +" : "Volumen −";

        var fadeIn = new DoubleAnimation(1, TimeSpan.FromMilliseconds(100));
        VolumeFeedbackOverlay.BeginAnimation(OpacityProperty, fadeIn);

        _volumeFadeTimer?.Stop();
        _volumeFadeTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(1000)
        };
        _volumeFadeTimer.Tick += (s, e) =>
        {
            _volumeFadeTimer?.Stop();
            var fadeOut = new DoubleAnimation(0, TimeSpan.FromMilliseconds(300));
            VolumeFeedbackOverlay.BeginAnimation(OpacityProperty, fadeOut);
        };
        _volumeFadeTimer.Start();
    }

    private void MediaButton_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
    }
}
