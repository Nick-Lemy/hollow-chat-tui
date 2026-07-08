using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace Chat;

public static class Server
{
    private const int Port = 11_000;
    private static readonly Dictionary<StreamWriter, Guid> RoomByWriter = [];
    private static readonly Lock Gate = new();

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static async Task Run()
    {
        var ipEndPoint = new IPEndPoint(IPAddress.Any, Port);

        using Socket listener = new(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        listener.Bind(ipEndPoint);
        listener.Listen(100);

        Console.WriteLine($"Chat server listening on port {Port}. Waiting for clients...");

        while (true)
        {
            var handler = await listener.AcceptAsync();
            Console.WriteLine($"Client connected: {handler.RemoteEndPoint}");
            _ = HandleClientAsync(handler);
        }
    }

    private static async Task HandleClientAsync(Socket handler)
    {
        var remote = handler.RemoteEndPoint;
        var stream = new NetworkStream(handler);
        var reader = new StreamReader(stream);
        var writer = new StreamWriter(stream) { AutoFlush = true };

        lock (Gate) RoomByWriter[writer] = Guid.Empty;

        try
        {
            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                Envelope? envelope;
                try { envelope = JsonSerializer.Deserialize<Envelope>(line, JsonOptions); }
                catch (JsonException) { continue; }
                if (envelope is null) continue;

                lock (Gate) RoomByWriter[writer] = envelope.RoomId;
                RouteToRoom(line, envelope.RoomId, except: writer);
            }
        }
        catch (IOException)
        {
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling client {remote}: {ex}");
        }
        finally
        {
            lock (Gate) RoomByWriter.Remove(writer);
            Console.WriteLine($"Client disconnected: {remote}");
        }
    }

    private static void RouteToRoom(string message, Guid roomId, StreamWriter except)
    {
        lock (Gate)
        {
            foreach (var (w, room) in RoomByWriter)
                if (room == roomId && w != except)
                    _ = w.WriteLineAsync(message);
        }
    }
    private sealed record Envelope(Guid RoomId);
}
