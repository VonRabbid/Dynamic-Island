using System.Windows;
using System.Windows.Input;
using DynamicIsland.ViewModels;

namespace DynamicIsland.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
        var viewModel = new SettingsViewModel();
        viewModel.RequestClose += Close;
        DataContext = viewModel;
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }
}
