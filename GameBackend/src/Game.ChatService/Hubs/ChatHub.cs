using Game.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Game.ChatService.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private static readonly Dictionary<string, HashSet<string>> RoomConnections = new();
    private static readonly Dictionary<string, UserConnectionInfo> ConnectedUsers = new();

    public async Task JoinRoom(Guid roomId, string username)
    {
        var connectionId = Context.ConnectionId;
        var userId = Context.UserIdentifier;

        if (userId == null) return;

        // Add connection to room
        if (!RoomConnections.TryGetValue(roomId.ToString(), out var connections))
        {
            connections = new HashSet<string>();
            RoomConnections[roomId.ToString()] = connections;
        }

        connections.Add(connectionId);

        // Update user info
        ConnectedUsers[connectionId] = new UserConnectionInfo
        {
            UserId = Guid.Parse(userId),
            Username = username,
            RoomId = roomId,
            ConnectedAt = DateTime.UtcNow
        };

        // Join SignalR group
        await Groups.AddToGroupAsync(connectionId, roomId.ToString());

        // Notify others in room
        await Clients.OthersInGroup(roomId.ToString()).SendAsync("UserJoined", new
        {
            UserId = userId,
            Username = username,
            Timestamp = DateTime.UtcNow
        });
    }

    public async Task LeaveRoom(Guid roomId)
    {
        var connectionId = Context.ConnectionId;
        var userId = Context.UserIdentifier;

        if (userId == null) return;

        // Remove from room
        if (RoomConnections.TryGetValue(roomId.ToString(), out var connections))
        {
            connections.Remove(connectionId);
            if (connections.Count == 0)
            {
                RoomConnections.Remove(roomId.ToString());
            }
        }

        // Remove user info
        ConnectedUsers.Remove(connectionId);

        // Leave SignalR group
        await Groups.RemoveFromGroupAsync(connectionId, roomId.ToString());

        // Notify others in room
        await Clients.OthersInGroup(roomId.ToString()).SendAsync("UserLeft", new
        {
            UserId = userId,
            Timestamp = DateTime.UtcNow
        });
    }

    public async Task SendMessage(Guid roomId, string content, MessageType messageType = MessageType.Text)
    {
        var connectionId = Context.ConnectionId;
        var userId = Context.UserIdentifier;

        if (userId == null || !ConnectedUsers.TryGetValue(connectionId, out var userInfo))
            return;

        var message = new Message
        {
            Id = Guid.NewGuid(),
            RoomId = roomId,
            SenderId = Guid.Parse(userId),
            SenderUsername = userInfo.Username,
            Content = content,
            Type = messageType,
            Timestamp = DateTime.UtcNow,
            Status = MessageStatus.Delivered,
            IsSystem = false
        };

        // Broadcast to all users in room
        await Clients.Group(roomId.ToString()).SendAsync("ReceiveMessage", message);
    }

    public async Task SendPrivateMessage(Guid recipientId, string content)
    {
        var connectionId = Context.ConnectionId;
        var senderId = Context.UserIdentifier;

        if (senderId == null || !ConnectedUsers.TryGetValue(connectionId, out var senderInfo))
            return;

        // Find recipient connections
        var recipientConnections = ConnectedUsers.Values
            .Where(u => u.UserId == recipientId)
            .Select(u => ConnectedUsers.FirstOrDefault(kv => kv.Value == u).Key)
            .ToList();

        if (recipientConnections.Count == 0) return;

        var message = new Message
        {
            Id = Guid.NewGuid(),
            RoomId = Guid.Empty, // Private messages don't belong to a room
            SenderId = Guid.Parse(senderId),
            SenderUsername = senderInfo.Username,
            Content = content,
            Type = MessageType.Private,
            Timestamp = DateTime.UtcNow,
            Status = MessageStatus.Delivered,
            IsSystem = false,
            RecipientId = recipientId.ToString()
        };

        // Send to sender and recipient
        await Clients.Clients(recipientConnections.Concat(new[] { connectionId }))
            .SendAsync("ReceivePrivateMessage", message);
    }

    public async Task GetOnlineUsers(Guid roomId)
    {
        var onlineUsers = ConnectedUsers.Values
            .Where(u => u.RoomId == roomId)
            .Select(u => new
            {
                u.UserId,
                u.Username,
                Status = UserStatus.Online,
                u.ConnectedAt
            })
            .ToList();

        await Clients.Caller.SendAsync("OnlineUsers", onlineUsers);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var connectionId = Context.ConnectionId;
        var userId = Context.UserIdentifier;

        if (ConnectedUsers.TryGetValue(connectionId, out var userInfo))
        {
            // Remove from room
            if (RoomConnections.TryGetValue(userInfo.RoomId.ToString(), out var connections))
            {
                connections.Remove(connectionId);
                if (connections.Count == 0)
                {
                    RoomConnections.Remove(userInfo.RoomId.ToString());
                }
            }

            // Remove user info
            ConnectedUsers.Remove(connectionId);

            // Notify others in room
            await Clients.OthersInGroup(userInfo.RoomId.ToString()).SendAsync("UserLeft", new
            {
                UserId = userId,
                Timestamp = DateTime.UtcNow
            });
        }

        await base.OnDisconnectedAsync(exception);
    }

    private class UserConnectionInfo
    {
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public Guid RoomId { get; set; }
        public DateTime ConnectedAt { get; set; }
    }
}