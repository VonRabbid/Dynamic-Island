using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media.Animation;
using DynamicIsland.ViewModels;
using UserControl = System.Windows.Controls.UserControl;
using Rectangle = System.Windows.Shapes.Rectangle;

namespace DynamicIsland.Views.Controls;

public partial class MusicCompactControl : UserControl
{
    private MainViewModel? _viewModel;

    public MusicCompactControl()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Loaded += OnLoaded;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is MainViewModel oldVm)
        {
            oldVm.Media.PropertyChanged -= OnMediaPropertyChanged;
        }

        if (e.NewValue is MainViewModel newVm)
        {
            _viewModel = newVm;
            _viewModel.Media.PropertyChanged += OnMediaPropertyChanged;
            UpdateAnimation(_viewModel.Media.IsPlaying);
        }
        else
        {
            _viewModel = null;
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_viewModel != null)
        {
            UpdateAnimation(_viewModel.Media.IsPlaying);
        }
    }

    private void OnMediaPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.Media.IsPlaying) && _viewModel != null)
        {
            Dispatcher.InvokeAsync(() => UpdateAnimation(_viewModel.Media.IsPlaying));
        }
    }

    public void UpdateAnimation(bool isPlaying)
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

    private static void StartBarAnimation(Rectangle bar, double min, double max, int durationMs)
    {
        var anim = new DoubleAnimation(min, max, TimeSpan.FromMilliseconds(durationMs))
        {
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        };
        bar.BeginAnimation(HeightProperty, anim);
    }

    private static void StopBarAnimation(Rectangle bar, double idleHeight)
    {
        var anim = new DoubleAnimation(idleHeight, TimeSpan.FromMilliseconds(200));
        bar.BeginAnimation(HeightProperty, anim);
    }
}
