using System.Windows;
using KickChatOverlay.ViewModels;

namespace KickChatOverlay.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
    }

    private void SaveClose_Click(object sender, RoutedEventArgs e)
    {
        var vm = (OverlayViewModel)DataContext;
        vm.OnSettingsUpdated();
        Close();
    }
}
