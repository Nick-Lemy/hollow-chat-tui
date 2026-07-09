using Chat.Models;

namespace Chat.Services;

public interface IChatService : IAsyncDisposable
{
    event Action<string, string>? MessageReceived;
    event Action<string>? UserJoined;
    event Action<string>? UserLeft;
    event Action? Disconnected;

    string UserName { get; set; }
    bool IsConnected { get; }
    RoomInfo? CurrentRoom { get; }

    Task ConnectAsync(string host, int port, CancellationToken cancellationToken = default);
    Task<RoomInfo[]> GetRoomsAsync();
    Task<RoomInfo> CreateRoomAsync(string name, string description, int? code);
    Task<(RoomInfo Room, string[] Members)> JoinRoomAsync(Guid roomId, int? code);
    Task SendAsync(string text);
}
