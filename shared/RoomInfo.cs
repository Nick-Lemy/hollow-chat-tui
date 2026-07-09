namespace Chat.Models;

public record RoomInfo(Guid Id, string Name, string Description, bool
IsPrivate, int MemberCount);