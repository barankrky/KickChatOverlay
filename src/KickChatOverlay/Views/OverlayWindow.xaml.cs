using System.Diagnostics;
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
    private LowLevelHookProc? _keyboardProc;
    private IntPtr _keyboardHookID;


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

        _keyboardProc = KeyboardHookCallback;
        Loaded += (_, _) => InstallKeyboardHook();
        Closed += (_, _) => RemoveKeyboardHook();
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
        HwndSource.FromHwnd(_windowHwnd)?.AddHook(WndProcHook);
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

    #region Keyboard Hook (Delete Toggle)

    private void InstallKeyboardHook()
    {
        using var curProcess = Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule;
        if (curModule == null || _keyboardProc == null)
            return;
        var hMod = GetModuleHandle(curModule.ModuleName);
        _keyboardHookID = SetWindowsHookEx(WH_KEYBOARD_LL, _keyboardProc, hMod, 0);
    }

    private void RemoveKeyboardHook()
    {
        if (_keyboardHookID != IntPtr.Zero)
            UnhookWindowsHookEx(_keyboardHookID);
    }

    private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && (wParam == WM_KEYDOWN || wParam == WM_SYSKEYDOWN))
        {
            var hookStruct = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
            if (hookStruct.vkCode == VK_DELETE)
            {
                Dispatcher.InvokeAsync(() =>
                {
                    var vm = DataContext as OverlayViewModel;
                    vm?.ToggleBordersCommand.Execute(null);
                });
            }
        }

        return CallNextHookEx(_keyboardHookID, nCode, wParam, lParam);
    }

    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int VK_DELETE = 0x2E;

    private delegate IntPtr LowLevelHookProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelHookProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [StructLayout(LayoutKind.Sequential)]
    private struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    #endregion

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

        CaptureWebView2HostHwnd();

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

    #region Click-Through

    private bool _isClickThrough;
    private IntPtr _webView2HostHwnd;

    private void SetClickThrough(bool enable)
    {
        if (_windowHwnd == IntPtr.Zero)
            return;

        _isClickThrough = enable;

        var exStyle = GetWindowLong(_windowHwnd, GWL_EXSTYLE);

        if (enable)
        {
            SetWindowLong(_windowHwnd, GWL_EXSTYLE, exStyle | WS_EX_LAYERED | WS_EX_TRANSPARENT);
            SetLayeredWindowAttributes(_windowHwnd, 0, 255, LWA_ALPHA);

            if (_webView2HostHwnd != IntPtr.Zero)
                EnableWindow(_webView2HostHwnd, false);
        }
        else
        {
            SetWindowLong(_windowHwnd, GWL_EXSTYLE, exStyle & ~(WS_EX_LAYERED | WS_EX_TRANSPARENT));

            if (_webView2HostHwnd != IntPtr.Zero)
                EnableWindow(_webView2HostHwnd, true);
        }

        SetWindowPos(_windowHwnd, IntPtr.Zero, 0, 0, 0, 0,
            SWP_FRAMECHANGED | SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
    }

    private void CaptureWebView2HostHwnd()
    {
        EnumChildWindows(_windowHwnd, (hwnd, _) =>
        {
            var sb = new System.Text.StringBuilder(256);
            GetClassName(hwnd, sb, 256);
            if (sb.ToString() == "Chrome_WidgetWin_0")
            {
                _webView2HostHwnd = GetParent(hwnd);
                return false;
            }
            return true;
        }, IntPtr.Zero);
    }

    private IntPtr WndProcHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_STYLECHANGING && wParam.ToInt32() == GWL_EXSTYLE && _isClickThrough)
        {
            var ss = Marshal.PtrToStructure<STYLESTRUCT>(lParam);
            ss.styleNew |= WS_EX_LAYERED | WS_EX_TRANSPARENT;
            Marshal.StructureToPtr(ss, lParam, false);
            handled = true;
        }
        return IntPtr.Zero;
    }

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_LAYERED = 0x00080000;
    private const uint LWA_ALPHA = 0x00000002;
    private const uint SWP_FRAMECHANGED = 0x0020;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const int WM_STYLECHANGING = 0x007C;

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    private static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnableWindow(IntPtr hWnd, bool bEnable);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumChildWindows(IntPtr hWndParent, EnumWindowProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern IntPtr GetParent(IntPtr hWnd);

    private delegate bool EnumWindowProc(IntPtr hWnd, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct STYLESTRUCT
    {
        public int styleOld;
        public int styleNew;
    }

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
