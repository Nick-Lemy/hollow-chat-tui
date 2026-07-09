using Terminal.Gui.App;
using Chat.Cli;
using Chat.Models;
using Chat.Services;
using Chat.Ui;

Console.Write("Enter your name: ");
var nameInput = (Console.ReadLine() ?? "").Trim();
var myName = nameInput.Length == 0 ? "Anonymous" : nameInput;

Console.Write("Enter server address (host or host:port) [localhost]: ");
var address = (Console.ReadLine() ?? "").Trim();
if (!ServerAddress.TryParse(address.Length == 0 ? "localhost" : address, out var host, out var port))
{
    Console.WriteLine("Invalid server address.");
    return;
}

await using IChatService chat = new ChatService();
chat.UserName = myName;

try
{
    await chat.ConnectAsync(host, port);
}
catch (Exception ex)
{
    Console.WriteLine($"Could not connect: {ex.Message}");
    return;
}

Console.WriteLine();
Console.WriteLine("=== Rooms ===");

RoomInfo room;
string[] members;
try
{
    (room, members) = await RoomSelector.SelectAsync(chat);
}
catch (Exception ex)
{
    Console.WriteLine($"Could not join a room: {ex.Message}");
    return;
}

var rooms = await chat.GetRoomsAsync();

using var app = Application.Create();
app.Init();

using var window = new ChatWindow(app, chat, room, members, rooms);
window.Run();
