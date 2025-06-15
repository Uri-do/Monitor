namespace MonitoringGrid.Api.Configuration;

/// <summary>
/// Configuration options for rate limiting
/// </summary>
public class RateLimitingOptions
{
    /// <summary>
    /// Whether rate limiting is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Maximum number of requests per window
    /// </summary>
    public int MaxRequests { get; set; } = 100;

    /// <summary>
    /// Time window in seconds
    /// </summary>
    public int WindowSeconds { get; set; } = 60;

    /// <summary>
    /// Block duration in seconds when limit is exceeded
    /// </summary>
    public int BlockDurationSeconds { get; set; } = 300;

    /// <summary>
    /// Whether to use distributed cache for rate limiting
    /// </summary>
    public bool UseDistributedCache { get; set; } = false;

    /// <summary>
    /// Custom rate limit rules by endpoint
    /// </summary>
    public Dictionary<string, EndpointRateLimit> EndpointLimits { get; set; } = new();
}

/// <summary>
/// Rate limit configuration for specific endpoints
/// </summary>
public class EndpointRateLimit
{
    /// <summary>
    /// Maximum requests for this endpoint
    /// </summary>
    public int MaxRequests { get; set; }

    /// <summary>
    /// Time window in seconds for this endpoint
    /// </summary>
    public int WindowSeconds { get; set; }

    /// <summary>
    /// Whether this endpoint is exempt from global rate limiting
    /// </summary>
    public bool IsExempt { get; set; }
}
