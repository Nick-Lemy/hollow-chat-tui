using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using Terminal.Gui.Drawing;
using Chat.Services;
using Attribute = Terminal.Gui.Drawing.Attribute;

Console.Write("Enter your name: ");
var myName = Console.ReadLine() ?? "Anonymous";

Console.Write("Enter server address (host or host:port) [localhost]: ");
var address = Console.ReadLine() ?? "";
if (!ChatService.TryParseServerAddress(address.Length == 0 ? "localhost" : address, out var host, out var port))
{
    Console.WriteLine("Invalid server address.");
    return;
}

await using IChatService chat = new ChatService();
chat.UserName = myName;

Console.WriteLine();
Console.WriteLine("=== Rooms ===");
var selectedRoomId = SelectRoom(chat);

try
{
    await chat.ConnectAsync(host, port);
}
catch (Exception ex)
{
    Console.WriteLine($"Could not connect: {ex.Message}");
    return;
}

using var app = Application.Create();
app.Init();

using var window = new Window() {
    Title = "Bankai Chat"
 };

var currentRoom = chat.GetRoomById(selectedRoomId);

var roomsPanel = new FrameView()
{
    Title = "My Rooms",
    X = 0, Y = 0,
    Width = 24,
    Height = Dim.Fill(),
};
var roomsList = new Label()
{
    X = 0, Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Auto(),
};
void RefreshRoomsList()
{
    var mine = chat.Rooms.Where(r => r.Members.Contains(myName));
    roomsList.Text = string.Join("\n", mine.Select(r =>
        (r.Id == selectedRoomId ? "> " : "  ") + r.Name + (r.Code is null ? "" : " *")));
}
RefreshRoomsList();
roomsPanel.Add(roomsList);

var roomPanel = new FrameView()
{
    Title = "Room",
    X = Pos.Right(roomsPanel), Y = 0,
    Width = Dim.Fill(),
    Height = 4,
};
var roomInfo = new Label()
{
    X = 0, Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Auto(),
    Text = $"{currentRoom.Name}{(currentRoom.Code is null ? "" : " [private]")}: {currentRoom.Description}\n"
         + $"Members: {string.Join(", ", currentRoom.Members)}",
};
roomPanel.Add(roomInfo);

var messagesPanel = new FrameView()
{
    Title = "Messages",
    X = Pos.Right(roomsPanel), Y = Pos.Bottom(roomPanel),
    Width = Dim.Fill(),
    Height = Dim.Fill(3),
};
messagesPanel.VerticalScrollBar.Visible = true;

var messages = new Label()
{
    Text = "",
    X = 0, Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Auto(),
};
messagesPanel.Add(messages);

var lineCount = 0;

void AddMessage(string line)
{
    messages.Text += messages.Text.Length == 0 ? line : "\n" + line;
    lineCount++;

    messagesPanel.SetContentHeight(lineCount);
    var viewportHeight = messagesPanel.Viewport.Height;
    messagesPanel.Viewport = messagesPanel.Viewport with { Y = Math.Max(0, lineCount - viewportHeight) };
}

var inputPanel = new FrameView()
{
    Title = "Type a message (Enter to send)",
    X = Pos.Right(roomsPanel), Y = Pos.Bottom(messagesPanel),
    Width = Dim.Fill(),
    Height = Dim.Fill(),
};

var textField = new TextField()
{
    X = 0, Y = 0,
    Width = Dim.Fill(),
};
inputPanel.Add(textField);

window.Add(roomsPanel, roomPanel, messagesPanel, inputPanel);

var baseBg  = new Color("#1e1e2e");
var surface = new Color("#313244");
var text    = new Color("#cdd6f4");
var blue    = new Color("#89b4fa");
var green   = new Color("#a6e3a1");
var mauve   = new Color("#cba6f7");
var peach   = new Color("#fab387");

