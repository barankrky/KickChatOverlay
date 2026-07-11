using System.Drawing;
using System.Reflection;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using H.NotifyIcon.Core;
using KickChatOverlay.Services;
using KickChatOverlay.ViewModels;
using KickChatOverlay.Views;

namespace KickChatOverlay;

public partial class App : Application
{
    private TrayIconWithContextMenu? _trayIcon;
    private Icon? _appIcon;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        CheckWebView2Runtime();

        _appIcon = LoadAppIcon();
        CreateTrayIcon();

        LocalizationService.Instance.PropertyChanged += (_, _) =>
        {
            Dispatcher.Invoke(CreateTrayIcon);
        };
    }

    private static void CheckWebView2Runtime()
    {
        try
        {
            var _ = CoreWebView2Environment.GetAvailableBrowserVersionString();
        }
        catch
        {
            var loc = LocalizationService.Instance;
            System.Windows.MessageBox.Show(
                loc["StatusWebviewError"],
                "WebView2 Required",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIcon?.Dispose();
        _appIcon?.Dispose();
        base.OnExit(e);
    }

    private void CreateTrayIcon()
    {
        _trayIcon?.Dispose();

        _trayIcon = new TrayIconWithContextMenu
        {
            ContextMenu = CreateContextMenu()
        };
        _trayIcon.Create();
        _trayIcon.UpdateToolTip(LocalizationService.Instance["TrayToolTip"]);
        _trayIcon.UpdateIcon(_appIcon!.Handle);
    }

    private static Icon LoadAppIcon()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("KickChatOverlay.Resources.kick.ico");
        return stream != null ? new Icon(stream) : CreateFallbackIcon();
    }

    private static Icon CreateFallbackIcon()
    {
        using var bmp = new Bitmap(16, 16);
        using var g = Graphics.FromImage(bmp);
        using var font = new Font("Arial", 6, System.Drawing.FontStyle.Bold);
        g.Clear(Color.FromArgb(30, 30, 46));
        g.DrawString("KC", font, Brushes.White, 0, 2);
        var hIcon = bmp.GetHicon();
        var icon = Icon.FromHandle(hIcon);
        var cloned = (Icon)icon.Clone();
        icon.Dispose();
        DestroyIcon(hIcon);
        return cloned;
    }

    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    private PopupMenu CreateContextMenu()
    {
        var loc = LocalizationService.Instance;
        var menu = new PopupMenu();

        menu.Items.Add(new PopupMenuItem(loc["MenuShowSettings"], (_, _) => Dispatcher.Invoke(TrayShowSettings)));

        menu.Items.Add(new PopupMenuItem(loc["MenuToggleBorders"], (_, _) =>
            Dispatcher.Invoke(() => GetViewModel()?.ToggleBordersCommand.Execute(null))));

        menu.Items.Add(new PopupMenuItem(loc["MenuResetPosition"], (_, _) =>
            Dispatcher.Invoke(() =>
            {
                var vm = GetViewModel();
                if (vm == null) return;
                vm.Settings.WindowLeft = 100;
                vm.Settings.WindowTop = 100;
                vm.Settings.WindowWidth = 350;
                vm.Settings.WindowHeight = 600;
            })));

        menu.Items.Add(new PopupMenuSeparator());

        menu.Items.Add(new PopupMenuItem(loc["MenuExit"], (_, _) =>
            Dispatcher.Invoke(() =>
            {
                GetViewModel()?.SaveSettings();
                Shutdown();
            })));

        return menu;
    }

    private OverlayViewModel? GetViewModel()
        => (MainWindow as OverlayWindow)?.DataContext as OverlayViewModel;

    private void TrayShowSettings()
    {
        if (MainWindow is OverlayWindow overlay)
        {
            var settings = new SettingsWindow
            {
                DataContext = overlay.DataContext,
                Owner = overlay
            };
            settings.ShowDialog();
        }
    }
}
