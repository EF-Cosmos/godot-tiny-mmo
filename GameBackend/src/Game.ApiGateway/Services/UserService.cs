using Game.Shared.Dtos;
using System.Net.Http.Json;
using System.Text;

namespace Game.ApiGateway.Services
{
    public class UserService : IUserService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public UserService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _httpClient = _httpClientFactory.CreateClient();
        }

        public async Task<UserDto> GetUserByIdAsync(Guid userId)
        {
            try
            {
                var authUrl = _configuration["Services:Auth:Url"];
                var response = await _httpClient.GetAsync($"{authUrl}/api/users/{userId}");

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<UserDto>();
                }

                throw new HttpRequestException($"User not found with ID: {userId}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get user by ID: {ex.Message}");
            }
        }

        public async Task<UserDto> GetUserByUsernameAsync(string username)
        {
            try
            {
                var authUrl = _configuration["Services:Auth:Url"];
                var response = await _httpClient.GetAsync($"{authUrl}/api/users/username/{username}");

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<UserDto>();
                }

                throw new HttpRequestException($"User not found with username: {username}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get user by username: {ex.Message}");
            }
        }

        public async Task<UserDto> CreateUserAsync(UserRegisterDto userDto)
        {
            try
            {
                var authUrl = _configuration["Services:Auth:Url"];
                var response = await _httpClient.PostAsJsonAsync($"{authUrl}/api/users/register", userDto);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<UserDto>();
                }

                throw new HttpRequestException($"Failed to create user: {userDto.Username}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create user: {ex.Message}");
            }
        }

        public async Task<UserTokenDto> LoginAsync(UserLoginDto loginDto)
        {
            try
            {
                var authUrl = _configuration["Services:Auth:Url"];
                var response = await _httpClient.PostAsJsonAsync($"{authUrl}/api/users/login", loginDto);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<UserTokenDto>();
                }

                throw new UnauthorizedAccessException("Invalid username or password");
            }
            catch (Exception ex)
            {
                throw new Exception($"Login failed: {ex.Message}");
            }
        }

        public async Task<bool> ValidateTokenAsync(string token)
        {
            try
            {
                var authUrl = _configuration["Services:Auth:Url"];
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.GetAsync($"{authUrl}/api/users/validate");

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                throw new Exception($"Token validation failed: {ex.Message}");
            }
        }
    }
}