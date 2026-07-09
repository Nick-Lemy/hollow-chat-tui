using System.Collections.ObjectModel;
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

    private readonly Window _window;
    private readonly ListView _roomsList;
    private readonly ObservableCollection<string> _roomItems = [];
    private readonly List<RoomInfo> _rooms = [];
    private readonly Button _newRoom;
    private readonly Label _roomInfo;
    private readonly MessageView _messages;
    private readonly TextField _input;

    private readonly Dictionary<Guid, List<string>> _history = [];
    private readonly List<string> _members = [];

    private RoomInfo _room;
    private bool _suppressSelection;
    private bool _switching;

    public ChatWindow(IApplication app, IChatService chat, RoomInfo[] rooms, RoomInfo current, string[] members)
    {
        _app = app;
        _chat = chat;
        _room = current;
        _members.AddRange(members);
        _history[current.Id] = [];

        _window = new Window { Title = "Hollow Chat" };

        var roomsPanel = new FrameView
        {
            Title = "Rooms (click or F6)",
            X = 0, Y = 0,
            Width = 26,
            Height = Dim.Fill(),
        };
        _roomsList = new ListView
        {
            X = 0, Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(1),
        };
        _roomsList.SetSource(_roomItems);
        _newRoom = new Button
        {
            Text = "New Room",
            X = 0,
            Y = Pos.AnchorEnd(1),
        };
        roomsPanel.Add(_roomsList, _newRoom);

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
            Title = "Message (Enter to send, F6 for rooms)",
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
        _roomsList.SetScheme(Theme.Readable);
        _newRoom.SetScheme(Theme.Accent(Theme.Green));
        roomPanel.SetScheme(Theme.Accent(Theme.Mauve));
        _roomInfo.SetScheme(Theme.Readable);
        inputPanel.SetScheme(Theme.Accent(Theme.Peach));
        _input.SetScheme(Theme.Input);

        SetRooms(rooms);
        RefreshRoomInfo();
        Subscribe();
        _input.SetFocus();
    }

    public void Run() => _app.Run(_window);

    private void Subscribe()
    {
        _chat.MessageReceived += OnMessageReceived;
        _chat.UserJoined += OnUserJoined;
        _chat.UserLeft += OnUserLeft;
        _chat.Disconnected += OnDisconnected;
        _input.Accepting += OnInputAccepted;
        _newRoom.Accepting += OnNewRoomClicked;
        _roomsList.ValueChanged += OnRoomSelected;
        _roomsList.Accepting += OnRoomAccepted;
    }

    private void OnRoomSelected(object? sender, ValueChangedEventArgs<int?> e)
    {
        if (_suppressSelection || _switching) return;
        if (e.NewValue is not { } index) return;

        JoinRoomAt(index);
    }

    private void OnRoomAccepted(object? sender, CommandEventArgs e)
    {
        e.Handled = true;
        if (_switching) return;
        if (_roomsList.SelectedItem is not { } index) return;

        JoinRoomAt(index);
    }

    private void JoinRoomAt(int index)
    {
        if (index < 0 || index >= _rooms.Count) return;

        var target = _rooms[index];
        if (target.Id == _room.Id) return;

        int? code = null;
        if (target.IsPrivate)
        {
            var entered = InputDialog.Prompt(_app, $"Join {target.Name}", "Code:");
            if (entered is null || !int.TryParse(entered, out var parsed))
            {
                RestoreSelection();
                return;
            }
            code = parsed;
        }

        _switching = true;
        _ = SwitchRoomAsync(target, code);
    }

    private async Task SwitchRoomAsync(RoomInfo target, int? code)
    {
        try
        {
            var (room, members) = await _chat.JoinRoomAsync(target.Id, code);
            var rooms = await _chat.GetRoomsAsync();

            _app.Invoke(() =>
            {
                _room = room;
                _members.Clear();
                _members.AddRange(members);

                if (!_history.ContainsKey(room.Id)) _history[room.Id] = [];
                _messages.SetLines(_history[room.Id]);

                SetRooms(rooms);
                RefreshRoomInfo();
                _switching = false;
                _input.SetFocus();
            });
        }
        catch (Exception ex)
        {
            _app.Invoke(() =>
            {
                _switching = false;
                RestoreSelection();
                MessageBox.ErrorQuery(_app, "Cannot join room", ex.Message, "OK");
            });
        }
    }

    private void OnNewRoomClicked(object? sender, CommandEventArgs e)
    {
        e.Handled = true;

        var request = CreateRoomDialog.Show(_app);
        if (request is null) return;

        _switching = true;
        _ = CreateRoomAsync(request.Value);
    }

    private async Task CreateRoomAsync((string Name, string Description, int? Code) request)
    {
        try
        {
            var created = await _chat.CreateRoomAsync(request.Name, request.Description, request.Code);
            var (room, members) = await _chat.JoinRoomAsync(created.Id, request.Code);
            var rooms = await _chat.GetRoomsAsync();

            _app.Invoke(() =>
            {
                _room = room;
                _members.Clear();
                _members.AddRange(members);
                _history[room.Id] = [];
                _messages.SetLines([]);

                SetRooms(rooms);
                RefreshRoomInfo();
                _switching = false;
                _input.SetFocus();
            });
        }
        catch (Exception ex)
        {
            _app.Invoke(() =>
            {
                _switching = false;
                RestoreSelection();
                MessageBox.ErrorQuery(_app, "Cannot create room", ex.Message, "OK");
            });
        }
    }

    private async void OnInputAccepted(object? sender, CommandEventArgs e)
    {
        e.Handled = true;

        var text = _input.Text;
        if (string.IsNullOrWhiteSpace(text)) return;

        Append($"{_chat.UserName}: {text}");
        _input.Text = string.Empty;

        try
        {
            await _chat.SendAsync(text);
        }
        catch (Exception ex)
        {
            _app.Invoke(() => MessageBox.ErrorQuery(_app, "Could not send", ex.Message, "OK"));
        }
    }

    private void OnMessageReceived(string sender, string text) =>
        _app.Invoke(() => Append($"{sender}: {text}"));

    private void OnUserJoined(string user) => _app.Invoke(() =>
    {
        if (!_members.Contains(user)) _members.Add(user);
        RefreshRoomInfo();
        Append($"* {user} joined the room");
    });

    private void OnUserLeft(string user) => _app.Invoke(() =>
    {
        _members.Remove(user);
        RefreshRoomInfo();
        Append($"* {user} left the room");
    });

    private void OnDisconnected() =>
        _app.Invoke(() => Append("* Disconnected from server."));

    private void Append(string line)
    {
        if (!_history.TryGetValue(_room.Id, out var lines))
        {
            lines = [];
            _history[_room.Id] = lines;
        }
        lines.Add(line);
        _messages.Append(line);
    }

    private void SetRooms(RoomInfo[] rooms)
    {
        _suppressSelection = true;

        _rooms.Clear();
        _rooms.AddRange(rooms);

        _roomItems.Clear();
        foreach (var room in rooms)
            _roomItems.Add((room.Id == _room.Id ? "> " : "  ") + room.Name + (room.IsPrivate ? " *" : ""));

        RestoreSelectionCore();
        _suppressSelection = false;
    }

    private void RestoreSelection()
    {
        _suppressSelection = true;
        RestoreSelectionCore();
        _suppressSelection = false;
    }

    private void RestoreSelectionCore()
    {
        var index = _rooms.FindIndex(r => r.Id == _room.Id);
        if (index >= 0) _roomsList.SelectedItem = index;
    }

    private void RefreshRoomInfo()
    {
        _messages.Title = $"Messages - {_room.Name}";
        _roomInfo.Text = $"{_room.Name}{(_room.IsPrivate ? " [private]" : "")}: {_room.Description}\n"
                       + $"Members: {string.Join(", ", _members)}";
    }

    public void Dispose()
    {
        _chat.MessageReceived -= OnMessageReceived;
        _chat.UserJoined -= OnUserJoined;
        _chat.UserLeft -= OnUserLeft;
        _chat.Disconnected -= OnDisconnected;
        _input.Accepting -= OnInputAccepted;
        _newRoom.Accepting -= OnNewRoomClicked;
        _roomsList.ValueChanged -= OnRoomSelected;
        _roomsList.Accepting -= OnRoomAccepted;
        _window.Dispose();
    }
}
