using System.Security.Claims;
using Game.GameService.Data;
using Game.GameService.DTOs;
using Game.GameService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Game.GameService.Controllers;

[ApiController]
[Route("api")]
public class WorldController : ControllerBase
{
    private readonly GameDbContext _context;
    private readonly ILogger<WorldController> _logger;

    public WorldController(GameDbContext context, ILogger<WorldController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("worlds")]
    public ActionResult<IEnumerable<WorldDto>> GetWorlds()
    {
        // In a real scenario, this would come from Service Discovery (Consul) or RoomService
        // For now, we return the local Godot World Server configuration
        return Ok(new[]
        {
            new WorldDto
            {
                Id = "world-1",
                Name = "Classic World",
                Address = "127.0.0.1", // Or the Docker host IP if running in container
                Port = 8088, // Gateway Server Port (Client connects to Gateway, not World directly)
                CurrentPlayers = 0,
                MaxPlayers = 200,
                Status = "Online"
            }
        });
    }

    [Authorize]
    [HttpPost("world/characters")]
    public async Task<ActionResult<IEnumerable<CharacterDto>>> GetCharacters()
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var characters = await _context.Characters
            .Where(c => c.UserId == userId)
            .Select(c => new CharacterDto
            {
                Id = c.Id,
                Name = c.Name,
                Level = c.Level,
                SkinColor = c.SkinColor,
                HairStyle = c.HairStyle,
                HairColor = c.HairColor,
                ShirtColor = c.ShirtColor,
                PantsColor = c.PantsColor
            })
            .ToListAsync();

        return Ok(characters);
    }

    [Authorize]
    [HttpPost("world/character/create")]
    public async Task<ActionResult<CharacterDto>> CreateCharacter([FromBody] CreateCharacterRequest request)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        if (await _context.Characters.AnyAsync(c => c.Name == request.Name))
        {
            return BadRequest("Character name already exists");
        }

        var character = new PlayerCharacter
        {
            UserId = userId,
            Name = request.Name,
            SkinColor = request.SkinColor,
            HairStyle = request.HairStyle,
            HairColor = request.HairColor,
            ShirtColor = request.ShirtColor,
            PantsColor = request.PantsColor
        };

        _context.Characters.Add(character);
        await _context.SaveChangesAsync();

        return Ok(new CharacterDto
        {
            Id = character.Id,
            Name = character.Name,
            Level = character.Level,
            SkinColor = character.SkinColor,
            HairStyle = character.HairStyle,
            HairColor = character.HairColor,
            ShirtColor = character.ShirtColor,
            PantsColor = character.PantsColor
        });
    }

    [Authorize]
    [HttpPost("world/enter")]
    public ActionResult<EnterWorldResponse> EnterWorld([FromBody] EnterWorldRequest request)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        // Verify character belongs to user
        var character = _context.Characters.FirstOrDefault(c => c.Id == request.CharacterId && c.UserId == userId);
        if (character == null)
        {
            return BadRequest("Character not found or does not belong to user");
        }

        // Generate a one-time token for the Godot Server
        // In a real implementation, this would be signed or stored in Redis for the Game Server to verify
        var token = $"{userId}:{character.Id}:{Guid.NewGuid()}";

        return Ok(new EnterWorldResponse
        {
            Token = token,
            Host = "127.0.0.1",
            Port = 8088, // Gateway Server Port
            CharacterId = character.Id
        });
    }

    private Guid GetUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
        if (idClaim != null && Guid.TryParse(idClaim.Value, out var guid))
        {
            return guid;
        }
        return Guid.Empty;
    }
}
