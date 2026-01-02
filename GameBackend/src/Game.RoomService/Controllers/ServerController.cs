using Game.RoomService.Models;
using Game.RoomService.Services;
using Microsoft.AspNetCore.Mvc;

namespace Game.RoomService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ServerController : ControllerBase
{
    private readonly IGameServerManager _manager;

    public ServerController(IGameServerManager manager)
    {
        _manager = manager;
    }

    [HttpPost("register")]
    public ActionResult<string> Register([FromBody] RegisterServerRequest request)
    {
        var id = _manager.RegisterServer(request);
        return Ok(new { ServerId = id });
    }

    [HttpPost("heartbeat")]
    public ActionResult Heartbeat([FromBody] HeartbeatRequest request)
    {
        if (_manager.UpdateHeartbeat(request))
        {
            return Ok();
        }
        return NotFound("Server not found. Please re-register.");
    }

    [HttpGet("list")]
    public ActionResult<IEnumerable<GameServerInfo>> GetList([FromQuery] string? mapName)
    {
        return Ok(_manager.GetAvailableServers(mapName ?? ""));
    }
}
