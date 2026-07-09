using Terminal.Gui.App;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using Chat.Models;
using Chat.Services;

namespace Chat.Ui;

public sealed class ChatWindow : IDisposable
{
    private readonly IApplication _app;
    private readonly IChatService _chat;
    private readonly RoomInfo _room;
    private readonly List<string> _members;

    private readonly Window _window;
    private readonly Label _roomInfo;
    private readonly MessageView _messages;
    private readonly TextField _input;

    public ChatWindow(IApplication app, IChatService chat, RoomInfo room, IEnumerable<string> members, RoomInfo[] rooms)
    {
        _app = app;
        _chat = chat;
        _room = room;
        _members = [.. members];

        _window = new Window { Title = "Hollow Chat" };

        var roomsPanel = new FrameView
        {
            Title = "Rooms",
            X = 0, Y = 0,
            Width = 24,
            Height = Dim.Fill(),
        };
        var roomsList = new Label
        {
            X = 0, Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Auto(),
            Text = string.Join("\n", rooms.Select(r =>
                (r.Id == room.Id ? "> " : "  ") + r.Name + (r.IsPrivate ? " *" : ""))),
        };
        roomsPanel.Add(roomsList);

        var roomPanel = new FrameView
        {
            Title = "Room",
            X = Pos.Right(roomsPanel), Y = 0,
            Width = Dim.Fill(),
            Height = 4,
        };
        _roomInfo = new Label
        {
            X = 0, Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Auto(),
        };
        roomPanel.Add(_roomInfo);

        _messages = new MessageView(
            Pos.Right(roomsPanel), Pos.Bottom(roomPanel), Dim.Fill(), Dim.Fill(3));

        var inputPanel = new FrameView
        {
            Title = "Type a message (Enter to send)",
            X = Pos.Right(roomsPanel), Y = Pos.Bottom(_messages.View),
            Width = Dim.Fill(),
            Height = Dim.Fill(),
        };
        _input = new TextField
        {
            X = 0, Y = 0,
            Width = Dim.Fill(),
        };
        inputPanel.Add(_input);

        _window.Add(roomsPanel, roomPanel, _messages.View, inputPanel);

        _window.SetScheme(Theme.Readable);
        roomsPanel.SetScheme(Theme.Accent(Theme.Green));
        roomsList.SetScheme(Theme.Readable);
        roomPanel.SetScheme(Theme.Accent(Theme.Mauve));
        _roomInfo.SetScheme(Theme.Readable);
        inputPanel.SetScheme(Theme.Accent(Theme.Peach));
        _input.SetScheme(Theme.Input);

        RefreshRoomInfo();
        Subscribe();
    }

    public void Run() => _app.Run(_window);

    private void Subscribe()
    {
        _chat.MessageReceived += OnMessageReceived;
        _chat.UserJoined += OnUserJoined;
        _chat.UserLeft += OnUserLeft;
        _chat.Disconnected += OnDisconnected;
        _input.Accepting += OnInputAccepted;
    }

    private void OnMessageReceived(string sender, string text) =>
        _app.Invoke(() => _messages.Append($"{sender}: {text}"));

    private void OnUserJoined(string user) => _app.Invoke(() =>
    {
        if (!_members.Contains(user)) _members.Add(user);
        RefreshRoomInfo();
        _messages.Append($"* {user} joined the room");
    });

    private void OnUserLeft(string user) => _app.Invoke(() =>
    {
        _members.Remove(user);
        RefreshRoomInfo();
        _messages.Append($"* {user} left the room");
    });

    private void OnDisconnected() =>
        _app.Invoke(() => _messages.Append("* Disconnected from server."));

    private async void OnInputAccepted(object? sender, CommandEventArgs e)
    {
        e.Handled = true;

        var text = _input.Text;
        if (string.IsNullOrWhiteSpace(text)) return;

        _messages.Append($"{_chat.UserName}: {text}");
        _input.Text = string.Empty;
        await _chat.SendAsync(text);
    }

    private void RefreshRoomInfo() =>
        _roomInfo.Text = $"{_room.Name}{(_room.IsPrivate ? " [private]" : "")}: {_room.Description}\n"
                       + $"Members: {string.Join(", ", _members)}";

    public void Dispose()
    {
        _chat.MessageReceived -= OnMessageReceived;
        _chat.UserJoined -= OnUserJoined;
        _chat.UserLeft -= OnUserLeft;
        _chat.Disconnected -= OnDisconnected;
        _input.Accepting -= OnInputAccepted;
        _window.Dispose();
    }
}
