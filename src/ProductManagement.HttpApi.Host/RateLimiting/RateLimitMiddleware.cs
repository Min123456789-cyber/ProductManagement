using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System;
using System.Threading.Tasks;

namespace ProductManagement.RateLimiting;

public class RateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;
    private readonly IConnectionMultiplexer _redis;
    private readonly RateLimitOptions _options;
    private readonly ILogger<RateLimitMiddleware> _logger;

    public RateLimitMiddleware(
        RequestDelegate next,
        IMemoryCache cache,
        IConnectionMultiplexer redis,
        IOptions<RateLimitOptions> options,
        ILogger<RateLimitMiddleware> logger)
    {
        _next = next;
        _cache = cache;
        _redis = redis;
        _options = options.Value;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        // Check if IP is whitelisted
        if (_options.WhitelistedIPs.Contains(ipAddress))
        {
            await _next(context);
            return;
        }

        var endpoint = context.Request.Path.Value?.ToLower() ?? "";
        var limit = GetEndpointLimit(endpoint);
        var key = $"rate_limit:{ipAddress}:{endpoint}";

        if (_options.UseRedis)
        {
            await HandleRedisRateLimit(context, key, limit, ipAddress);
        }
        else
        {
            await HandleMemoryRateLimit(context, key, limit, ipAddress);
        }
    }

    private int GetEndpointLimit(string endpoint)
    {
        return _options.EndpointLimits.TryGetValue(endpoint, out var limit) 
            ? limit 
            : _options.RequestsPerMinute;
    }

    private async Task HandleRedisRateLimit(HttpContext context, string key, int limit, string ipAddress)
    {
        var db = _redis.GetDatabase();
        var current = await db.StringIncrementAsync(key);
        
        if (current == 1)
        {
            await db.KeyExpireAsync(key, TimeSpan.FromMinutes(1));
        }

        if (current > limit)
        {
            await HandleRateLimitExceeded(context, ipAddress, (int)(current - limit));
            return;
        }

        await _next(context);
    }

    private async Task HandleMemoryRateLimit(HttpContext context, string key, int limit, string ipAddress)
    {
        var cacheEntry = await _cache.GetOrCreateAsync(key, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
            return new RateLimitInfo { Count = 0 };
        });

        cacheEntry.Count++;

        if (cacheEntry.Count > limit)
        {
            await HandleRateLimitExceeded(context, ipAddress, cacheEntry.Count - limit);
            return;
        }

        await _next(context);
    }

    private async Task HandleRateLimitExceeded(HttpContext context, string ipAddress, int excessRequests)
    {
        var retryAfter = _options.EnableExponentialBackoff
            ? _options.BaseBackoffSeconds * (int)Math.Pow(2, excessRequests - 1)
            : _options.BaseBackoffSeconds;

        context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.Response.Headers.Add("Retry-After", retryAfter.ToString());
        
        var response = new
        {
            error = "Too many requests",
            message = $"Rate limit exceeded. Please try again in {retryAfter} seconds.",
            retryAfter = retryAfter
        };

        await context.Response.WriteAsJsonAsync(response);
        
        _logger.LogWarning(
            "Rate limit exceeded for IP {IPAddress}. Excess requests: {ExcessRequests}, Retry after: {RetryAfter} seconds",
            ipAddress, excessRequests, retryAfter);
    }

    private class RateLimitInfo
    {
        public int Count { get; set; }
    }
} 