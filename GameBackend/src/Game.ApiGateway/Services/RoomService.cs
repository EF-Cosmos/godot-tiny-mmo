using Game.Shared.Models;
using System.Net.Http.Json;

namespace Game.ApiGateway.Services
{
    public class RoomService : IRoomService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public RoomService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _httpClient = _httpClientFactory.CreateClient();
        }

        public async Task<Room> GetRoomByIdAsync(Guid roomId)
        {
            try
            {
                var roomUrl = _configuration["Services:Room:Url"];
                var response = await _httpClient.GetAsync($"{roomUrl}/api/rooms/{roomId}");

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<Room>();
                }

                throw new HttpRequestException($"Room not found with ID: {roomId}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get room by ID: {ex.Message}");
            }
        }

        public async Task<List<Room>> GetRoomsByGameIdAsync(Guid gameId)
        {
            try
            {
                var roomUrl = _configuration["Services:Room:Url"];
                var response = await _httpClient.GetAsync($"{roomUrl}/api/rooms/game/{gameId}");

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<Room>>();
                }

                throw new HttpRequestException($"Failed to get rooms for game: {gameId}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get rooms by game ID: {ex.Message}");
            }
        }

        public async Task<List<Room>> GetAvailableRoomsAsync()
        {
            try
            {
                var roomUrl = _configuration["Services:Room:Url"];
                var response = await _httpClient.GetAsync($"{roomUrl}/api/rooms/available");

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<Room>>();
                }

                throw new HttpRequestException("Failed to get available rooms");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get available rooms: {ex.Message}");
            }
        }

        public async Task<Room> CreateRoomAsync(Room room)
        {
            try
            {
                var roomUrl = _configuration["Services:Room:Url"];
                var response = await _httpClient.PostAsJsonAsync($"{roomUrl}/api/rooms", room);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<Room>();
                }

                throw new HttpRequestException($"Failed to create room: {room.Name}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create room: {ex.Message}");
            }
        }

        public async Task<bool> JoinRoomAsync(Guid roomId, Guid userId)
        {
            try
            {
                var roomUrl = _configuration["Services:Room:Url"];
                var response = await _httpClient.PostAsync($"{roomUrl}/api/rooms/{roomId}/join/{userId}", null);

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to join room: {ex.Message}");
            }
        }

        public async Task<bool> LeaveRoomAsync(Guid roomId, Guid userId)
        {
            try
            {
                var roomUrl = _configuration["Services:Room:Url"];
                var response = await _httpClient.PostAsync($"{roomUrl}/api/rooms/{roomId}/leave/{userId}", null);

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to leave room: {ex.Message}");
            }
        }

        public async Task<bool> DeleteRoomAsync(Guid roomId)
        {
            try
            {
                var roomUrl = _configuration["Services:Room:Url"];
                var response = await _httpClient.DeleteAsync($"{roomUrl}/api/rooms/{roomId}");

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to delete room: {ex.Message}");
            }
        }
    }
}