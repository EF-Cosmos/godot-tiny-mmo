namespace Game.Shared.Consul;

public class ConsulServiceOptions
{
    public string ServiceName { get; set; } = string.Empty;
    public string ServiceId { get; set; } = string.Empty;
    public string Host { get; set; } = "localhost";
    public int Port { get; set; }
    public string ConsulHost { get; set; } = "localhost";
    public int ConsulPort { get; set; } = 8500;
    public string[]? Tags { get; set; }
    public HealthCheckOptions? HealthCheck { get; set; }

    public class HealthCheckOptions
    {
        public string Endpoint { get; set; } = "/health";
        public int IntervalSeconds { get; set; } = 10;
        public int TimeoutSeconds { get; set; } = 5;
        public int DeregisterCriticalServicesAfterSeconds { get; set; } = 30;
    }
}
