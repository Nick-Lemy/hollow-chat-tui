namespace Chat.Models;

public record Envelope(
    EnvelopeType Type,
    string? UserName = null,
    Guid? RoomId = null,
    string? Text = null,
    string? RoomName = null,
    string? Description = null,
    int? Code = null,
    RoomInfo? Room = null,
    RoomInfo[]? Rooms = null,
    string[]? Members = null,
    string? Reason = null,
    DateTimeOffset? Timestamp = null)
{
    public static Envelope Hello(string userName) =>
        new(EnvelopeType.Hello, UserName: userName);

    public static Envelope ListRooms() =>
        new(EnvelopeType.ListRooms);

    public static Envelope CreateRoom(string name, string description, int? code) =>
        new(EnvelopeType.CreateRoom, RoomName: name, Description: description, Code: code);

    public static Envelope JoinRoom(Guid roomId, int? code) =>
        new(EnvelopeType.JoinRoom, RoomId: roomId, Code: code);

    public static Envelope LeaveRoom(Guid roomId) =>
        new(EnvelopeType.LeaveRoom, RoomId: roomId);

    public static Envelope SendMessage(Guid roomId, string text) =>
        new(EnvelopeType.SendMessage, RoomId: roomId, Text: text);

    public static Envelope RoomList(RoomInfo[] rooms) =>
        new(EnvelopeType.RoomList, Rooms: rooms);

    public static Envelope RoomCreated(RoomInfo room) =>
        new(EnvelopeType.RoomCreated, Room: room);

    public static Envelope Joined(RoomInfo room, string[] members) =>
        new(EnvelopeType.Joined, Room: room, Members: members);

    public static Envelope JoinDenied(Guid roomId, string reason) =>
        new(EnvelopeType.JoinDenied, RoomId: roomId, Reason: reason);

    public static Envelope Chat(Guid roomId, string sender, string text, DateTimeOffset timestamp) =>
        new(EnvelopeType.ChatMessage, RoomId: roomId, UserName: sender, Text: text, Timestamp: timestamp);

    public static Envelope UserJoined(Guid roomId, string userName) =>
        new(EnvelopeType.UserJoined, RoomId: roomId, UserName: userName);

    public static Envelope UserLeft(Guid roomId, string userName) =>
        new(EnvelopeType.UserLeft, RoomId: roomId, UserName: userName);

    public static Envelope Error(string reason) =>
        new(EnvelopeType.Error, Reason: reason);
}
