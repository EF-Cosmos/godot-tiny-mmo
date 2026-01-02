using Game.ChatService.Data;
using Game.ChatService.Models;
using Microsoft.EntityFrameworkCore;

namespace Game.ChatService.Services;

public interface IChatService
{
    Task<ChatMessage> SaveMessageAsync(Guid userId, string username, int roomId, string content);
    Task<List<ChatMessage>> GetRoomHistoryAsync(int roomId, int limit = 50, DateTime? before = null);
    Task<ChatRoom?> GetRoomAsync(int roomId);
    Task<ChatRoom?> GetRoomByNameAsync(string name);
    Task<List<ChatRoom>> GetPublicRoomsAsync();
}

public class ChatService : IChatService
{
    private readonly ChatDbContext _context;
    private readonly ILogger<ChatService> _logger;

    public ChatService(ChatDbContext context, ILogger<ChatService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ChatMessage> SaveMessageAsync(Guid userId, string username, int roomId, string content)
    {
        var message = new ChatMessage
        {
            SenderId = userId,
            SenderUsername = username,
            RoomId = roomId,
            Content = content,
            Timestamp = DateTime.UtcNow
        };

        _context.Messages.Add(message);
        await _context.SaveChangesAsync();
        return message;
    }

    public async Task<List<ChatMessage>> GetRoomHistoryAsync(int roomId, int limit = 50, DateTime? before = null)
    {
        var query = _context.Messages
            .Where(m => m.RoomId == roomId);

        if (before.HasValue)
        {
            query = query.Where(m => m.Timestamp < before.Value);
        }

        return await query
            .OrderByDescending(m => m.Timestamp)
            .Take(limit)
            .OrderBy(m => m.Timestamp)
            .ToListAsync();
    }

    public async Task<ChatRoom?> GetRoomAsync(int roomId)
    {
        return await _context.Rooms.FindAsync(roomId);
    }

    public async Task<ChatRoom?> GetRoomByNameAsync(string name)
    {
        return await _context.Rooms.FirstOrDefaultAsync(r => r.Name == name);
    }

    public async Task<List<ChatRoom>> GetPublicRoomsAsync()
    {
        return await _context.Rooms.ToListAsync();
    }
}
