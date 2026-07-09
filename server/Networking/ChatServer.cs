using System.Net;
using System.Net.Sockets;
using Chat.Handlers;
using Chat.Models;

namespace Chat.Networking;

public sealed class ChatServer(EnvelopeRouter router, ConnectionRegistry connections, int port = 11_000)
{
    public async Task RunAsync()
    {
        var endpoint = new IPEndPoint(IPAddress.Any, port);

        using Socket listener = new(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        listener.Bind(endpoint);
        listener.Listen(100);

        Console.WriteLine($"Chat server listening on port {port}. Waiting for clients...");

        while (true)
        {
            var handler = await listener.AcceptAsync();
            Console.WriteLine($"Client connected: {handler.RemoteEndPoint}");
            _ = HandleClientAsync(handler);
        }
    }

    private async Task HandleClientAsync(Socket handler)
    {
        var stream = new NetworkStream(handler);
        var reader = new StreamReader(stream);
        var connection = new ClientConnection(new StreamWriter(stream) { AutoFlush = true })
        {
            Remote = handler.RemoteEndPoint,
        };

        connections.Add(connection);

        try
        {
            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                var envelope = Wire.Deserialize(line);
                if (envelope is null)
                {
                    await connection.SendAsync(Envelope.Error("Malformed frame."));
                    continue;
                }

                await router.HandleAsync(connection, envelope);
            }
        }
        catch (IOException) { }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling client {connection.Remote}: {ex.Message}");
        }
        finally
        {
            await router.DisconnectAsync(connection);
            Console.WriteLine($"Client disconnected: {connection.Remote}");
        }
    }
}
