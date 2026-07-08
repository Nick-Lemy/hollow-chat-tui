using System.Net;
using System.Net.Sockets;

namespace Chat;

public static class Server
{
    private const int Port = 11_000;
    private static readonly List<StreamWriter> Writers = [];
    private static readonly Lock WritersLock = new();

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

        lock (WritersLock) Writers.Add(writer);

        try
        {
            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                Broadcast(line, except: writer);
            }
        }
        catch (IOException)
        {
            Console.WriteLine($"Client disconnected: {remote}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling client {remote}: {ex}");
        }
        finally
        {
            lock (WritersLock) Writers.Remove(writer);
            Console.WriteLine($"Client disconnected: {remote}");
        }
    }

    private static void Broadcast(string message, StreamWriter except)
    {
        lock (WritersLock)
        {
            foreach (var w in Writers)
            {                
                if (w != except) _ = w.WriteLineAsync(message);
            }
        }
    }
}
