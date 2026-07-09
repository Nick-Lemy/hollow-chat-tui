using Chat.Models;
using Chat.Services;

namespace Chat.Cli;

public static class RoomSelector
{
    public static async Task<(RoomInfo Room, string[] Members)> SelectAsync(IChatService chat)
    {
        while (true)
        {
            var rooms = await chat.GetRoomsAsync();
            PrintRooms(rooms);

            Console.Write("\n(J)oin an existing room or (C)reate a new one? [J]: ");
            var choice = (Console.ReadLine() ?? "").Trim().ToLowerInvariant();

            var joined = choice is "c" or "create"
                ? await TryCreateAsync(chat)
                : await TryJoinAsync(chat, rooms);

            if (joined is not null) return joined.Value;
        }
    }

    private static void PrintRooms(RoomInfo[] rooms)
    {
        Console.WriteLine("\nAvailable rooms:");
        foreach (var room in rooms)
        {
            var visibility = room.IsPrivate ? " [private]" : "";
            Console.WriteLine($"  - {room.Name}{visibility}: {room.Description} ({room.MemberCount} online)");
        }
    }

    private static async Task<(RoomInfo, string[])?> TryCreateAsync(IChatService chat)
    {
        Console.Write("New room name: ");
        var name = (Console.ReadLine() ?? "").Trim();
        if (name.Length == 0)
        {
            Console.WriteLine("Room name cannot be empty.");
            return null;
        }

        Console.Write("Description: ");
        var description = (Console.ReadLine() ?? "").Trim();

        Console.Write("Enter a 4-digit code to make it private (or leave blank for public): ");
        if (!TryReadOptionalCode(out var code)) return null;

        try
        {
            var created = await chat.CreateRoomAsync(name, description, code);
            var joined = await chat.JoinRoomAsync(created.Id, code);
            Console.WriteLine($"Created and joined '{created.Name}'.");
            return joined;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return null;
        }
    }

    private static async Task<(RoomInfo, string[])?> TryJoinAsync(IChatService chat, RoomInfo[] rooms)
    {
        Console.Write("Room name to join [General]: ");
        var joinName = (Console.ReadLine() ?? "").Trim();
        if (joinName.Length == 0) joinName = "General";

        var target = rooms.FirstOrDefault(r => r.Name.Equals(joinName, StringComparison.OrdinalIgnoreCase));
        if (target is null)
        {
            Console.WriteLine($"No room named '{joinName}'. Try again or create it.");
            return null;
        }

        int? code = null;
        if (target.IsPrivate)
        {
            Console.Write("This room is private. Enter its code: ");
            if (!int.TryParse((Console.ReadLine() ?? "").Trim(), out var parsed))
            {
                Console.WriteLine("Invalid code.");
                return null;
            }
            code = parsed;
        }

        try
        {
            var joined = await chat.JoinRoomAsync(target.Id, code);
            Console.WriteLine($"Joined '{target.Name}'.");
            return joined;
        }
        catch (UnauthorizedAccessException ex)
        {
            Console.WriteLine(ex.Message);
            return null;
        }
    }

    private static bool TryReadOptionalCode(out int? code)
    {
        code = null;
        var input = (Console.ReadLine() ?? "").Trim();
        if (input.Length == 0) return true;

        if (!int.TryParse(input, out var parsed) || parsed < 1000 || parsed > 9999)
        {
            Console.WriteLine("Invalid code. Enter a number between 1000 and 9999, or leave blank.");
            return false;
        }

        code = parsed;
        return true;
    }
}
