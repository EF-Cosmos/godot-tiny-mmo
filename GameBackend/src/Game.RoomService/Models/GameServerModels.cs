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
}

public class RegisterServerRequest
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Region { get; set; } = "default";
    public int MaxPlayers { get; set; }
    public string MapName { get; set; } = string.Empty;
}

public class HeartbeatRequest
{
    public string ServerId { get; set; } = string.Empty;
    public int CurrentPlayers { get; set; }
}
