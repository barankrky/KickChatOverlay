using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KickChatOverlay.Models;
using KickChatOverlay.Services;

namespace KickChatOverlay.ViewModels;

public partial class OverlayViewModel : ObservableObject
{
    private readonly KickChatService _kickService = new();
    private readonly SoundService _soundService = new();
    private readonly HashSet<string> _recentMessageIds = new();

    public ObservableCollection<ChatMessage> Messages { get; } = [];

    [ObservableProperty] private bool _isConnected;
    [ObservableProperty] private bool _isBorderVisible = true;
    [ObservableProperty] private string _statusText = "Disconnected";
    [ObservableProperty] private AppSettings _settings;
    private bool _hasError;

    public OverlayViewModel()
    {
        _settings = AppSettings.Load();
        _soundService.SetSound(_settings.NotificationSound);

        _kickService.OnMessageReceived += HandleMessage;
        _kickService.OnError += err =>
            Application.Current.Dispatcher.InvokeAsync(() => SetError($"Kick error: {err}"));
        _kickService.OnConnected += () =>
            Application.Current.Dispatcher.InvokeAsync(() => SetStatus("Kick connected"));
        _kickService.OnDisconnected += () =>
            Application.Current.Dispatcher.InvokeAsync(() => SetStatus("Kick disconnected"));
    }

    // Errors stick — non-error status won't overwrite an error
    private void SetError(string msg)
    {
        _hasError = true;
        StatusText = msg;
    }

    private void SetStatus(string msg)
    {
        if (!_hasError)
            StatusText = msg;
    }

    private void HandleMessage(ChatMessage msg)
    {
        if (!_recentMessageIds.Add(msg.Id))
            return;

        if (_recentMessageIds.Count > 500)
            _recentMessageIds.Clear();

        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            Messages.Add(msg);
            while (Messages.Count > Settings.MaxMessages)
                Messages.RemoveAt(0);
            _soundService.Play(Settings.NotificationVolume);
        });
    }

    [RelayCommand]
    private async Task ConnectAsync()
    {
        _hasError = false;

        if (string.IsNullOrWhiteSpace(Settings.KickUsername))
        {
            StatusText = "Enter a Kick username";
            return;
        }

        StatusText = "Connecting...";
        try
        {
            var manualId = string.IsNullOrWhiteSpace(Settings.KickChatroomId) ? null : Settings.KickChatroomId.Trim();
            await _kickService.ConnectAsync(Settings.KickUsername, manualId);
            IsConnected = true;
            if (!_hasError)
                StatusText = "Connected";
        }
        catch (Exception ex)
        {
            SetError($"Connection error: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task DisconnectAsync()
    {
        await _kickService.DisconnectAsync();
        IsConnected = false;
        _hasError = false;
        StatusText = "Disconnected";
    }

    [RelayCommand]
    private void ToggleBorders()
    {
        IsBorderVisible = !IsBorderVisible;
    }

    [RelayCommand]
    private void ClearChat()
    {
        Messages.Clear();
    }

    public void PreviewSound(string soundName, double volume)
    {
        _soundService.PlayPreview(soundName, volume);
    }

    public void UpdateNotificationSound()
    {
        _soundService.SetSound(Settings.NotificationSound);
    }

    public void SaveSettings()
    {
        Settings.Save();
    }
}
