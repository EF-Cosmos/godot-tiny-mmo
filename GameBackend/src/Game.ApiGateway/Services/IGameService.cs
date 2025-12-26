using Game.Shared.Models;
using GameModel = Game.Shared.Models.Game;

namespace Game.ApiGateway.Services
{
    public interface IGameService
    {
        Task<GameModel> GetGameByIdAsync(Guid gameId);
        Task<List<GameModel>> GetAllGamesAsync();
        Task<GameModel> CreateGameAsync(GameModel game);
        Task<bool> DeleteGameAsync(Guid gameId);
    }
}
