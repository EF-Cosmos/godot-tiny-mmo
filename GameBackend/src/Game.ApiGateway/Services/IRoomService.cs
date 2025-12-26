using Game.Shared.Models;

namespace Game.ApiGateway.Services
{
    public interface IRoomService
    {
        Task<Room> GetRoomByIdAsync(Guid roomId);
        Task<List<Room>> GetRoomsByGameIdAsync(Guid gameId);
        Task<List<Room>> GetAvailableRoomsAsync();
        Task<Room> CreateRoomAsync(Room room);
        Task<bool> JoinRoomAsync(Guid roomId, Guid userId);
        Task<bool> LeaveRoomAsync(Guid roomId, Guid userId);
        Task<bool> DeleteRoomAsync(Guid roomId);
    }
}