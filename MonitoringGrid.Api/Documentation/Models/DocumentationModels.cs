namespace MonitoringGrid.Api.Documentation.Models;

/// <summary>
/// Comprehensive API documentation model
/// </summary>
public class ApiDocumentation
{
    public DateTime GeneratedAt { get; set; }
    public string Version { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public List<EndpointDocumentation> Endpoints { get; set; } = new();
    public List<ApiExample> Examples { get; set; } = new();
    public PerformanceDocumentation Performance { get; set; } = new();
    public SecurityDocumentation Security { get; set; } = new();
    public ArchitectureDocumentation Architecture { get; set; } = new();
}

/// <summary>
/// Documentation for a specific API endpoint
/// </summary>
public class EndpointDocumentation
{
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<ParameterDocumentation> Parameters { get; set; } = new();
    public RequestBodyDocumentation? RequestBody { get; set; }
    public List<ResponseDocumentation> Responses { get; set; } = new();
    public RateLimitDocumentation? RateLimits { get; set; }
    public CachingDocumentation? CachingInfo { get; set; }
    public List<string> Tags { get; set; } = new();
}

/// <summary>
/// Parameter documentation
/// </summary>
public class ParameterDocumentation
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Required { get; set; }
    public string Location { get; set; } = "query"; // query, path, header, body
    public string? DefaultValue { get; set; }
    public string? Example { get; set; }
}

/// <summary>
/// Request body documentation
/// </summary>
public class RequestBodyDocumentation
{
    public string ContentType { get; set; } = "application/json";
    public string Description { get; set; } = string.Empty;
    public string Example { get; set; } = string.Empty;
    public string Schema { get; set; } = string.Empty;
    public bool Required { get; set; } = true;
}

/// <summary>
/// Response documentation
/// </summary>
public class ResponseDocumentation
{
    public int StatusCode { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Example { get; set; } = string.Empty;
    public string Schema { get; set; } = string.Empty;
    public Dictionary<string, string> Headers { get; set; } = new();
}

/// <summary>
/// Rate limiting documentation
/// </summary>
public class RateLimitDocumentation
{
    public int RequestsPerMinute { get; set; }
    public int BurstSize { get; set; }
    public string Scope { get; set; } = string.Empty;
    public bool AutoBlockingEnabled { get; set; }
    public TimeSpan? BlockDuration { get; set; }
}

/// <summary>
/// Caching documentation
/// </summary>
public class CachingDocumentation
{
    public bool CacheEnabled { get; set; }
    public TimeSpan CacheDuration { get; set; }
    public bool ETagSupport { get; set; }
    public string[] VaryHeaders { get; set; } = Array.Empty<string>();
    public string CacheStrategy { get; set; } = string.Empty;
}

/// <summary>
/// API example documentation
/// </summary>
public class ApiExample
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public Dictionary<string, string> Headers { get; set; } = new();
    public string? RequestBody { get; set; }
    public string Response { get; set; } = string.Empty;
    public int ExpectedStatusCode { get; set; } = 200;
}

/// <summary>
/// Performance documentation
/// </summary>
public class PerformanceDocumentation
{
    public CachingStrategyDocumentation CachingStrategy { get; set; } = new();
    public RateLimitingDocumentation RateLimiting { get; set; } = new();
    public CompressionDocumentation Compression { get; set; } = new();
    public DatabaseOptimizationDocumentation DatabaseOptimization { get; set; } = new();
}

/// <summary>
/// Caching strategy documentation
/// </summary>
public class CachingStrategyDocumentation
{
    public string Description { get; set; } = string.Empty;
    public string MemoryCacheSize { get; set; } = string.Empty;
    public string DistributedCacheType { get; set; } = string.Empty;
    public TimeSpan DefaultCacheDuration { get; set; }
    public bool CacheWarmupEnabled { get; set; }
    public bool TagBasedInvalidation { get; set; }
}

/// <summary>
/// Rate limiting documentation
/// </summary>
public class RateLimitingDocumentation
{
    public string Description { get; set; } = string.Empty;
    public string IpLimits { get; set; } = string.Empty;
    public string UserLimits { get; set; } = string.Empty;
    public string EndpointLimits { get; set; } = string.Empty;
    public bool AutoBlockingEnabled { get; set; }
    public bool ThreatDetectionEnabled { get; set; }
}

/// <summary>
/// Compression documentation
/// </summary>
public class CompressionDocumentation
{
    public string Description { get; set; } = string.Empty;
    public string[] Algorithms { get; set; } = Array.Empty<string>();
    public string MinimumSize { get; set; } = string.Empty;
    public string CompressionLevel { get; set; } = string.Empty;
    public bool ETagSupport { get; set; }
}

/// <summary>
/// Database optimization documentation
/// </summary>
public class DatabaseOptimizationDocumentation
{
    public string Description { get; set; } = string.Empty;
    public string SlowQueryThreshold { get; set; } = string.Empty;
    public string CriticalQueryThreshold { get; set; } = string.Empty;
    public bool AutoMaintenanceEnabled { get; set; }
    public bool ConnectionPoolOptimization { get; set; }
    public bool QueryOptimizationSuggestions { get; set; }
}

/// <summary>
/// Security documentation
/// </summary>
public class SecurityDocumentation
{
    public AuthenticationDocumentation Authentication { get; set; } = new();
    public SecurityRateLimitingDocumentation RateLimiting { get; set; } = new();
    public SecurityHeadersDocumentation SecurityHeaders { get; set; } = new();
}

/// <summary>
/// Authentication documentation
/// </summary>
public class AuthenticationDocumentation
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TokenExpiry { get; set; } = string.Empty;
    public bool RefreshTokenSupport { get; set; }
    public bool ReplayProtection { get; set; }
    public bool SuspiciousActivityDetection { get; set; }
}

/// <summary>
/// Security rate limiting documentation
/// </summary>
public class SecurityRateLimitingDocumentation
{
    public string Description { get; set; } = string.Empty;
    public bool MultiDimensionalLimits { get; set; }
    public bool AutomaticBlocking { get; set; }
    public bool ThreatDetection { get; set; }
    public bool SecurityEventLogging { get; set; }
}

/// <summary>
/// Security headers documentation
/// </summary>
public class SecurityHeadersDocumentation
{
    public string Description { get; set; } = string.Empty;
    public bool HSTS { get; set; }
    public bool ContentSecurityPolicy { get; set; }
    public bool XFrameOptions { get; set; }
    public bool XContentTypeOptions { get; set; }
    public bool ReferrerPolicy { get; set; }
}

/// <summary>
/// Architecture documentation
/// </summary>
public class ArchitectureDocumentation
{
    public string Overview { get; set; } = string.Empty;
    public List<LayerDocumentation> Layers { get; set; } = new();
    public List<PatternDocumentation> Patterns { get; set; } = new();
    public List<TechnologyDocumentation> Technologies { get; set; } = new();
}

/// <summary>
/// Layer documentation
/// </summary>
public class LayerDocumentation
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Responsibilities { get; set; } = new();
}

/// <summary>
/// Pattern documentation
/// </summary>
public class PatternDocumentation
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Implementation { get; set; } = string.Empty;
    public List<string> Benefits { get; set; } = new();
}

/// <summary>
/// Technology documentation
/// </summary>
public class TechnologyDocumentation
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
}
