using System;
using System.Collections.Generic;

namespace Game.Shared.Models
{
    public class Game
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public GameType Type { get; set; }
        public string Version { get; set; }
        public int MinPlayers { get; set; }
        public int MaxPlayers { get; set; }
        public Dictionary<string, object> Rules { get; set; }
        public List<GameMode> Modes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public enum GameType
    {
        Multiplayer,
        SinglePlayer,
        Cooperative,
        Competitive,
        Puzzle
    }

    public class GameMode
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int MinPlayers { get; set; }
        public int MaxPlayers { get; set; }
        public Dictionary<string, object> Settings { get; set; }
    }
}