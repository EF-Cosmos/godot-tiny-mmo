using System;

namespace Game.Shared.Models
{
    public class Message
    {
        public Guid Id { get; set; }
        public Guid RoomId { get; set; }
        public Guid SenderId { get; set; }
        public string SenderUsername { get; set; }
        public string Content { get; set; }
        public MessageType Type { get; set; }
        public DateTime Timestamp { get; set; }
        public MessageStatus Status { get; set; }
        public bool IsSystem { get; set; }
        public string RecipientId { get; set; }
    }

    public enum MessageType
    {
        Text,
        System,
        Action,
        Emote,
        Game,
        Private
    }

    public enum MessageStatus
    {
        Delivered,
        Read,
        Failed
    }
}