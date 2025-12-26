using Microsoft.AspNetCore.Mvc;
using Game.Shared.Models;
using Game.ApiGateway.Services;

namespace Game.ApiGateway.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;

        public ChatController(IChatService chatService)
        {
            _chatService = chatService;
        }

        [HttpGet("room/{roomId}")]
        public async Task<ActionResult<List<Message>>> GetRoomMessages(Guid roomId, [FromQuery] int limit = 50)
        {
            try
            {
                var messages = await _chatService.GetRoomMessagesAsync(roomId, limit);
                return Ok(messages);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost]
        public async Task<ActionResult<Message>> SendMessage([FromBody] Message message)
        {
            try
            {
                var sentMessage = await _chatService.SendMessageAsync(message);
                return CreatedAtAction(nameof(GetRoomMessages), new { roomId = message.RoomId }, sentMessage);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpDelete("{messageId}")]
        public async Task<IActionResult> DeleteMessage(Guid messageId)
        {
            try
            {
                var success = await _chatService.DeleteMessageAsync(messageId);
                return success ? NoContent() : NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}