namespace MonitoringGrid.Api.Models;

/// <summary>
/// API documentation information
/// </summary>
public class ApiDocumentationInfo
{
    public string ApiName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public List<ControllerInfo> Controllers { get; set; } = new();
    public List<EndpointInfo> Endpoints { get; set; } = new();
    public int TotalEndpoints { get; set; }
    public List<string> AuthenticationSchemes { get; set; } = new();
    public string[] SupportedFormats { get; set; } = Array.Empty<string>();
    public RateLimitInfo RateLimits { get; set; } = new();
    public Dictionary<string, object> PerformanceMetrics { get; set; } = new();
}

/// <summary>
/// Controller information
/// </summary>
public class ControllerInfo
{
    public string Name { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public List<ActionInfo> Actions { get; set; } = new();
}

/// <summary>
/// Action information
/// </summary>
public class ActionInfo
{
    public string Name { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ReturnType { get; set; } = string.Empty;
    public List<ParameterInfo> Parameters { get; set; } = new();
}

/// <summary>
/// Parameter information
/// </summary>
public class ParameterInfo
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public string? DefaultValue { get; set; }
}

/// <summary>
/// Endpoint information
/// </summary>
public class EndpointInfo
{
    public string Path { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string Controller { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public bool RequiresAuthentication { get; set; }
    public List<string> Roles { get; set; } = new();
}

/// <summary>
/// Rate limit information
/// </summary>
public class RateLimitInfo
{
    public int DefaultLimit { get; set; }
    public int WindowSeconds { get; set; }
    public int BurstLimit { get; set; }
    public Dictionary<string, EndpointRateLimit> EndpointLimits { get; set; } = new();
}

/// <summary>
/// Endpoint-specific rate limit
/// </summary>
public class EndpointRateLimit
{
    public int Limit { get; set; }
    public int WindowSeconds { get; set; }
    public bool IsExempt { get; set; }
}

/// <summary>
/// API health information
/// </summary>
public class ApiHealthInfo
{
    public string Status { get; set; } = string.Empty;
    public DateTime CheckedAt { get; set; }
    public string Version { get; set; } = string.Empty;
    public TimeSpan Uptime { get; set; }
    public List<DependencyHealth> Dependencies { get; set; } = new();
    public long MemoryUsage { get; set; }
    public int ActiveConnections { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Dependency health information
/// </summary>
public class DependencyHealth
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Error { get; set; }
    public TimeSpan? ResponseTime { get; set; }
    public Dictionary<string, object> Details { get; set; } = new();
}

/// <summary>
/// API performance information
/// </summary>
public class ApiPerformanceInfo
{
    public DateTime CollectedAt { get; set; }
    public double AverageResponseTime { get; set; }
    public double RequestsPerSecond { get; set; }
    public double ErrorRate { get; set; }
    public double CacheHitRate { get; set; }
    public object DatabaseConnectionPool { get; set; } = new();
    public List<object> TopEndpoints { get; set; } = new();
    public List<object> RecentErrors { get; set; } = new();
}

/// <summary>
/// API metrics summary
/// </summary>
public class ApiMetricsSummary
{
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public long TotalRequests { get; set; }
    public long SuccessfulRequests { get; set; }
    public long FailedRequests { get; set; }
    public double AverageResponseTime { get; set; }
    public double MedianResponseTime { get; set; }
    public double P95ResponseTime { get; set; }
    public double P99ResponseTime { get; set; }
    public Dictionary<string, long> StatusCodeCounts { get; set; } = new();
    public Dictionary<string, long> EndpointCounts { get; set; } = new();
    public List<ErrorSummary> TopErrors { get; set; } = new();
}

/// <summary>
/// Error summary information
/// </summary>
public class ErrorSummary
{
    public string ErrorType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public long Count { get; set; }
    public DateTime FirstOccurrence { get; set; }
    public DateTime LastOccurrence { get; set; }
    public List<string> AffectedEndpoints { get; set; } = new();
}

/// <summary>
/// API usage statistics
/// </summary>
public class ApiUsageStatistics
{
    public DateTime CollectedAt { get; set; }
    public TimeSpan Period { get; set; }
    public long TotalRequests { get; set; }
    public long UniqueUsers { get; set; }
    public Dictionary<string, long> UserAgentCounts { get; set; } = new();
    public Dictionary<string, long> CountryCodeCounts { get; set; } = new();
    public Dictionary<string, long> HourlyDistribution { get; set; } = new();
    public List<TopUser> TopUsers { get; set; } = new();
    public List<PopularEndpoint> PopularEndpoints { get; set; } = new();
}

/// <summary>
/// Top user information
/// </summary>
public class TopUser
{
    public string UserId { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public long RequestCount { get; set; }
    public DateTime LastActivity { get; set; }
    public List<string> TopEndpoints { get; set; } = new();
}

/// <summary>
/// Popular endpoint information
/// </summary>
public class PopularEndpoint
{
    public string Path { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public long RequestCount { get; set; }
    public double AverageResponseTime { get; set; }
    public double ErrorRate { get; set; }
    public long UniqueUsers { get; set; }
}
