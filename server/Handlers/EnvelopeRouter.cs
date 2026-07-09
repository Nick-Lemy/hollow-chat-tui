using Chat.Models;
using Chat.Networking;
using Chat.Rooms;

namespace Chat.Handlers;

public sealed class EnvelopeRouter(RoomRegistry rooms, ConnectionRegistry connections)
{
    public async Task HandleAsync(ClientConnection connection, Envelope envelope)
    {
        if (connection.UserName is null && envelope.Type != EnvelopeType.Hello)
        {
            await connection.SendAsync(Envelope.Error("Send Hello before anything else."));
            return;
        }

        switch (envelope.Type)
        {
            case EnvelopeType.Hello:
                await HelloAsync(connection, envelope);
                break;
            case EnvelopeType.ListRooms:
                await connection.SendAsync(Envelope.RoomList(SnapshotRooms()));
                break;
            case EnvelopeType.CreateRoom:
                await CreateRoomAsync(connection, envelope);
                break;
            case EnvelopeType.JoinRoom:
                await JoinRoomAsync(connection, envelope);
                break;
            case EnvelopeType.LeaveRoom:
                await LeaveCurrentRoomAsync(connection);
                break;
            case EnvelopeType.SendMessage:
                await SendMessageAsync(connection, envelope);
                break;
            default:
                await connection.SendAsync(Envelope.Error($"Unsupported frame: {envelope.Type}."));
                break;
        }
    }

    public async Task DisconnectAsync(ClientConnection connection)
    {
        var roomId = connection.RoomId;
        var userName = connection.UserName;

        connections.Remove(connection);
        connection.RoomId = null;

        if (roomId is { } id && userName is not null)
            await connections.BroadcastAsync(id, Envelope.UserLeft(id, userName));
    }

    private static async Task HelloAsync(ClientConnection connection, Envelope envelope)
    {
        if (string.IsNullOrWhiteSpace(envelope.UserName))
        {
            await connection.SendAsync(Envelope.Error("Hello requires a user name."));
            return;
        }

        connection.UserName = envelope.UserName.Trim();
        Console.WriteLine($"{connection.Remote} identified as {connection.UserName}");
    }

    private async Task CreateRoomAsync(ClientConnection connection, Envelope envelope)
    {
        if (string.IsNullOrWhiteSpace(envelope.RoomName))
        {
            await connection.SendAsync(Envelope.Error("CreateRoom requires a name."));
            return;
        }
        if (envelope.Code is { } code && (code < 1000 || code > 9999))
        {
            await connection.SendAsync(Envelope.Error("Room code must be between 1000 and 9999."));
            return;
        }

        var name = envelope.RoomName.Trim();
        var description = string.IsNullOrWhiteSpace(envelope.Description)
            ? "No description."
            : envelope.Description.Trim();

        if (!rooms.TryCreate(name, description, envelope.Code, out var room))
        {
            await connection.SendAsync(Envelope.Error($"A room named '{name}' already exists."));
            return;
        }

        Console.WriteLine($"{connection.UserName} created room {room.Name} ({(room.IsPrivate ? "private" : "public")})");
        await connection.SendAsync(Envelope.RoomCreated(ToInfo(room)));
    }

    private async Task JoinRoomAsync(ClientConnection connection, Envelope envelope)
    {
        if (envelope.RoomId is not { } roomId)
        {
            await connection.SendAsync(Envelope.Error("JoinRoom requires a room id."));
            return;
        }

        var room = rooms.Find(roomId);
        if (room is null)
        {
            await connection.SendAsync(Envelope.Error("No such room."));
            return;
        }
        if (room.IsPrivate && room.Code != envelope.Code)
        {
            await connection.SendAsync(Envelope.JoinDenied(roomId, "Incorrect code."));
            return;
        }

        if (connection.RoomId is { } previous && previous != roomId)
            await LeaveCurrentRoomAsync(connection);

        connection.RoomId = roomId;

        await connection.SendAsync(Envelope.Joined(ToInfo(room), connections.MemberNames(roomId)));
        await connections.BroadcastAsync(roomId, Envelope.UserJoined(roomId, connection.UserName!), except: connection);

        Console.WriteLine($"{connection.UserName} joined {room.Name}");
    }

    private async Task LeaveCurrentRoomAsync(ClientConnection connection)
    {
        if (connection.RoomId is not { } roomId) return;

        connection.RoomId = null;
        await connections.BroadcastAsync(roomId, Envelope.UserLeft(roomId, connection.UserName!));
    }

    private async Task SendMessageAsync(ClientConnection connection, Envelope envelope)
    {
        if (connection.RoomId is not { } roomId)
        {
            await connection.SendAsync(Envelope.Error("Join a room before sending messages."));
            return;
        }
        if (string.IsNullOrWhiteSpace(envelope.Text)) return;

        var chat = Envelope.Chat(roomId, connection.UserName!, envelope.Text, DateTimeOffset.UtcNow);
        await connections.BroadcastAsync(roomId, chat, except: connection);
    }

    private RoomInfo ToInfo(Room room) =>
        new(room.Id, room.Name, room.Description, room.IsPrivate, connections.CountIn(room.Id));

    private RoomInfo[] SnapshotRooms() => [.. rooms.All().Select(ToInfo)];
}
