using System.Collections.Concurrent;
using Game.RoomService.Models;

namespace Game.RoomService.Services;

public interface IGameServerManager
{
    string RegisterServer(RegisterServerRequest request);
    bool UpdateHeartbeat(HeartbeatRequest request);
    void RemoveInactiveServers();
    IEnumerable<GameServerInfo> GetAvailableServers(string mapName);
    GameServerInfo? GetServer(string serverId);
}

public class GameServerManager : IGameServerManager
{
    private readonly ConcurrentDictionary<string, GameServerInfo> _servers = new();
    private readonly ILogger<GameServerManager> _logger;

    public GameServerManager(ILogger<GameServerManager> logger)
    {
        _logger = logger;
    }

    public string RegisterServer(RegisterServerRequest request)
    {
        var server = new GameServerInfo
        {
            Name = request.Name,
            Address = request.Address,
            Port = request.Port,
            Region = request.Region,
            MaxPlayers = request.MaxPlayers,
            MapName = request.MapName,
            LastHeartbeat = DateTime.UtcNow
        };

        _servers[server.Id] = server;
        _logger.LogInformation("Registered new game server: {Name} ({Id}) at {Address}:{Port}", 
            server.Name, server.Id, server.Address, server.Port);
        
        return server.Id;
    }

    public bool UpdateHeartbeat(HeartbeatRequest request)
    {
        if (_servers.TryGetValue(request.ServerId, out var server))
        {
            server.LastHeartbeat = DateTime.UtcNow;
            server.CurrentPlayers = request.CurrentPlayers;
            return true;
        }
        return false;
    }

    public void RemoveInactiveServers()
    {
        var inactiveIds = _servers.Values
            .Where(s => !s.IsActive)
            .Select(s => s.Id)
            .ToList();

        foreach (var id in inactiveIds)
        {
            if (_servers.TryRemove(id, out var server))
            {
                _logger.LogWarning("Removed inactive server: {Name} ({Id})", server.Name, server.Id);
            }
        }
    }

    public IEnumerable<GameServerInfo> GetAvailableServers(string mapName)
    {
        var query = _servers.Values.Where(s => s.IsActive);
        
        if (!string.IsNullOrEmpty(mapName))
        {
            query = query.Where(s => s.MapName.Equals(mapName, StringComparison.OrdinalIgnoreCase));
        }

        return query.OrderByDescending(s => s.MaxPlayers - s.CurrentPlayers); // 优先返回空闲位置多的
    }

    public GameServerInfo? GetServer(string serverId)
    {
        _servers.TryGetValue(serverId, out var server);
        return server;
    }
}
