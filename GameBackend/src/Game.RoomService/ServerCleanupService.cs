using Game.RoomService.Services;

namespace Game.RoomService;

public class ServerCleanupService : BackgroundService
{
    private readonly IGameServerManager _manager;
    private readonly ILogger<ServerCleanupService> _logger;

    public ServerCleanupService(IGameServerManager manager, ILogger<ServerCleanupService> logger)
    {
        _manager = manager;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogDebug("Running server cleanup...");
            _manager.RemoveInactiveServers();
            
            // Run cleanup every 10 seconds
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
}
