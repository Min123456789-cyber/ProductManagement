using System.Collections.Generic;

namespace ProductManagement.RateLimiting;

public class RateLimitOptions
{
    public int RequestsPerMinute { get; set; } = 5;
    public List<string> WhitelistedIPs { get; set; } = new();
    public Dictionary<string, int> EndpointLimits { get; set; } = new();
    public bool UseRedis { get; set; }
    public string RedisConnectionString { get; set; }
    public bool EnableExponentialBackoff { get; set; }
    public int BaseBackoffSeconds { get; set; } = 60;
} 