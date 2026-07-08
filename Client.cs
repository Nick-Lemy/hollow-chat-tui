using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Chat.Models;

namespace Chat;

public static class Client
{
    public static async Task Run(string name)
    {
        Console.Write("Paste the host link (e.g. 192.168.1.188:11000): ");
        var link = (Console.ReadLine() ?? "").Trim();

        if (!TryParseEndPoint(link, out var ipEndPoint))
        {
            Console.WriteLine("Invalid link. Expected something like 192.168.1.188:11000");
            return;
        }

        using Socket client = new(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

        Console.WriteLine($"Connecting to {ipEndPoint}...");
        await client.ConnectAsync(ipEndPoint);
        Console.WriteLine("Connected!");

        using var stream = new NetworkStream(client);
        using var reader = new StreamReader(stream);
        using var writer = new StreamWriter(stream) { AutoFlush = true };

        _ = Task.Run(async () =>
        {
            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                var msg = JsonSerializer.Deserialize<ChatMessage>(line);
                Console.WriteLine($"\n{msg?.Sender}: {msg?.Message}");
            }
            Console.WriteLine("* Disconnected from host.");
        });

        Console.WriteLine($"Signed in as {name}. Type a message and press Enter:\n");
        string? input;
        while ((input = Console.ReadLine()) != null)
        {
            var msg = new ChatMessage(name, input);
            await writer.WriteLineAsync(JsonSerializer.Serialize(msg));
        }
    }

    static bool TryParseEndPoint(string value, out IPEndPoint endPoint)
    {
        endPoint = new IPEndPoint(IPAddress.None, 0);
        var parts = value.Split(':');
        if (parts.Length != 2) return false;
        if (!IPAddress.TryParse(parts[0], out var ip)) return false;
        if (!int.TryParse(parts[1], out var port)) return false;
        endPoint = new IPEndPoint(ip, port);
        return true;
    }
}
