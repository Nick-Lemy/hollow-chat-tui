using System.Net.Sockets;
using Chat.Models;

namespace Chat.Services;

public sealed class ChatConnection : IAsyncDisposable
{
    private TcpClient? _client;
    private StreamWriter? _writer;

    public event Action<Envelope>? EnvelopeReceived;
    public event Action? Closed;

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

    public async Task SendAsync(Envelope envelope)
    {
        if (_writer is null)
            throw new InvalidOperationException("Not connected. Call ConnectAsync first.");

        await _writer.WriteLineAsync(Wire.Serialize(envelope));
    }

    private async Task ReceiveLoopAsync(StreamReader reader)
    {
        try
        {
            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                var envelope = Wire.Deserialize(line);
                if (envelope is not null) EnvelopeReceived?.Invoke(envelope);
            }
        }
        catch (IOException) { }
        finally
        {
            Closed?.Invoke();
        }
    }

    public ValueTask DisposeAsync()
    {
        _writer?.Dispose();
        _client?.Dispose();
        return ValueTask.CompletedTask;
    }
}
