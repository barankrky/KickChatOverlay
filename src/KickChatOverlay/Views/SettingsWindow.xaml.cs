using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Navigation;
using KickChatOverlay.ViewModels;

namespace KickChatOverlay.Views;

public partial class SettingsWindow : Window
{
    private OverlayViewModel? _vm;

    public SettingsWindow()
    {
        InitializeComponent();
        VersionText.Text = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0.0";
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, DependencyPropertyChangedEventArgs e)
    {
        if (_vm != null)
            _vm.PropertyChanged -= OnViewModelPropertyChanged;

        _vm = DataContext as OverlayViewModel;
        if (_vm == null)
            return;

        _vm.PropertyChanged += OnViewModelPropertyChanged;
        BotrixPasswordBox.Password = _vm.Settings.BotrixBotId;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(OverlayViewModel.IsBotrixIdVisible) && !_vm!.IsBotrixIdVisible)
            BotrixPasswordBox.Password = _vm.Settings.BotrixBotId;
    }

    private void BotrixPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (_vm != null)
            _vm.Settings.BotrixBotId = BotrixPasswordBox.Password;
    }

    private void SaveClose_Click(object sender, RoutedEventArgs e)
    {
        if (_vm == null)
            return;
        _vm.OnSettingsUpdated();
        Close();
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }
}
