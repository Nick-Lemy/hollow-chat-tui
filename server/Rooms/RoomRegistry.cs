namespace Chat.Rooms;

public sealed class RoomRegistry
{
    private readonly Dictionary<Guid, Room> _rooms = [];
    private readonly Lock _gate = new();

    public RoomRegistry()
    {
        var general = new Room { Name = "General", Description = "The default room for all users." };
        _rooms[general.Id] = general;
    }

    public Room? Find(Guid id)
    {
        lock (_gate) return _rooms.GetValueOrDefault(id);
    }

    public Room[] All()
    {
        lock (_gate) return [.. _rooms.Values];
    }

    public bool TryCreate(string name, string description, int? code, out Room room)
    {
        lock (_gate)
        {
            if (_rooms.Values.Any(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                room = null!;
                return false;
            }

            room = new Room { Name = name, Description = description, Code = code };
            _rooms[room.Id] = room;
            return true;
        }
    }
}
