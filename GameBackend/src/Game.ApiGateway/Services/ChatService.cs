using Game.Shared.Models;
using System.Net.Http.Json;

namespace Game.ApiGateway.Services
{
    public class ChatService : IChatService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public ChatService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _httpClient = _httpClientFactory.CreateClient();
        }

        public async Task<List<Message>> GetRoomMessagesAsync(Guid roomId, int limit = 50)
        {
            try
            {
                var chatUrl = _configuration["Services:Chat:Url"];
                var response = await _httpClient.GetAsync($"{chatUrl}/api/messages/room/{roomId}?limit={limit}");

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<Message>>();
                }

                throw new HttpRequestException($"Failed to get messages for room: {roomId}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get room messages: {ex.Message}");
            }
        }

        public async Task<Message> SendMessageAsync(Message message)
        {
            try
            {
                var chatUrl = _configuration["Services:Chat:Url"];
                var response = await _httpClient.PostAsJsonAsync($"{chatUrl}/api/messages", message);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<Message>();
                }

                throw new HttpRequestException($"Failed to send message");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to send message: {ex.Message}");
            }
        }

        public async Task<bool> DeleteMessageAsync(Guid messageId)
        {
            try
            {
                var chatUrl = _configuration["Services:Chat:Url"];
                var response = await _httpClient.DeleteAsync($"{chatUrl}/api/messages/{messageId}");

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to delete message: {ex.Message}");
            }
        }
    }
}