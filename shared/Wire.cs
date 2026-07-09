using System.Text.Json;
using System.Text.Json.Serialization;

namespace Chat.Models;

public static class Wire
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static string Serialize(Envelope envelope) =>
        JsonSerializer.Serialize(envelope, Options);

    public static Envelope? Deserialize(string line)
    {
        try
        {
            return JsonSerializer.Deserialize<Envelope>(line, Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
