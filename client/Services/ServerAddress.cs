namespace Chat.Services;

public static class ServerAddress
{
    public const int DefaultPort = 11_000;

    public static bool TryParse(string value, out string host, out int port)
    {
        host = value.Trim();
        port = DefaultPort;
        if (host.Length == 0) return false;

        var parts = host.Split(':');
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
