using Chat.Models;

namespace Chat.Services;

public sealed class ChatService : IChatService
{
    private readonly ChatConnection _connection = new();
    private string _userName = "Anonymous";

    private TaskCompletionSource<RoomInfo[]>? _pendingRoomList;
    private TaskCompletionSource<RoomInfo>? _pendingCreate;
    private TaskCompletionSource<(RoomInfo, string[])>? _pendingJoin;

    public ChatService()
    {
        _connection.EnvelopeReceived += Dispatch;
        _connection.Closed += OnClosed;
    }

    public event Action<string, string>? MessageReceived;
    public event Action<string>? UserJoined;
    public event Action<string>? UserLeft;
    public event Action? Disconnected;

    public RoomInfo? CurrentRoom { get; private set; }

    public string UserName
    {
        get => _userName;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("User name cannot be null or whitespace.", nameof(value));
            _userName = value;
        }
    }

    public bool IsConnected => _connection.IsConnected;

    public async Task ConnectAsync(string host, int port, CancellationToken cancellationToken = default)
    {
        await _connection.ConnectAsync(host, port, cancellationToken);
        await _connection.SendAsync(Envelope.Hello(UserName));
    }

    public async Task<RoomInfo[]> GetRoomsAsync()
    {
        var pending = NewRequest(out _pendingRoomList);
        await _connection.SendAsync(Envelope.ListRooms());
        return await pending;
    }

    public async Task<RoomInfo> CreateRoomAsync(string name, string description, int? code)
    {
        var pending = NewRequest(out _pendingCreate);
        await _connection.SendAsync(Envelope.CreateRoom(name, description, code));
        return await pending;
    }

    public async Task<(RoomInfo Room, string[] Members)> JoinRoomAsync(Guid roomId, int? code)
    {
        var pending = NewRequest(out _pendingJoin);
        await _connection.SendAsync(Envelope.JoinRoom(roomId, code));
        return await pending;
    }

    public Task SendAsync(string text)
    {
        if (CurrentRoom is null)
            throw new InvalidOperationException("Join a room before sending messages.");

        return _connection.SendAsync(Envelope.SendMessage(CurrentRoom.Id, text));
    }

    private void Dispatch(Envelope envelope)
    {
        switch (envelope.Type)
        {
            case EnvelopeType.RoomList:
                Complete(ref _pendingRoomList, envelope.Rooms ?? []);
                break;

            case EnvelopeType.RoomCreated:
                if (envelope.Room is not null) Complete(ref _pendingCreate, envelope.Room);
                break;

            case EnvelopeType.Joined:
                if (envelope.Room is not null)
                {
                    CurrentRoom = envelope.Room;
                    Complete(ref _pendingJoin, (envelope.Room, envelope.Members ?? []));
                }
                break;

            case EnvelopeType.JoinDenied:
                Fail(ref _pendingJoin, new UnauthorizedAccessException(envelope.Reason ?? "Join denied."));
                break;

            case EnvelopeType.ChatMessage:
                MessageReceived?.Invoke(envelope.UserName ?? "?", envelope.Text ?? "");
                break;

            case EnvelopeType.UserJoined:
                if (envelope.UserName is not null) UserJoined?.Invoke(envelope.UserName);
                break;

            case EnvelopeType.UserLeft:
                if (envelope.UserName is not null) UserLeft?.Invoke(envelope.UserName);
                break;

            case EnvelopeType.Error:
                FailPending(new InvalidOperationException(envelope.Reason ?? "Server error."));
                break;
        }
    }

    private void OnClosed()
    {
        FailPending(new IOException("Disconnected from server."));
        Disconnected?.Invoke();
    }

    private static Task<T> NewRequest<T>(out TaskCompletionSource<T> slot)
    {
        slot = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        return slot.Task;
    }

    private static void Complete<T>(ref TaskCompletionSource<T>? slot, T value)
    {
        var pending = slot;
        slot = null;
        pending?.TrySetResult(value);
    }

    private static void Fail<T>(ref TaskCompletionSource<T>? slot, Exception error)
    {
        var pending = slot;
        slot = null;
        pending?.TrySetException(error);
    }

    private void FailPending(Exception error)
    {
        Fail(ref _pendingRoomList, error);
        Fail(ref _pendingCreate, error);
        Fail(ref _pendingJoin, error);
    }

    public ValueTask DisposeAsync() => _connection.DisposeAsync();
}
