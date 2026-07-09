using Chat.Models;

namespace Chat.Networking;

public sealed class ConnectionRegistry
{
    private readonly List<ClientConnection> _connections = [];
    private readonly Lock _gate = new();

    public void Add(ClientConnection connection)
    {
        lock (_gate) _connections.Add(connection);
    }

    public void Remove(ClientConnection connection)
    {
        lock (_gate) _connections.Remove(connection);
    }

    public int CountIn(Guid roomId)
    {
        lock (_gate) return _connections.Count(c => c.RoomId == roomId);
    }

    public string[] MemberNames(Guid roomId)
    {
        lock (_gate)
            return [.. _connections
                .Where(c => c.RoomId == roomId && c.UserName is not null)
                .Select(c => c.UserName!)];
    }

    public async Task BroadcastAsync(Guid roomId, Envelope envelope, ClientConnection? except = null)
    {
        ClientConnection[] targets;
        lock (_gate)
            targets = [.. _connections.Where(c => c.RoomId == roomId && c != except)];

        foreach (var target in targets)
            await target.SendAsync(envelope);
    }
}
