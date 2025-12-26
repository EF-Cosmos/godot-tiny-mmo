using Microsoft.AspNetCore.Mvc;
using Game.Shared.Models;
using Game.ApiGateway.Services;

namespace Game.ApiGateway.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RoomsController : ControllerBase
    {
        private readonly IRoomService _roomService;

        public RoomsController(IRoomService roomService)
        {
            _roomService = roomService;
        }

        [HttpGet("{roomId}")]
        public async Task<ActionResult<Room>> GetRoomById(Guid roomId)
        {
            try
            {
                var room = await _roomService.GetRoomByIdAsync(roomId);
                return Ok(room);
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpGet("game/{gameId}")]
        public async Task<ActionResult<List<Room>>> GetRoomsByGameId(Guid gameId)
        {
            try
            {
                var rooms = await _roomService.GetRoomsByGameIdAsync(gameId);
                return Ok(rooms);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("available")]
        public async Task<ActionResult<List<Room>>> GetAvailableRooms()
        {
            try
            {
                var rooms = await _roomService.GetAvailableRoomsAsync();
                return Ok(rooms);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost]
        public async Task<ActionResult<Room>> CreateRoom([FromBody] Room room)
        {
            try
            {
                var createdRoom = await _roomService.CreateRoomAsync(room);
                return CreatedAtAction(nameof(GetRoomById), new { roomId = createdRoom.Id }, createdRoom);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("{roomId}/join/{userId}")]
        public async Task<IActionResult> JoinRoom(Guid roomId, Guid userId)
        {
            try
            {
                var success = await _roomService.JoinRoomAsync(roomId, userId);
                return success ? Ok() : NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("{roomId}/leave/{userId}")]
        public async Task<IActionResult> LeaveRoom(Guid roomId, Guid userId)
        {
            try
            {
                var success = await _roomService.LeaveRoomAsync(roomId, userId);
                return success ? Ok() : NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpDelete("{roomId}")]
        public async Task<IActionResult> DeleteRoom(Guid roomId)
        {
            try
            {
                var success = await _roomService.DeleteRoomAsync(roomId);
                return success ? NoContent() : NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}