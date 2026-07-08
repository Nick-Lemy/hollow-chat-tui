using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Chat.Models;

namespace Chat;

public static class Host
{
    private const int Port = 11_000;

    public static async Task Run(string name)
    {
        var localIp = GetLocalIpAddress();
        var ipEndPoint = new IPEndPoint(IPAddress.Any, Port);

        using Socket listener = new(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        listener.Bind(ipEndPoint);
        listener.Listen(100);

        Console.WriteLine();
        Console.WriteLine("You are the HOST. Share this link with others so they can join:");
        Console.WriteLine($"    {localIp}:{Port}");
        Console.WriteLine();
        Console.WriteLine("Waiting for people to connect...");

        var writers = new List<StreamWriter>();
        var writersLock = new object();

        _ = Task.Run(async () =>
        {
            while (true)
            {
                var handler = await listener.AcceptAsync();
                Console.WriteLine("A user connected!");
                _ = HandleClientAsync(handler);
            }
        });

        async Task HandleClientAsync(Socket handler)
        {
            var stream = new NetworkStream(handler);
            var reader = new StreamReader(stream);
            var writer = new StreamWriter(stream) { AutoFlush = true };
            lock (writersLock) writers.Add(writer);
            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                var msg = JsonSerializer.Deserialize<ChatMessage>(line);
                Console.WriteLine($"\n{msg?.Sender}: {msg?.Message}\n");
                Broadcast(line, except: writer);
            }

            lock (writersLock) writers.Remove(writer);
            Console.WriteLine("A user disconnected.");
        }

        void Broadcast(string message, StreamWriter? except)
        {
            lock (writersLock)
            {
                foreach (var w in writers)
                    if (w != except)
                        w.WriteLineAsync(message);
            }
        }

        Console.WriteLine($"Signed in as {name}. Type a message and press Enter:\n");
        string? input;
        while ((input = Console.ReadLine()) != null)
        {
            var msg = new ChatMessage(name, input);
            Broadcast(JsonSerializer.Serialize(msg), except: null);
        }
    }

    static IPAddress GetLocalIpAddress()
    {
        try
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Connect("8.8.8.8", 65530);
            if (socket.LocalEndPoint is IPEndPoint endPoint)
                return endPoint.Address;
        }
        catch
        {
            Console.WriteLine("Could not determine local IP address. Using loopback.");
        }
        return IPAddress.Loopback;
    }
}
