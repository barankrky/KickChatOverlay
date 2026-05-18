using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using KickChatOverlay.Services;
using KickChatOverlay.ViewModels;

namespace KickChatOverlay.Views;

public partial class OverlayWindow : Window
{
    public OverlayWindow()
    {
        InitializeComponent();

        var vm = (OverlayViewModel)DataContext;
        vm.Messages.CollectionChanged += (_, _) =>
        {
            Dispatcher.InvokeAsync(() =>
            {
                if (ChatScrollViewer != null)
                    ChatScrollViewer.ScrollToEnd();
            }, System.Windows.Threading.DispatcherPriority.Loaded);
        };

        LocalizationService.Instance.PropertyChanged += (_, _) =>
        {
            UpdateLangButton();
        };
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 1)
            DragMove();
    }

    private void ResizeGrip_DragDelta(object sender, DragDeltaEventArgs e)
    {
        var newWidth = Width + e.HorizontalChange;
        var newHeight = Height + e.VerticalChange;
        if (newWidth >= 200) Width = newWidth;
        if (newHeight >= 150) Height = newHeight;
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        var settingsWindow = new SettingsWindow
        {
            DataContext = DataContext,
            Owner = this
        };
        settingsWindow.ShowDialog();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        var vm = (OverlayViewModel)DataContext;
        vm.SaveSettings();
        Application.Current.Shutdown();
    }

    private void LangButton_Click(object sender, RoutedEventArgs e)
    {
        var vm = (OverlayViewModel)DataContext;
        vm.ToggleLanguage();
    }

    private void UpdateLangButton()
    {
        var lang = LocalizationService.Instance.CurrentLanguageCode;
        var flag = lang == "tr" ? "tr" : "us";
        LangFlagImage.Source = new BitmapImage(new Uri($"pack://application:,,,/Resources/flag-{flag}.png"));
    }

    protected override void OnLocationChanged(EventArgs e)
    {
        base.OnLocationChanged(e);
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
    }

    private void ComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {

    }
}
