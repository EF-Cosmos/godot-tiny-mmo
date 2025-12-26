using Game.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Game.ChatService.Services;

public interface IChatService
{
    Task<bool> ValidateMessageAsync(string content);
    Task<string> FilterMessageAsync(string content);
    Task<Message> SaveMessageAsync(Message message);
    Task<List<Message>> GetRoomHistoryAsync(Guid roomId, int limit, DateTime? before = null);
    Task<bool> UserCanJoinRoomAsync(Guid userId, Guid roomId);
    Task<User?> GetOrCreateUserAsync(Guid userId, string username, string? avatar = null);
}

public class ChatService : IChatService
{
    private readonly ChatDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ChatService> _logger;
    private readonly List<string> _bannedWords;

    public ChatService(ChatDbContext context, IConfiguration configuration, ILogger<ChatService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
        _bannedWords = LoadBannedWords();
    }

    public async Task<bool> ValidateMessageAsync(string content)
    {
        var maxLength = _configuration.GetValue<int>("ChatService:MaxMessageLength", 500);
        var enableFilter = _configuration.GetValue<bool>("ChatService:EnableContentFilter", true);

        // Check length
        if (string.IsNullOrWhiteSpace(content) || content.Length > maxLength)
            return false;

        // Check for banned content
        if (enableFilter && ContainsBannedContent(content))
            return false;

        return true;
    }

    public async Task<string> FilterMessageAsync(string content)
    {
        var enableFilter = _configuration.GetValue<bool>("ChatService:EnableContentFilter", true);
        if (!enableFilter)
            return content;

        var filteredContent = content;

        // Replace banned words with asterisks
        foreach (var word in _bannedWords)
        {
            filteredContent = System.Text.RegularExpressions.Regex.Replace(
                filteredContent,
                word,
                new string('*', word.Length),
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        return filteredContent;
    }

    public async Task<Message> SaveMessageAsync(Message message)
    {
        message.Status = MessageStatus.Delivered;
        message.Timestamp = DateTime.UtcNow;

        _context.Messages.Add(message);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Message saved: {MessageId} from {SenderId} in room {RoomId}",
            message.Id, message.SenderId, message.RoomId);

        return message;
    }

    public async Task<List<Message>> GetRoomHistoryAsync(Guid roomId, int limit, DateTime? before = null)
    {
        var maxLimit = _configuration.GetValue<int>("ChatService:MessageHistoryLimit", 100);
        limit = Math.Min(limit, maxLimit);

        var query = _context.Messages
            .Where(m => m.RoomId == roomId)
            .OrderByDescending(m => m.Timestamp)
            .Take(limit);

        if (before.HasValue)
        {
            query = query.Where(m => m.Timestamp < before.Value);
        }

        return await query.OrderBy(m => m.Timestamp).ToListAsync();
    }

    public async Task<bool> UserCanJoinRoomAsync(Guid userId, Guid roomId)
    {
        var room = await _context.Rooms.FindAsync(roomId);
        if (room == null)
            return false;

        // Public rooms can be joined by anyone
        if (room.Type == RoomType.Public)
            return true;

        // Private rooms need additional logic (room members, invitations, etc.)
        // For now, allow joining private rooms
        return true;
    }

    public async Task<User?> GetOrCreateUserAsync(Guid userId, string username, string? avatar = null)
    {
        var user = await _context.Users.FindAsync(userId);

        if (user == null)
        {
            user = new User
            {
                Id = userId,
                Username = username,
                Avatar = avatar ?? string.Empty,
                Email = string.Empty,
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow,
                Status = UserStatus.Online,
                Level = 1,
                Experience = 0
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("New user created: {UserId} - {Username}", userId, username);
        }
        else
        {
            // Update last login and status
            user.LastLoginAt = DateTime.UtcNow;
            user.Status = UserStatus.Online;

            if (!string.IsNullOrEmpty(avatar))
                user.Avatar = avatar;

            await _context.SaveChangesAsync();
        }

        return user;
    }

    private List<string> LoadBannedWords()
    {
        // In a real implementation, this would load from a configuration file or database
        // For now, return a basic list
        return new List<string>
        {
            "spam", "abuse", "hate", "vulgar"
            // Add more banned words as needed
        };
    }

    private bool ContainsBannedContent(string content)
    {
        var contentLower = content.ToLower();

        foreach (var word in _bannedWords)
        {
            if (contentLower.Contains(word.ToLower()))
                return true;
        }

        return false;
    }
}