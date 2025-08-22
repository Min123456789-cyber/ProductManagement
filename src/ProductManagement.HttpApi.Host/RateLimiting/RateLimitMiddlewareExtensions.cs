using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using System;

namespace ProductManagement.RateLimiting;

public static class RateLimitMiddlewareExtensions
{
    public static IServiceCollection AddRateLimiting(
        this IServiceCollection services,
        Action<RateLimitOptions> configureOptions)
    {
        services.Configure(configureOptions);
        services.AddMemoryCache();

        var options = new RateLimitOptions();
        configureOptions(options);

        if (options.UseRedis)
        {
            services.AddSingleton<IConnectionMultiplexer>(sp =>
                ConnectionMultiplexer.Connect(options.RedisConnectionString));
        }

        return services;
    }

    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RateLimitMiddleware>();
    }
} 