using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Microsoft.Web.WebView2.Core;
using KickChatOverlay.Services;
using KickChatOverlay.ViewModels;

namespace KickChatOverlay.Views;

public partial class OverlayWindow : Window
{
    public OverlayWindow()
    {
        InitializeComponent();

        // Must be set BEFORE CoreWebView2 is initialized
        ChatWebView.DefaultBackgroundColor = System.Drawing.Color.Transparent;
        Environment.SetEnvironmentVariable("WEBVIEW2_DEFAULT_BACKGROUND_COLOR", "00000000");

        LocalizationService.Instance.PropertyChanged += (_, _) =>
        {
            UpdateLangButton();
        };

        var vm = (OverlayViewModel)DataContext;
        vm.BotrixUrlChanged += OnBotrixUrlChanged;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var hwnd = new WindowInteropHelper(this).Handle;
        EnablePerPixelTransparency(hwnd);
    }

    private static void EnablePerPixelTransparency(IntPtr hwnd)
    {
        // Make DWM render the window background fully transparent
        var accent = new AccentPolicy
        {
            AccentState = 2, // ACCENT_ENABLE_TRANSPARENTGRADIENT
            AccentFlags = 2, // MUST be 2 for GradientColor to take effect
            GradientColor = 0x00000000, // fully transparent (ABGR)
            AnimationId = 0
        };

        var accentSize = Marshal.SizeOf(accent);
        var accentPtr = Marshal.AllocHGlobal(accentSize);
        try
        {
            Marshal.StructureToPtr(accent, accentPtr, false);

            var data = new WindowCompositionAttributeData
            {
                Attribute = 19, // WCA_ACCENT_POLICY
                SizeOfData = accentSize,
                Data = accentPtr
            };

            SetWindowCompositionAttribute(hwnd, ref data);
        }
        finally
        {
            Marshal.FreeHGlobal(accentPtr);
        }

        // Tell WPF's DWM composition target to not paint a solid background
        var source = HwndSource.FromHwnd(hwnd);
        source.CompositionTarget.BackgroundColor = System.Windows.Media.Colors.Transparent;
    }

    private void OnBotrixUrlChanged(Uri url)
    {
        LoadingOverlay.Visibility = Visibility.Visible;
        LoadingUrlText.Text = url.Host + url.PathAndQuery;

        var vm = DataContext as OverlayViewModel;
        if (vm != null)
            vm.StatusText = $"{LocalizationService.Instance["StatusLoading"]} {url.Host}";
    }

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        _ = StartLoadingTimeoutAsync();
    }

    private async Task StartLoadingTimeoutAsync()
    {
        await Task.Delay(20000);
        await Dispatcher.InvokeAsync(() =>
        {
            if (LoadingOverlay.Visibility != Visibility.Visible) return;

            var vm = DataContext as OverlayViewModel;
            vm?.SetError(LocalizationService.Instance["StatusTimeout"]);
        });
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

    private void ChatWebView_CoreWebView2Ready(object sender, CoreWebView2InitializationCompletedEventArgs e)
    {
        var vm = DataContext as OverlayViewModel;
        if (vm == null) return;

        if (!e.IsSuccess)
        {
            vm.SetError(LocalizationService.Instance.Format("StatusBotrixError",
                e.InitializationException?.Message ?? "CoreWebView2 initialization failed"));
            return;
        }

        var core = ChatWebView.CoreWebView2;
        if (core == null) return;

        core.Settings.AreDevToolsEnabled = false;
        core.Settings.AreDefaultContextMenusEnabled = false;
        core.Settings.IsStatusBarEnabled = false;
        core.Settings.AreBrowserAcceleratorKeysEnabled = false;

        ChatWebView.DefaultBackgroundColor = System.Drawing.Color.Transparent;

        // Force transparent background on all pages loaded in WebView2
        core.AddScriptToExecuteOnDocumentCreatedAsync(
            "document.documentElement.style.backgroundColor = 'transparent'; " +
            "document.body.style.backgroundColor = 'transparent';");

        if (vm.BotrixUrl != null)
        {
            LoadingUrlText.Text = vm.BotrixUrl.Host + vm.BotrixUrl.PathAndQuery;
            vm.StatusText = $"{LocalizationService.Instance["StatusLoading"]} {vm.BotrixUrl.Host}";
        }
    }

    private void ChatWebView_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
    {
        LoadingOverlay.Visibility = Visibility.Visible;
    }

    private void ChatWebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        var vm = DataContext as OverlayViewModel;
        if (vm == null) return;

        LoadingOverlay.Visibility = Visibility.Collapsed;

        if (e.IsSuccess)
        {
            vm.SetConnected();
        }
        else
        {
            vm.SetError(LocalizationService.Instance.Format("StatusBotrixError",
                $"Navigation failed (HTTP {e.HttpStatusCode})"));
        }
    }

    #region Win32 DWM Transparency

    [StructLayout(LayoutKind.Sequential)]
    private struct AccentPolicy
    {
        public int AccentState;
        public int AccentFlags;
        public uint GradientColor;
        public int AnimationId;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WindowCompositionAttributeData
    {
        public int Attribute;
        public IntPtr Data;
        public int SizeOfData;
    }

    [DllImport("user32.dll")]
    private static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

    #endregion
}
