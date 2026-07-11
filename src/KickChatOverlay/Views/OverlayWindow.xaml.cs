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
    private IntPtr _windowHwnd;

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
        vm.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(OverlayViewModel.IsBorderVisible))
            return;

        var vm = (OverlayViewModel)DataContext;
        SetClickThrough(!vm.IsBorderVisible);
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        _windowHwnd = new WindowInteropHelper(this).Handle;
        EnablePerPixelTransparency(_windowHwnd);
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

    #region Click-Through (WS_EX_TRANSPARENT)

    private void SetClickThrough(bool enable)
    {
        if (_windowHwnd == IntPtr.Zero)
            return;

        // Apply to main window
        var exStyle = GetWindowLong(_windowHwnd, GWL_EXSTYLE);
        SetWindowLong(_windowHwnd, GWL_EXSTYLE, enable ? exStyle | WS_EX_TRANSPARENT : exStyle & ~WS_EX_TRANSPARENT);

        // Apply recursively to all child HWNDs (including WebView2's Chromium windows)
        EnumChildWindows(_windowHwnd, (childHwnd, _) =>
        {
            var childEx = GetWindowLong(childHwnd, GWL_EXSTYLE);
            SetWindowLong(childHwnd, GWL_EXSTYLE, enable ? childEx | WS_EX_TRANSPARENT : childEx & ~WS_EX_TRANSPARENT);
            return true;
        }, IntPtr.Zero);

        // Force frame update so hit-test boundaries take effect immediately
        SetWindowPos(_windowHwnd, IntPtr.Zero, 0, 0, 0, 0,
            SWP_FRAMECHANGED | SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
    }

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const uint SWP_FRAMECHANGED = 0x0020;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_NOACTIVATE = 0x0010;

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    private static extern bool EnumChildWindows(IntPtr hWndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    #endregion

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
