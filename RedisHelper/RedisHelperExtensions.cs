using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace RedisHelper;

public static class RedisHelperExtensions
{
    public static IServiceCollection AddRedisHelper(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);

        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<RedisHelperOptions>()
            .Configure(configuration.Bind)
            .ValidateDataAnnotations();
        services.AddSingleton<IRedisHelper, RedisHelper>();
        return services;
    }

    public static IServiceCollection AddRedisHelper(this IServiceCollection services,Action<RedisHelperOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);

        ArgumentNullException.ThrowIfNull(configureOptions);

        services.Configure(configureOptions);
        services.AddSingleton<IRedisHelper, RedisHelper>();
        return services;
    }
}
