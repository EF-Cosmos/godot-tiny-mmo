using Microsoft.AspNetCore.Mvc;

namespace Game.ApiGateway.Controllers
{
    [ApiController]
    [Route("health")]
    public class HealthController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<HealthController> _logger;

        public HealthController(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<HealthController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var healthStatus = new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                service = "Game.ApiGateway",
                version = "1.0.0",
                port = _configuration.GetValue<int>("Consul:Port"),
                dependencies = await CheckDependenciesAsync()
            };

            return Ok(healthStatus);
        }

        private async Task<Dictionary<string, object>> CheckDependenciesAsync()
        {
            var dependencies = new Dictionary<string, object>();
            var httpClient = _httpClientFactory.CreateClient();

            // Check downstream services
            var services = new[]
            {
                new { Name = "AuthService", Url = _configuration["Services:Auth:Url"] },
                new { Name = "GameService", Url = _configuration["Services:Game:Url"] },
                new { Name = "RoomService", Url = _configuration["Services:Room:Url"] },
                new { Name = "ChatService", Url = _configuration["Services:Chat:Url"] }
            };

            foreach (var service in services)
            {
                try
                {
                    var response = await httpClient.GetAsync($"{service.Url}/health");
                    dependencies[service.Name] = new
                    {
                        status = response.IsSuccessStatusCode ? "healthy" : "unhealthy",
                        statusCode = (int)response.StatusCode,
                        url = service.Url
                    };
                }
                catch (Exception ex)
                {
                    dependencies[service.Name] = new
                    {
                        status = "unreachable",
                        error = ex.Message,
                        url = service.Url
                    };
                }
            }

            return dependencies;
        }
    }
}
