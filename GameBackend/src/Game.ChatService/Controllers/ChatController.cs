using Game.ChatService.Models;
using Game.ChatService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Game.ChatService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(IChatService chatService, ILogger<ChatController> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    [HttpGet("rooms")]
    public async Task<ActionResult<IEnumerable<ChatRoom>>> GetRooms()
    {
        var rooms = await _chatService.GetPublicRoomsAsync();
        return Ok(rooms);
    }

    [HttpGet("rooms/{roomId}/messages")]
    public async Task<ActionResult<IEnumerable<ChatMessage>>> GetRoomHistory(int roomId, [FromQuery] int limit = 50, [FromQuery] DateTime? before = null)
    {
        var history = await _chatService.GetRoomHistoryAsync(roomId, limit, before);
        return Ok(history);
    }
}
