using Game.Shared.Models;
using System.Net.Http.Json;
using GameModel = Game.Shared.Models.Game;

namespace Game.ApiGateway.Services
{
    public class GameService : IGameService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public GameService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _httpClient = _httpClientFactory.CreateClient();
        }

        public async Task<GameModel> GetGameByIdAsync(Guid gameId)
        {
            try
            {
                var gameUrl = _configuration["Services:Game:Url"];
                var response = await _httpClient.GetAsync($"{gameUrl}/api/games/{gameId}");

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<GameModel>();
                }

                throw new HttpRequestException($"Game not found with ID: {gameId}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get game by ID: {ex.Message}");
            }
        }

        public async Task<List<GameModel>> GetAllGamesAsync()
        {
            try
            {
                var gameUrl = _configuration["Services:Game:Url"];
                var response = await _httpClient.GetAsync($"{gameUrl}/api/games");

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<GameModel>>();
                }

                throw new HttpRequestException("Failed to get games");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get games: {ex.Message}");
            }
        }

        public async Task<GameModel> CreateGameAsync(GameModel game)
        {
            try
            {
                var gameUrl = _configuration["Services:Game:Url"];
                var response = await _httpClient.PostAsJsonAsync($"{gameUrl}/api/games", game);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<GameModel>();
                }

                throw new HttpRequestException($"Failed to create game: {game.Name}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create game: {ex.Message}");
            }
        }

        public async Task<bool> DeleteGameAsync(Guid gameId)
        {
            try
            {
                var gameUrl = _configuration["Services:Game:Url"];
                var response = await _httpClient.DeleteAsync($"{gameUrl}/api/games/{gameId}");

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to delete game: {ex.Message}");
            }
        }
    }
}
