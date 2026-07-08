using Chat.Models;

namespace Chat.Services;

public interface IChatService : IAsyncDisposable
{
    event Action<ChatMessage>? MessageReceived;
    event Action? Disconnected;
    string UserName { get; }
    bool IsConnected { get; }
    Task ConnectAsync(string host, int port, CancellationToken cancellationToken = default);
    Task SendAsync(string message);
}
