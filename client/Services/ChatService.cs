using System.Net.Sockets;
using System.Text.Json;
using Chat.Models;
using Chat.Services;

namespace Chat.Services;
public sealed class ChatService : IChatService
{
    public const int DefaultPort = 11_000;

    private TcpClient? _client;
    private StreamWriter? _writer;

    public ChatService(string userName) => UserName = userName;

    public event Action<ChatMessage>? MessageReceived;
    public event Action? Disconnected;

    public string UserName { get; }

    public bool IsConnected => _client?.Connected ?? false;

    public async Task ConnectAsync(string host, int port, CancellationToken cancellationToken = default)
    {
        _client = new TcpClient();
        await _client.ConnectAsync(host, port, cancellationToken);

        var stream = _client.GetStream();
        var reader = new StreamReader(stream);
        _writer = new StreamWriter(stream) { AutoFlush = true };

        _ = ReceiveLoopAsync(reader);
    }

    public async Task SendAsync(string message)
    {
        if (_writer is null)
            throw new InvalidOperationException("Not connected. Call ConnectAsync first.");

        var chatMessage = new ChatMessage(UserName, message);
        await _writer.WriteLineAsync(JsonSerializer.Serialize(chatMessage));
    }

    private async Task ReceiveLoopAsync(StreamReader reader)
    {
        try
        {
            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                var message = JsonSerializer.Deserialize<ChatMessage>(line);
                if (message is not null)
                    MessageReceived?.Invoke(message);
            }
        }
        catch (IOException)
        {
        }
        finally
        {
            Disconnected?.Invoke();
        }
    }

    public static bool TryParseServerAddress(string value, out string host, out int port)
    {
        host = value.Trim();
        port = DefaultPort;
        if (host.Length == 0) return false;

        var parts = host.Split(':');
        if (parts.Length == 1)
        {
            host = parts[0];
            return host.Length > 0;
        }
        if (parts.Length == 2)
        {
            host = parts[0];
            return host.Length > 0 && int.TryParse(parts[1], out port);
        }
        return false;
    }

    public ValueTask DisposeAsync()
    {
        _writer?.Dispose();
        _client?.Dispose();
        return ValueTask.CompletedTask;
    }
}
