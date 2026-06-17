using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace RedisHelper;

public static class RedisHelperExtensions
{
    /// <summary>
    /// 通过 IConfiguration 注册 RedisHelper 服务
    /// </summary>
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

    /// <summary>
    /// 通过连接字符串和数据库编号注册 RedisHelper 服务
    /// </summary>
    public static IServiceCollection AddRedisHelper(this IServiceCollection services, string redisConnStr, int dbNumber)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(redisConnStr);

        services.Configure<RedisHelperOptions>(opt =>
        {
            opt.ConnectionString = redisConnStr;
            opt.DbNumber = dbNumber;
        });
        services.AddSingleton<IRedisHelper, RedisHelper>();
        return services;
    }
}
