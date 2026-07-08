using Chat.Models;

namespace Chat;

// UI-agnostic contract for the chat client. The Terminal.Gui layer depends on
// this interface only: it calls ConnectAsync/SendAsync and reacts to the events.
// The service knows nothing about the console or Terminal.Gui.
public interface IChatService : IAsyncDisposable
{
    // Raised (on a background thread) whenever a message arrives from the server.
    // UI code must marshal back to the UI thread, e.g. Application.Invoke(...).
    event Action<ChatMessage>? MessageReceived;

    // Raised (on a background thread) when the connection to the server ends.
    event Action? Disconnected;

    // The name this client sends messages as.
    string UserName { get; }

    // True once ConnectAsync has succeeded and the connection is still open.
    bool IsConnected { get; }

    // Connects to the server. Throws SocketException if it can't reach it.
    Task ConnectAsync(string host, int port, CancellationToken cancellationToken = default);

    // Sends a chat message from UserName. Throws if not connected.
    Task SendAsync(string message);
}
