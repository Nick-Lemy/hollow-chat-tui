namespace Chat.Models;

public class Room
{
    public Guid Id => new(System.Security.Cryptography.MD5.HashData(
      System.Text.Encoding.UTF8.GetBytes($"{Name.Trim().ToLowerInvariant()}|{Code}")));
    public required string Name { get; set; }
    public required string Description { get; set; }
    public int? Code { get; set; }
    public List<string> Members { get; set; } = [];
    public List<Message> Messages { get; set; } = [];
}
