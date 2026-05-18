using System.Drawing;
using System.Windows;
using H.NotifyIcon.Core;
using KickChatOverlay.Services;
using KickChatOverlay.ViewModels;
using KickChatOverlay.Views;

namespace KickChatOverlay;

public partial class App : Application
{
    private TrayIconWithContextMenu? _trayIcon;
    private IntPtr _iconHandle;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        using var icon = CreateAppIcon();
        _iconHandle = icon.Handle;

        CreateTrayIcon();

        LocalizationService.Instance.PropertyChanged += (_, _) =>
        {
            Dispatcher.Invoke(CreateTrayIcon);
        };
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIcon?.Dispose();
        DestroyIcon(_iconHandle);
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
        _trayIcon.UpdateIcon(_iconHandle);
    }

    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    private static Icon CreateAppIcon()
    {
        using var bmp = new Bitmap(16, 16);
        using var g = Graphics.FromImage(bmp);
        using var font = new Font("Arial", 6, System.Drawing.FontStyle.Bold);
        g.Clear(Color.FromArgb(30, 30, 46));
        g.DrawString("KC", font, Brushes.White, 0, 2);
        var hIcon = bmp.GetHicon();
        var icon = Icon.FromHandle(hIcon);
        var clonedIcon = (Icon)icon.Clone();
        icon.Dispose();
        DestroyIcon(hIcon);
        return clonedIcon;
    }

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

        menu.Items.Add(new PopupMenuItem(loc["MenuClearChat"], (_, _) =>
            Dispatcher.Invoke(() => GetViewModel()?.ClearChatCommand.Execute(null))));

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
