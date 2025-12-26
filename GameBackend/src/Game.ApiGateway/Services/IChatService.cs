using Game.Shared.Models;

namespace Game.ApiGateway.Services
{
    public interface IChatService
    {
        Task<List<Message>> GetRoomMessagesAsync(Guid roomId, int limit = 50);
        Task<Message> SendMessageAsync(Message message);
        Task<bool> DeleteMessageAsync(Guid messageId);
    }
}