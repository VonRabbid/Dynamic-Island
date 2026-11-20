using System.ComponentModel;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using DynamicIsland.ViewModels;
using UserControl = System.Windows.Controls.UserControl;

namespace DynamicIsland.Views.Controls;

public partial class ExpandedPlayerControl : UserControl
{
    private MainViewModel? _viewModel;
    private bool _isDraggingTimeline;
    private bool _isMouseDownOnTimeline;

    public ExpandedPlayerControl()
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
        }
        else
        {
            _viewModel = null;
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        TimelineSlider.AddHandler(Thumb.DragStartedEvent, new DragStartedEventHandler(OnTimelineDragStarted));
        TimelineSlider.AddHandler(Thumb.DragCompletedEvent, new DragCompletedEventHandler(OnTimelineDragCompleted));
        TimelineSlider.PreviewMouseLeftButtonDown += OnTimelinePreviewMouseDown;
        TimelineSlider.PreviewMouseLeftButtonUp += OnTimelinePreviewMouseUp;
        TimelineSlider.ValueChanged += OnTimelineValueChanged;
    }

    private void OnMediaPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.Media.ProgressPercentage))
        {
            if (!_isDraggingTimeline && !_isMouseDownOnTimeline && _viewModel != null)
            {
                Dispatcher.InvokeAsync(() =>
                {
                    if (!_isDraggingTimeline && !_isMouseDownOnTimeline && _viewModel != null)
                    {
                        TimelineSlider.Value = _viewModel.Media.ProgressPercentage;
                    }
                });
            }
        }
    }

    private void OnTimelineDragStarted(object sender, DragStartedEventArgs e)
    {
        _isDraggingTimeline = true;
        if (_viewModel != null)
        {
            _viewModel.Media.IsSeeking = true;
        }
    }

    private void OnTimelineDragCompleted(object sender, DragCompletedEventArgs e)
    {
        _isDraggingTimeline = false;
        if (_viewModel != null)
        {
            _ = _viewModel.Media.SeekToAsync(TimelineSlider.Value);
        }
    }

    private void OnTimelinePreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        _isMouseDownOnTimeline = true;
        if (_viewModel != null)
        {
            _viewModel.Media.IsSeeking = true;
        }
    }

    private void OnTimelinePreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_isMouseDownOnTimeline)
        {
            _isMouseDownOnTimeline = false;
            if (!_isDraggingTimeline && _viewModel != null)
            {
                _ = _viewModel.Media.SeekToAsync(TimelineSlider.Value);
            }
        }
    }

    private void OnTimelineValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if ((_isDraggingTimeline || _isMouseDownOnTimeline) && _viewModel != null)
        {
            _viewModel.Media.PreviewSeek(TimelineSlider.Value);
        }
    }

    private void MediaButton_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
    }
}
