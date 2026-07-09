namespace Chat.Rooms;

public sealed class Room
{
    public Guid Id { get; } = Guid.NewGuid();
    public required string Name { get; init; }
    public required string Description { get; init; }
    public int? Code { get; init; }

    public bool IsPrivate => Code is not null;
}