Scheme Accent(Color c) => new()
{
    Normal    = new Attribute(c, baseBg),
    HotNormal = new Attribute(c, baseBg),
    Focus     = new Attribute(baseBg, c),
    HotFocus  = new Attribute(baseBg, c),
};
var readable = new Scheme { Normal = new Attribute(text, baseBg) };

window.SetScheme(readable);

roomsPanel.SetScheme(Accent(green));
roomsList.SetScheme(readable);

roomPanel.SetScheme(Accent(mauve));
roomInfo.SetScheme(readable);

messagesPanel.SetScheme(Accent(blue));
messages.SetScheme(readable);

inputPanel.SetScheme(Accent(peach));
textField.SetScheme(new Scheme
{
    Normal = new Attribute(text, surface),
    Focus  = new Attribute(text, surface),
});

chat.MessageReceived += msg => app.Invoke(() =>
{
    if (msg.RoomId == selectedRoomId)
        AddMessage($"{msg.Sender}: {msg.Text}");
});

chat.Disconnected += () => app.Invoke(() => AddMessage("* Disconnected from server."));

textField.Accepting += async (sender, e) =>
{
    e.Handled = true;
    var text = textField.Text;
    if (!string.IsNullOrWhiteSpace(text))
    {
        AddMessage($"{myName}: {text}");
        textField.Text = string.Empty;
        await chat.SendAsync(text, selectedRoomId);
    }
};

app.Run(window);

Guid SelectRoom(IChatService service)
{
    while (true)
    {
        Console.WriteLine("\nAvailable rooms:");
        foreach (var room in service.Rooms)
        {
            var visibility = room.Code is null ? "" : " [private]";
            Console.WriteLine($"  - {room.Name}{visibility}: {room.Description}");
        }

        Console.Write("\n(J)oin an existing room or (C)reate a new one? [J]: ");
        var choice = (Console.ReadLine() ?? "").Trim().ToLowerInvariant();

        if (choice is "c" or "create")
        {
            Console.Write("New room name: ");
            var name = (Console.ReadLine() ?? "").Trim();
            if (name.Length == 0)
            {
                Console.WriteLine("Room name cannot be empty.");
                continue;
            }
            if (service.Rooms.Any(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                Console.WriteLine($"A room named '{name}' already exists.");
                continue;
            }

            Console.Write("Description: ");
            var description = (Console.ReadLine() ?? "").Trim();
            if (description.Length == 0) description = "No description.";

            Console.Write("Enter a 4-digit code for private room (or leave blank for public): ");
            var codeInput = (Console.ReadLine() ?? "").Trim();
            int? code = null;
            if (!string.IsNullOrWhiteSpace(codeInput))
            {
                if (!int.TryParse(codeInput, out int parsedCodeCreate))
                {
                    Console.WriteLine("Invalid code. Please enter a 4-digit integer between 1000 and 9999. or leave blank for public.");
                    continue;
                }
                code = parsedCodeCreate;
            }

            service.CreateRoom(name, description, code);
            var newRoomId = service.GetRoomIdByName(name);
            service.JoinRoom(newRoomId, service.UserName, code);
            Console.WriteLine($"Created and joined '{name}'.");
            return newRoomId;
        }

        Console.Write("Room name to join [General]: ");
        var joinName = (Console.ReadLine() ?? "").Trim();
        if (joinName.Length == 0) joinName = "General";

        Console.Write("Enter room code (if private) or leave blank: ");
        var inputCode = (Console.ReadLine() ?? "").Trim();
        var isParsed = int.TryParse(inputCode, out var parsedCode);
        if(inputCode != "" && !isParsed) { 
            throw new ArgumentException("Invalid code. Please enter a valid integer code for the room.");
        }

        var target = service.Rooms.FirstOrDefault(r => r.Name.Equals(joinName, StringComparison.OrdinalIgnoreCase));
        if (target is null)
        {
            Console.WriteLine($"No room named '{joinName}'. Try again or create it.");
            continue;
        }

        service.JoinRoom(target.Id, service.UserName, parsedCode);
        Console.WriteLine($"Joined '{target.Name}'.");
        return target.Id;
    }
}
