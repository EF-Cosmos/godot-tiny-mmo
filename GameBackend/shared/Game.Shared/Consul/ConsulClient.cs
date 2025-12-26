using System.Text.Json;

namespace Game.Shared.Consul;

public class ConsulClient
{
    private readonly HttpClient _httpClient;
    private readonly string _consulUrl;

    public ConsulClient(string consulHost = "localhost", int consulPort = 8500)
    {
        _consulUrl = $"http://{consulHost}:{consulPort}";
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_consulUrl)
        };
    }

    public async Task<bool> RegisterServiceAsync(ConsulServiceOptions options)
    {
        var registration = new
        {
            ID = options.ServiceId,
            Name = options.ServiceName,
            Tags = options.Tags ?? Array.Empty<string>(),
            Address = options.Host,
            Port = options.Port,
            Check = options.HealthCheck != null ? new
            {
                HTTP = $"http://{options.Host}:{options.Port}{options.HealthCheck.Endpoint}",
                Interval = $"{options.HealthCheck.IntervalSeconds}s",
                Timeout = $"{options.HealthCheck.TimeoutSeconds}s",
                DeregisterCriticalServiceAfter = $"{options.HealthCheck.DeregisterCriticalServicesAfterSeconds}s"
            } : null
        };

        var content = new StringContent(
            JsonSerializer.Serialize(registration),
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await _httpClient.PutAsync("/v1/agent/service/register", content);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeregisterServiceAsync(string serviceId)
    {
        var response = await _httpClient.PutAsync($"/v1/agent/service/deregister/{serviceId}", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<string[]?> GetServiceAsync(string serviceName)
    {
        var response = await _httpClient.GetAsync($"/v1/health/service/{serviceName}?passing=true");
        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var services = doc.RootElement.EnumerateArray()
            .Select(x => x.GetProperty("Service").GetProperty("Address").GetString() + ":" +
                         x.GetProperty("Service").GetProperty("Port").GetInt32())
            .ToArray();

        return services;
    }
}
