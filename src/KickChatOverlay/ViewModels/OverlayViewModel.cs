using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KickChatOverlay.Models;
using KickChatOverlay.Services;

namespace KickChatOverlay.ViewModels;

public partial class OverlayViewModel : ObservableObject
{
    public event Action<Uri>? BotrixUrlChanged;

    [ObservableProperty] private bool _isConnected;
    [ObservableProperty] private bool _isBorderVisible = true;
    [ObservableProperty] private bool _isWebViewReady;
    [ObservableProperty] private bool _isBotrixIdVisible;
    [ObservableProperty] private string _statusText = "";
    [ObservableProperty] private AppSettings _settings;
    [ObservableProperty] private Uri? _botrixUrl;
    private bool _hasError;

    public OverlayViewModel()
    {
        _settings = AppSettings.Load();

        LocalizationService.Instance.SetCulture(_settings.Language);
        UpdateStatusText("StatusLoading");

        LocalizationService.Instance.PropertyChanged += (_, _) =>
        {
            RefreshStatusText();
            Settings.Language = LocalizationService.Instance.CurrentLanguageCode;
            SaveSettings();
        };

        RebuildBotrixUrl();
    }

    private void RebuildBotrixUrl()
    {
        var url = BotrixUrlBuilder.Build(Settings);
        BotrixUrl = url;
        BotrixUrlChanged?.Invoke(url);
    }

    private void UpdateStatusText(string key, params object[] args)
    {
        StatusText = args.Length > 0
            ? LocalizationService.Instance.Format(key, args)
            : LocalizationService.Instance[key];
    }

    private void RefreshStatusText()
    {
        if (_hasError)
            return;

        if (IsConnected)
            UpdateStatusText("StatusConnected");
        else
            UpdateStatusText("StatusLoading");
    }

    public void SetConnected()
    {
        _hasError = false;
        IsConnected = true;
        IsWebViewReady = true;
        UpdateStatusText("StatusConnected");
    }

    public void SetError(string msg)
    {
        _hasError = true;
        IsConnected = false;
        StatusText = msg;
    }

    [RelayCommand]
    private void ToggleBorders()
    {
        IsBorderVisible = !IsBorderVisible;
    }

    [RelayCommand]
    private void ToggleBotrixIdVisibility()
    {
        IsBotrixIdVisible = !IsBotrixIdVisible;
    }

    public void ToggleLanguage()
    {
        var current = LocalizationService.Instance.CurrentLanguageCode;
        var next = current == "tr" ? "en" : "tr";
        LocalizationService.Instance.SetCulture(next);
    }

    public void OnSettingsUpdated()
    {
        _hasError = false;
        RebuildBotrixUrl();
        SaveSettings();
    }

    public void SaveSettings()
    {
        Settings.Save();
    }
}
