using Game.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Game.ChatService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly ChatDbContext _context;
    private readonly ILogger<ChatController> _logger;
    private readonly IConfiguration _configuration;

    public ChatController(ChatDbContext context, ILogger<ChatController> logger, IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
    }

    [HttpGet("rooms")]
    public async Task<ActionResult<IEnumerable<RoomDto>>> GetRooms()
    {
        var rooms = await _context.Rooms
            .Where(r => r.Type == RoomType.Public)
            .Select(r => new RoomDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                Type = r.Type.ToString(),
                MaxPlayers = r.MaxPlayers,
                CurrentPlayers = r.CurrentPlayers
            })
            .ToListAsync();

        return Ok(rooms);
    }

    [HttpGet("rooms/{roomId}/messages")]
    public async Task<ActionResult<MessageHistoryDto>> GetRoomMessages(
        Guid roomId,
        [FromQuery] int limit = 50,
        [FromQuery] DateTime? before = null)
    {
        var maxMessageLength = _configuration.GetValue<int>("ChatService:MaxMessageLength", 500);
        var messageHistoryLimit = _configuration.GetValue<int>("ChatService:MessageHistoryLimit", 100);

        limit = Math.Min(limit, messageHistoryLimit);

        var query = _context.Messages
            .Where(m => m.RoomId == roomId && !m.IsSystem)
            .OrderByDescending(m => m.Timestamp)
            .Take(limit);

        if (before.HasValue)
        {
            query = query.Where(m => m.Timestamp < before.Value);
        }

        var messages = await query
            .OrderBy(m => m.Timestamp)
            .Select(m => new MessageDto
            {
                Id = m.Id,
                SenderId = m.SenderId,
                SenderUsername = m.SenderUsername,
                Content = m.Content,
                Type = m.Type.ToString(),
                Timestamp = m.Timestamp,
                Status = m.Status.ToString()
            })
            .ToListAsync();

        return Ok(new MessageHistoryDto
        {
            Messages = messages,
            HasMore = messages.Count == limit
        });
    }

    [HttpPost("rooms")]
    public async Task<ActionResult<RoomDto>> CreateRoom([FromBody] CreateRoomRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var room = new Room
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            Type = RoomType.Private,
            Status = RoomStatus.Waiting,
            MaxPlayers = request.MaxPlayers ?? 50,
            CurrentPlayers = 0,
            HostId = Guid.Parse(User.Identity?.Name ?? Guid.Empty.ToString()),
            CreatedAt = DateTime.UtcNow,
            Settings = request.Settings ?? new Dictionary<string, string>()
        };

        _context.Rooms.Add(room);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetRooms), new { roomId = room.Id }, new RoomDto
        {
            Id = room.Id,
            Name = room.Name,
            Description = room.Description,
            Type = room.Type.ToString(),
            MaxPlayers = room.MaxPlayers,
            CurrentPlayers = room.CurrentPlayers
        });
    }

    [HttpGet("users/{userId}")]
    public async Task<ActionResult<UserDto>> GetUser(Guid userId)
    {
        var user = await _context.Users
            .Where(u => u.Id == userId)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Username = u.Username,
                Avatar = u.Avatar,
                Status = u.Status.ToString(),
                Level = u.Level,
                LastLoginAt = u.LastLoginAt
            })
            .FirstOrDefaultAsync();

        if (user == null)
            return NotFound();

        return Ok(user);
    }

    [HttpPost("users/{userId}/register")]
    [AllowAnonymous]
    public async Task<ActionResult<UserDto>> RegisterUser(Guid userId, [FromBody] RegisterUserRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var existingUser = await _context.Users.FindAsync(userId);
        if (existingUser != null)
            return Conflict("User already exists");

        var user = new User
        {
            Id = userId,
            Username = request.Username,
            Email = request.Email,
            Avatar = request.Avatar,
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow,
            Status = UserStatus.Online,
            Level = request.Level ?? 1,
            Experience = request.Experience ?? 0
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok(new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Avatar = user.Avatar,
            Status = user.Status.ToString(),
            Level = user.Level,
            LastLoginAt = user.LastLoginAt
        });
    }

    [HttpPut("users/{userId}/status")]
    public async Task<ActionResult> UpdateUserStatus(Guid userId, [FromBody] UpdateUserStatusRequest request)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return NotFound();

        if (Enum.TryParse<UserStatus>(request.Status, out var newStatus))
        {
            user.Status = newStatus;
            if (newStatus == UserStatus.Online)
                user.LastLoginAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("health")]
    [AllowAnonymous]
    public ActionResult HealthCheck()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }
}

// DTOs
public class RoomDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int MaxPlayers { get; set; }
    public int CurrentPlayers { get; set; }
}

public class MessageDto
{
    public Guid Id { get; set; }
    public Guid SenderId { get; set; }
    public string SenderUsername { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class MessageHistoryDto
{
    public List<MessageDto> Messages { get; set; } = new();
    public bool HasMore { get; set; }
}

public class UserDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Avatar { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int Level { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

// Request DTOs
public class CreateRoomRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    public int? MaxPlayers { get; set; }
    public Dictionary<string, string>? Settings { get; set; }
}

public class RegisterUserRequest
{
    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Avatar { get; set; } = string.Empty;

    public int? Level { get; set; }
    public int? Experience { get; set; }
}

public class UpdateUserStatusRequest
{
    [Required]
    public string Status { get; set; } = string.Empty;
}