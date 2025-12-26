using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Game.Shared.Consul;

public static class ConsulExtensions
{
    public static IServiceCollection AddConsulServiceDiscovery(
        this IServiceCollection services,
        Action<ConsulServiceOptions> configure)
    {
        var options = new ConsulServiceOptions();
        configure(options);

        // Ensure ServiceId is set (default to Guid)
        if (string.IsNullOrEmpty(options.ServiceId))
        {
            options.ServiceId = $"{options.ServiceName}-{Guid.NewGuid()}";
        }

        services.AddSingleton(options);
        services.AddSingleton(sp => new ConsulClient(options.ConsulHost, options.ConsulPort));
        services.AddHostedService<ConsulHostedService>();

        return services;
    }

    public static IServiceCollection AddConsulServiceDiscovery(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = configuration.GetSection("Consul").Get<ConsulServiceOptions>()
            ?? new ConsulServiceOptions();

        // Ensure ServiceId is set
        if (string.IsNullOrEmpty(options.ServiceId))
        {
            options.ServiceId = $"{options.ServiceName}-{Guid.NewGuid()}";
        }

        services.AddSingleton(options);
        services.AddSingleton(sp => new ConsulClient(options.ConsulHost, options.ConsulPort));
        services.AddHostedService<ConsulHostedService>();

        return services;
    }
}
