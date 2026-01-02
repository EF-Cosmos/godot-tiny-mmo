using System.ComponentModel.DataAnnotations;

namespace Game.ChatService.Models;

public class ChatMessage
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public Guid SenderId { get; set; }
    public string SenderUsername { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string MessageType { get; set; } = "Text"; // Text, System, Emote
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class ChatRoom
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "Public"; // Public, Private, Guild, Party
    public bool IsPersistent { get; set; } = true;
}
