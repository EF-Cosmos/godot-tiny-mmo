using Microsoft.Extensions.Hosting;

namespace Game.Shared.Consul;

public class ConsulHostedService : IHostedService
{
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ConsulClient _consulClient;
    private readonly ConsulServiceOptions _options;

    public ConsulHostedService(
        IHostApplicationLifetime lifetime,
        ConsulClient consulClient,
        ConsulServiceOptions options)
    {
        _lifetime = lifetime;
        _consulClient = consulClient;
        _options = options;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Register service with Consul
        await _consulClient.RegisterServiceAsync(_options);

        // Deregister on shutdown
        _lifetime.ApplicationStopping.Register(async () =>
        {
            await _consulClient.DeregisterServiceAsync(_options.ServiceId);
        });
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        // Deregister is handled by ApplicationStopping event
        return Task.CompletedTask;
    }
}
