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
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Windows.Point;
using Cursors = System.Windows.Input.Cursors;

namespace DynamicIsland.Views;

public partial class MainWindow : Window, IDisposable
{
    private readonly MainViewModel _viewModel;
    private readonly FullscreenDetector _fullscreenDetector;
    private DispatcherTimer? _collapseTimer;
    private FrameworkElement? _currentTargetView;
    private bool _isDragging;
    private Point _dragStartCursorPos;
    private double _anchorCenterX;
    private double _anchorTop;

    public MainWindow()
    {
        InitializeComponent();

        _viewModel = new MainViewModel();
        DataContext = _viewModel;

        _fullscreenDetector = new FullscreenDetector();
        _fullscreenDetector.FullscreenChanged += OnFullscreenChanged;

        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        _viewModel.ColorsChanged += OnViewModelColorsChanged;
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
        _viewModel.UpdateOpacity(SettingsService.Instance.CurrentSettings.IslandOpacity);
        GradientStop1.Color = _viewModel.BackgroundStartColor;
        GradientStop2.Color = _viewModel.BackgroundEndColor;

        ApplyState(_viewModel.CurrentState, animate: false);
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
            _viewModel.UpdateOpacity(settings.IslandOpacity);
        });
    }

    private void ApplyScale(double scale)
    {
        if (scale < 0.5) scale = 1.0;
        IslandBorder.LayoutTransform = new ScaleTransform(scale, scale);
    }

    public void ResetPosition()
    {
        ResetToDefaultPosition();
        IslandBorder.Visibility = Visibility.Visible;
        IslandBorder.Opacity = 1;
        ApplyState(_viewModel.CurrentState, animate: true);
    }

    public void ResetToDefaultPosition()
    {
        var settings = SettingsService.Instance.CurrentSettings;
        settings.UseCustomPosition = false;
        settings.CustomPositionX = -1;
        settings.CustomPositionY = -1;
        SettingsService.Instance.Save(settings);

        var screen = ScreenService.GetScreen(settings.SelectedMonitorDeviceName);
        var workingArea = screen.WorkingArea;

        double dpiScaleX = 1.0;
        double dpiScaleY = 1.0;
        var source = PresentationSource.FromVisual(this);
        if (source?.CompositionTarget != null)
        {
            dpiScaleX = source.CompositionTarget.TransformToDevice.M11;
            dpiScaleY = source.CompositionTarget.TransformToDevice.M22;
        }

        double workLeft = workingArea.Left / dpiScaleX;
        double workTop = workingArea.Top / dpiScaleY;
        double workWidth = workingArea.Width / dpiScaleX;

        double targetCenterX = workLeft + (workWidth / 2);
        double targetTop = workTop + 6 + settings.VerticalOffset;
        double winWidth = ActualWidth > 0 ? ActualWidth : Width;
        double targetLeft = targetCenterX - (winWidth / 2);

        _anchorCenterX = targetCenterX;
        _anchorTop = targetTop;

        var duration = TimeSpan.FromMilliseconds(300);
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };

        var animLeft = new DoubleAnimation(targetLeft, duration) { EasingFunction = ease };
        var animTop = new DoubleAnimation(targetTop, duration) { EasingFunction = ease };

        animLeft.Completed += (s, e) =>
        {
            BeginAnimation(LeftProperty, null);
            Left = targetLeft;
        };
        animTop.Completed += (s, e) =>
        {
            BeginAnimation(TopProperty, null);
            Top = targetTop;
        };

        BeginAnimation(LeftProperty, animLeft);
        BeginAnimation(TopProperty, animTop);
    }

    private void CenterOnWorkArea()
    {
        var settings = SettingsService.Instance.CurrentSettings;
        if (settings.UseCustomPosition && settings.CustomPositionX >= 0 && settings.CustomPositionY >= 0)
        {
            _anchorCenterX = settings.CustomPositionX;
            _anchorTop = settings.CustomPositionY;
        }
        else
        {
            var screen = ScreenService.GetScreen(settings.SelectedMonitorDeviceName);
            var workingArea = screen.WorkingArea;

            double dpiScaleX = 1.0;
            double dpiScaleY = 1.0;
            var source = PresentationSource.FromVisual(this);
            if (source?.CompositionTarget != null)
            {
                dpiScaleX = source.CompositionTarget.TransformToDevice.M11;
                dpiScaleY = source.CompositionTarget.TransformToDevice.M22;
            }

            double workLeft = workingArea.Left / dpiScaleX;
            double workTop = workingArea.Top / dpiScaleY;
            double workWidth = workingArea.Width / dpiScaleX;

            _anchorCenterX = workLeft + (workWidth / 2);
            _anchorTop = workTop + 6 + settings.VerticalOffset;
        }

        UpdateWindowPosition();
    }

    private void UpdateWindowPosition()
    {
        double winWidth = ActualWidth > 0 ? ActualWidth : Width;
        Left = _anchorCenterX - (winWidth / 2);
        Top = _anchorTop;
    }

    private void OnFullscreenChanged(object? sender, bool isFullscreen)
    {
        Dispatcher.InvokeAsync(() =>
        {
            _viewModel.SetFullscreen(isFullscreen);

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

        Dispose();
        base.OnClosing(e);
    }

    public void Dispose()
    {
        _collapseTimer?.Stop();
        VolumeFeedbackOverlay.Dispose();
        _fullscreenDetector.Dispose();
        GC.SuppressFinalize(this);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.CurrentState))
        {
            Dispatcher.InvokeAsync(() => ApplyState(_viewModel.CurrentState, animate: true));
        }
    }

    private void ApplyState(IslandState state, bool animate)
    {
        UpdateWindowPosition();

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
                viewsToHide.Add(PowerAlertView);
                viewsToHide.Add(CapsLockAlertView);
                break;

            case IslandState.PowerAlert:
                targetWidth = 240;
                targetHeight = 38;
                targetRadius = 19;
                targetView = PowerAlertView;
                viewsToHide.Add(IdleView);
                viewsToHide.Add(MusicCompactView);
                viewsToHide.Add(ExpandedView);
                viewsToHide.Add(NotificationView);
                viewsToHide.Add(CapsLockAlertView);
                break;

            case IslandState.CapsLockAlert:
                targetWidth = 210;
                targetHeight = 34;
                targetRadius = 17;
                targetView = CapsLockAlertView;
                viewsToHide.Add(IdleView);
                viewsToHide.Add(MusicCompactView);
                viewsToHide.Add(ExpandedView);
                viewsToHide.Add(NotificationView);
                viewsToHide.Add(PowerAlertView);
                break;

            case IslandState.MusicPlaying:
                targetWidth = 54;
                targetHeight = 28;
                targetRadius = 14;
                targetView = MusicCompactView;
                viewsToHide.Add(IdleView);
                viewsToHide.Add(ExpandedView);
                viewsToHide.Add(NotificationView);
                viewsToHide.Add(PowerAlertView);
                viewsToHide.Add(CapsLockAlertView);
                break;

            case IslandState.Expanded:
                targetWidth = 360;
                targetHeight = 158;
                targetRadius = 26;
                targetView = ExpandedView;
                viewsToHide.Add(IdleView);
                viewsToHide.Add(MusicCompactView);
                viewsToHide.Add(NotificationView);
                viewsToHide.Add(PowerAlertView);
                viewsToHide.Add(CapsLockAlertView);
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
                viewsToHide.Add(PowerAlertView);
                viewsToHide.Add(CapsLockAlertView);
                break;
        }

        _currentTargetView = targetView;

        if (!animate || !IsLoaded)
        {
            IslandBorder.BeginAnimation(WidthProperty, null);
            IslandBorder.BeginAnimation(HeightProperty, null);
            IslandBorder.Width = targetWidth;
            IslandBorder.Height = targetHeight;
            IslandBorder.CornerRadius = new CornerRadius(targetRadius);

            targetView.BeginAnimation(OpacityProperty, null);
            targetView.Visibility = Visibility.Visible;
            targetView.Opacity = 1;

            foreach (var view in viewsToHide)
            {
                view.BeginAnimation(OpacityProperty, null);
                view.Visibility = Visibility.Collapsed;
                view.Opacity = 0;
            }
            return;
        }

        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        var duration = TimeSpan.FromMilliseconds(300);

        // Animación de ancho y alto de la cápsula
        var widthAnim = new DoubleAnimation(targetWidth, duration) { EasingFunction = ease };
        IslandBorder.BeginAnimation(WidthProperty, widthAnim);

        var heightAnim = new DoubleAnimation(targetHeight, duration) { EasingFunction = ease };
        IslandBorder.BeginAnimation(HeightProperty, heightAnim);

        IslandBorder.CornerRadius = new CornerRadius(targetRadius);

        // Mostrar de inmediato la vista objetivo y asegurar transición fluida
        targetView.Visibility = Visibility.Visible;
        targetView.BeginAnimation(OpacityProperty, null);

        var fadeIn = new DoubleAnimation(1.0, TimeSpan.FromMilliseconds(220))
        {
            EasingFunction = ease
        };
        fadeIn.Completed += (s, e) =>
        {
            if (_currentTargetView == targetView)
            {
                targetView.Opacity = 1.0;
            }
        };
        targetView.BeginAnimation(OpacityProperty, fadeIn);

        // Desvanecer vistas previas
        foreach (var view in viewsToHide)
        {
            if (view.Visibility == Visibility.Visible)
            {
                var fadeOut = new DoubleAnimation(0.0, TimeSpan.FromMilliseconds(120));
                var viewCapture = view;
                fadeOut.Completed += (s, e) =>
                {
                    if (viewCapture != _currentTargetView)
                    {
                        viewCapture.Visibility = Visibility.Collapsed;
                        viewCapture.Opacity = 0;
                    }
                };
                view.BeginAnimation(OpacityProperty, fadeOut);
            }
            else
            {
                view.BeginAnimation(OpacityProperty, null);
                view.Visibility = Visibility.Collapsed;
                view.Opacity = 0;
            }
        }
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

    private void IslandBorder_MouseEnter(object sender, MouseEventArgs e)
    {
        if (_isDragging) return;

        Focus();
        _collapseTimer?.Stop();

        var pos = Mouse.GetPosition(IslandBorder);
        var bounds = new Rect(0, 0, IslandBorder.ActualWidth, IslandBorder.ActualHeight);
        if (bounds.Contains(pos))
        {
            _viewModel.IsHovered = true;
        }
    }

    private void IslandBorder_MouseMove(object sender, MouseEventArgs e)
    {
        if (_isDragging) return;

        if (_collapseTimer != null && _collapseTimer.IsEnabled)
        {
            var pos = Mouse.GetPosition(IslandBorder);
            var bounds = new Rect(0, 0, IslandBorder.ActualWidth, IslandBorder.ActualHeight);
            if (bounds.Contains(pos))
            {
                _collapseTimer.Stop();
                if (!_viewModel.IsHovered)
                {
                    _viewModel.IsHovered = true;
                }
            }
        }
    }

    private void IslandBorder_MouseLeave(object sender, MouseEventArgs e)
    {
        if (_isDragging) return;

        _collapseTimer?.Stop();
        _collapseTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(250)
        };
        _collapseTimer.Tick += (s, args) =>
        {
            _collapseTimer.Stop();

            var pos = Mouse.GetPosition(IslandBorder);
            var bounds = new Rect(0, 0, IslandBorder.ActualWidth, IslandBorder.ActualHeight);
            if (!bounds.Contains(pos))
            {
                _viewModel.IsHovered = false;
            }
        };
        _collapseTimer.Start();
    }

    private void IslandBorder_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        Focus();

        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && e.LeftButton == MouseButtonState.Pressed)
        {
            _isDragging = true;
            _dragStartCursorPos = PointToScreen(e.GetPosition(this));
            Mouse.Capture((IInputElement)sender);
            Mouse.OverrideCursor = Cursors.SizeAll;
            e.Handled = true;
        }
    }

    private void IslandBorder_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (_isDragging)
        {
            var currentCursorPos = PointToScreen(e.GetPosition(this));

            double dpiScaleX = 1.0;
            double dpiScaleY = 1.0;
            var source = PresentationSource.FromVisual(this);
            if (source?.CompositionTarget != null)
            {
                dpiScaleX = source.CompositionTarget.TransformToDevice.M11;
                dpiScaleY = source.CompositionTarget.TransformToDevice.M22;
            }

            double deltaX = (currentCursorPos.X - _dragStartCursorPos.X) / dpiScaleX;
            double deltaY = (currentCursorPos.Y - _dragStartCursorPos.Y) / dpiScaleY;

            _anchorCenterX += deltaX;
            _anchorTop += deltaY;

            UpdateWindowPosition();

            _dragStartCursorPos = currentCursorPos;
            e.Handled = true;
        }
    }

    private void IslandBorder_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_isDragging)
        {
            _isDragging = false;
            Mouse.Capture(null);
            Mouse.OverrideCursor = null;

            var settings = SettingsService.Instance.CurrentSettings;
            settings.UseCustomPosition = true;
            settings.CustomPositionX = _anchorCenterX;
            settings.CustomPositionY = _anchorTop;
            SettingsService.Instance.Save(settings);

            e.Handled = true;
        }
    }

    private void IslandBorder_LostMouseCapture(object sender, MouseEventArgs e)
    {
        if (_isDragging)
        {
            _isDragging = false;
            Mouse.OverrideCursor = null;
        }
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

        VolumeFeedbackOverlay.ShowFeedback(isUp);
    }

    private void OnViewModelColorsChanged(object? sender, EventArgs e)
    {
        Dispatcher.InvokeAsync(() =>
        {
            var duration = new Duration(TimeSpan.FromMilliseconds(480));
            var easing = new CubicEase { EasingMode = EasingMode.EaseOut };

            var anim1 = new ColorAnimation
            {
                To = _viewModel.BackgroundStartColor,
                Duration = duration,
                EasingFunction = easing
            };

            var anim2 = new ColorAnimation
            {
                To = _viewModel.BackgroundEndColor,
                Duration = duration,
                EasingFunction = easing
            };

            GradientStop1.BeginAnimation(GradientStop.ColorProperty, anim1);
            GradientStop2.BeginAnimation(GradientStop.ColorProperty, anim2);
        });
    }
}
