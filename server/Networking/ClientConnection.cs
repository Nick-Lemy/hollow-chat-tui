using System.Net;
using Chat.Models;

namespace Chat.Networking;

public sealed class ClientConnection(StreamWriter writer)
{
    private readonly SemaphoreSlim _sendLock = new(1, 1);

    public StreamWriter Writer { get; } = writer;
    public EndPoint? Remote { get; init; }
    public string? UserName { get; set; }
    public Guid? RoomId { get; set; }

    public async Task SendAsync(Envelope envelope)
    {
        var line = Wire.Serialize(envelope);

        await _sendLock.WaitAsync();
        try
        {
            await Writer.WriteLineAsync(line);
        }
        catch (IOException) { }
        catch (ObjectDisposedException) { }
        finally
        {
            _sendLock.Release();
        }
    }
}
