using System;

namespace Game.Shared.Models
{
    public partial class User
    {
        public Guid Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Avatar { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public UserStatus Status { get; set; }
        public int Level { get; set; }
        public int Experience { get; set; }
    }

    public enum UserStatus
    {
        Online,
        Offline,
        InGame,
        Away,
        Busy
    }
}