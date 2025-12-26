using System;
using System.Collections.Generic;

namespace Game.Shared.Models
{
    public class Room
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public RoomType Type { get; set; }
        public RoomStatus Status { get; set; }
        public int MaxPlayers { get; set; }
        public int CurrentPlayers { get; set; }
        public Guid HostId { get; set; }
        public Guid GameId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public Dictionary<string, string> Settings { get; set; }
        public List<Player> Players { get; set; }
    }

    public enum RoomType
    {
        Public,
        Private,
        Tournament,
        Training
    }

    public enum RoomStatus
    {
        Waiting,
        Starting,
        InProgress,
        Paused,
        Finished,
        Cancelled
    }

    public class Player
    {
        public Guid UserId { get; set; }
        public string Username { get; set; }
        public string Avatar { get; set; }
        public PlayerStatus Status { get; set; }
        public int Score { get; set; }
        public int Team { get; set; }
        public bool IsReady { get; set; }
        public bool IsHost { get; set; }
    }

    public enum PlayerStatus
    {
        Connected,
        Ready,
        InGame,
        Disconnected,
        Spectating
    }
}