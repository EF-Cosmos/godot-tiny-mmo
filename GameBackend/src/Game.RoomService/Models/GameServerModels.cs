namespace Game.RoomService.Models;

public class GameServerInfo
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Region { get; set; } = "default";
    public int CurrentPlayers { get; set; }
    public int MaxPlayers { get; set; }
    public string MapName { get; set; } = string.Empty;
    public DateTime LastHeartbeat { get; set; } = DateTime.UtcNow;
    public bool IsActive => (DateTime.UtcNow - LastHeartbeat).TotalSeconds < 30;

    // Server type classification: world, instance, pvp, dungeon, etc.
    public string ServerType { get; set; } = "world";

    // Instance identifier for multi-instance deployments: forest-1, dungeon-2
    public string InstanceId { get; set; } = string.Empty;

    // Flexible tags for custom metadata: difficulty, events, etc.
    public Dictionary<string, string> Tags { get; set; } = new();
}

public class RegisterServerRequest
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Region { get; set; } = "default";
    public int MaxPlayers { get; set; }
    public string MapName { get; set; } = string.Empty;

    // Optional server type (defaults to "world")
    public string? ServerType { get; set; }

    // Optional instance identifier (auto-generated if not provided)
    public string? InstanceId { get; set; }

    // Optional tags dictionary for custom metadata
    public Dictionary<string, string>? Tags { get; set; }
}

public class HeartbeatRequest
{
    public string ServerId { get; set; } = string.Empty;
    public int CurrentPlayers { get; set; }
}
