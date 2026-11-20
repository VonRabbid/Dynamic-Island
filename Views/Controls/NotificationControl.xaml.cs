using System.Windows;
using UserControl = System.Windows.Controls.UserControl;

namespace DynamicIsland.Views.Controls;

public partial class NotificationControl : UserControl
{
    public NotificationControl()
    {
        InitializeComponent();
    }

    private void DismissButton_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
    }
}
