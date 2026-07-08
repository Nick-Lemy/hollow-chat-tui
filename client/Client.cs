using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Chat.Models;

namespace Chat;

public static class Client
{
    private const int DefaultPort = 11_000;

    public static async Task Run(string name)
    {
        Console.Write("Enter server address (host or host:port) [localhost]: ");
        var input = (Console.ReadLine() ?? "").Trim();
        if (input.Length == 0) input = "localhost";

        if (!TryParseServerAddress(input, out var host, out var port))
        {
            Console.WriteLine("Invalid server address. Expected e.g. chat.example.com or 203.0.113.5:11000");
            return;
        }

        using var client = new TcpClient();
        Console.WriteLine($"Connecting to {host}:{port}...");
        try
        {
            await client.ConnectAsync(host, port);
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"Could not connect: {ex.Message}");
            return;
        }
        Console.WriteLine("Connected!");

        using var stream = client.GetStream();
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
            Console.WriteLine("* Disconnected from server.");
        });

        Console.WriteLine($"Signed in as {name}. Type a message and press Enter:\n");
        string? text;
        while ((text = Console.ReadLine()) != null)
        {
            var msg = new ChatMessage(name, text);
            await writer.WriteLineAsync(JsonSerializer.Serialize(msg));
        }
    }

    private static bool TryParseServerAddress(string value, out string host, out int port)
    {
        host = value;
        port = DefaultPort;

        var parts = value.Split(':');
        if (parts.Length == 1)
        {
            host = parts[0];
            return host.Length > 0;
        }
        if (parts.Length == 2)
        {
            host = parts[0];
            return host.Length > 0 && int.TryParse(parts[1], out port);
        }
        return false;
    }
}
