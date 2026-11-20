using System;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using UserControl = System.Windows.Controls.UserControl;

namespace DynamicIsland.Views.Controls;

public partial class VolumeOverlayControl : UserControl, IDisposable
{
    private DispatcherTimer? _volumeFadeTimer;

    public VolumeOverlayControl()
    {
        InitializeComponent();
        Unloaded += (s, e) => Dispose();
    }

    public void ShowFeedback(bool isUp)
    {
        VolumeText.Text = isUp ? "Volumen +" : "Volumen −";

        var fadeIn = new DoubleAnimation(1, TimeSpan.FromMilliseconds(100));
        VolumeBorder.BeginAnimation(OpacityProperty, fadeIn);

        _volumeFadeTimer?.Stop();
        _volumeFadeTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(1000)
        };
        _volumeFadeTimer.Tick += (s, e) =>
        {
            _volumeFadeTimer?.Stop();
            var fadeOut = new DoubleAnimation(0, TimeSpan.FromMilliseconds(300));
            VolumeBorder.BeginAnimation(OpacityProperty, fadeOut);
        };
        _volumeFadeTimer.Start();
    }

    public void Dispose()
    {
        _volumeFadeTimer?.Stop();
        _volumeFadeTimer = null;
        GC.SuppressFinalize(this);
    }
}
