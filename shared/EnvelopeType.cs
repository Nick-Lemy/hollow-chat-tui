using System.Text.Json.Serialization;

namespace Chat.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EnvelopeType
{
    // client -> server
    Hello,
    ListRooms,
    CreateRoom,
    JoinRoom,
    LeaveRoom,
    SendMessage,

    // server -> client
    RoomList,
    RoomCreated,
    Joined,
    JoinDenied,
    ChatMessage,
    UserJoined,
    UserLeft,
    Error,
}