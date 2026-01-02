using Game.ChatService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Game.ChatService.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IChatService _chatService;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(IChatService chatService, ILogger<ChatHub> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        var username = Context.User?.FindFirst(ClaimTypes.Name)?.Value;
        
        _logger.LogInformation($"User connected: {username} ({userId})");
        await base.OnConnectedAsync();
    }

    public async Task JoinRoom(string roomName)
    {
        var room = await _chatService.GetRoomByNameAsync(roomName);
        if (room == null)
        {
            await Clients.Caller.SendAsync("Error", "Room not found");
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, room.Id.ToString());
        await Clients.Caller.SendAsync("JoinedRoom", room.Id, room.Name);
        
        // Notify others
        var username = Context.User?.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
        await Clients.Group(room.Id.ToString()).SendAsync("UserJoined", new { Username = username });

        // Send history
        var history = await _chatService.GetRoomHistoryAsync(room.Id);
        await Clients.Caller.SendAsync("ReceiveHistory", history);
    }

    public async Task LeaveRoom(string roomName)
    {
        var room = await _chatService.GetRoomByNameAsync(roomName);
        if (room == null) return;

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, room.Id.ToString());
        
        var username = Context.User?.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
        await Clients.Group(room.Id.ToString()).SendAsync("UserLeft", new { Username = username });
    }

    public async Task SendMessage(int roomId, string content)
    {
        var userIdStr = Context.UserIdentifier;
        if (!Guid.TryParse(userIdStr, out var userId))
        {
            return;
        }

        var username = Context.User?.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";

        // Save to DB
        var message = await _chatService.SaveMessageAsync(userId, username, roomId, content);

        // Broadcast to room
        await Clients.Group(roomId.ToString()).SendAsync("ReceiveMessage", message);
    }

    public async Task SendPrivateMessage(Guid recipientId, string content)
    {
        var senderId = Context.UserIdentifier;
        var senderName = Context.User?.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
        
        await Clients.User(recipientId.ToString()).SendAsync("ReceivePrivateMessage", new 
        { 
            SenderId = senderId, 
            SenderUsername = senderName, 
            Content = content,
            Timestamp = DateTime.UtcNow
        });
    }
}
