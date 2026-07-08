using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using Chat;
using Terminal.Gui.Text;

// --- Gather connection info before starting the TUI (plain console) ---
Console.Write("Enter your name: ");
var myName = Console.ReadLine() ?? "Anonymous";

Console.Write("Enter server address (host or host:port) [localhost]: ");
var address = Console.ReadLine() ?? "";
if (!ChatService.TryParseServerAddress(address.Length == 0 ? "localhost" : address, out var host, out var port))
{
    Console.WriteLine("Invalid server address.");
    return;
}

// --- Create the service and connect ---
await using IChatService chat = new ChatService(myName);
try
{
    await chat.ConnectAsync(host, port);
}
catch (Exception ex)
{
    Console.WriteLine($"Could not connect: {ex.Message}");
    return;
}

// --- Build the TUI ---
using var app = Application.Create();
app.Init();

using var window = new Window() {
    Title = "Bankai Chat"
 };

var messagesPanel = new FrameView()
{
    Title = "Messages",
    X = 0, Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Percent(85),
};

// The chat history is one running block of text (a Label sized to its content)
// living inside the scrollable messagesPanel. New lines are appended at the
// bottom; the panel scrolls so older messages move up and out of view.
messagesPanel.VerticalScrollBar.Visible = true;

var messages = new Label()
{
    Text = "",
    X = 0, Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Auto(),   // grows with the text so all lines exist to scroll through
};
messagesPanel.Add(messages);

var lineCount = 0;

// Appends a line at the bottom and scrolls the panel to keep it in view.
void AddMessage(string line)
{
    messages.Text += messages.Text.Length == 0 ? line : "\n" + line;
    lineCount++;

    // Tell the panel how tall its scrollable content is, then move the viewport
    // to the bottom so the newest line is visible.
    messagesPanel.SetContentHeight(lineCount);
    var viewportHeight = messagesPanel.Viewport.Height;
    messagesPanel.Viewport = messagesPanel.Viewport with { Y = Math.Max(0, lineCount - viewportHeight) };
}

var inputPanel = new FrameView()
{
    Title = "Type a message (Enter to send)",
    X = 0, Y = Pos.Bottom(messagesPanel),
    Width = Dim.Fill(),
    Height = Dim.Fill(),
};

var textField = new TextField()
{
    X = 0, Y = 0,
    Width = Dim.Fill(),
};
inputPanel.Add(textField);

window.Add(messagesPanel, inputPanel);

chat.MessageReceived += msg => app.Invoke(() => AddMessage($"{msg.Sender}: {msg.Message}"));

chat.Disconnected += () => app.Invoke(() => AddMessage("* Disconnected from server."));

textField.Accepting += async (sender, e) =>
{
    e.Handled = true;
    var text = textField.Text;
    if (!string.IsNullOrWhiteSpace(text))
    {
        AddMessage($"{myName}: {text}");   // echo our own line locally
        textField.Text = string.Empty;
        await chat.SendAsync(text);
    }
};

app.Run(window);
