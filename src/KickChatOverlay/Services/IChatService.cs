using KickChatOverlay.Models;

namespace KickChatOverlay.Services;

public interface IChatService : IAsyncDisposable
{
    event Action<ChatMessage>? OnMessageReceived;
    event Action<string>? OnError;
    event Action? OnConnected;
    event Action? OnDisconnected;
    Task ConnectAsync(string username, CancellationToken ct = default);
    Task DisconnectAsync();
}
