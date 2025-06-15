namespace MonitoringGrid.Api.Models;

/// <summary>
/// Cache options for intelligent caching
/// </summary>
public class CacheOptions
{
    public TimeSpan? Expiration { get; set; }
    public CachePriority Priority { get; set; } = CachePriority.Normal;
    public bool SlidingExpiration { get; set; } = false;
    public string[]? Tags { get; set; }
    public bool CompressData { get; set; } = false;
    public bool EnableIntelligentExpiration { get; set; } = true;

    public static CacheOptions Default => new()
    {
        Expiration = TimeSpan.FromMinutes(30),
        Priority = CachePriority.Normal,
        EnableIntelligentExpiration = true
    };

    public static CacheOptions ShortTerm => new()
    {
        Expiration = TimeSpan.FromMinutes(5),
        Priority = CachePriority.High,
        EnableIntelligentExpiration = false
    };

    public static CacheOptions LongTerm => new()
    {
        Expiration = TimeSpan.FromHours(4),
        Priority = CachePriority.Low,
        EnableIntelligentExpiration = true
    };

    public static CacheOptions Critical => new()
    {
        Expiration = TimeSpan.FromHours(1),
        Priority = CachePriority.High,
        SlidingExpiration = true,
        EnableIntelligentExpiration = false
    };
}

/// <summary>
/// Cache priority levels
/// </summary>
public enum CachePriority
{
    Low,
    Normal,
    High,
    Critical
}

/// <summary>
/// Cache layer enumeration
/// </summary>
public enum CacheLayer
{
    Memory,
    Distributed,
    External
}

/// <summary>
/// Cache metrics for tracking performance
/// </summary>
public class CacheMetrics
{
    public long Hits { get; set; }
    public long Misses { get; set; }
    public int Size { get; set; }
    public DateTime LastHit { get; set; }
    public DateTime LastMiss { get; set; }
    public DateTime LastUpdate { get; set; }
    public TimeSpan AverageHitTime { get; set; }
    public double HitRate => Hits + Misses > 0 ? (double)Hits / (Hits + Misses) : 0;
}

/// <summary>
/// Cache access pattern tracking
/// </summary>
public class CacheAccessPattern
{
    public long AccessCount { get; set; }
    public DateTime FirstAccess { get; set; }
    public DateTime LastAccess { get; set; }
    public double AccessFrequency { get; set; }
    public List<DateTime> RecentAccesses { get; set; } = new();
    public TimeSpan AverageTimeBetweenAccesses { get; set; }
}

/// <summary>
/// Cache analytics and insights
/// </summary>
public class CacheAnalytics
{
    public DateTime GeneratedAt { get; set; }
    public int TotalKeys { get; set; }
    public long TotalMemoryUsage { get; set; }
    public double HitRate { get; set; }
    public double MissRate { get; set; }
    public List<CacheKeyInfo> TopKeys { get; set; } = new();
    public double CacheEfficiency { get; set; }
    public List<CacheRecommendation> Recommendations { get; set; } = new();
    public Dictionary<string, object> AdditionalMetrics { get; set; } = new();
}

/// <summary>
/// Cache key information
/// </summary>
public class CacheKeyInfo
{
    public string Key { get; set; } = string.Empty;
    public long AccessCount { get; set; }
    public double AccessFrequency { get; set; }
    public DateTime LastAccess { get; set; }
    public int Size { get; set; }
    public double HitRate { get; set; }
    public TimeSpan AverageResponseTime { get; set; }
}

/// <summary>
/// Cache recommendation
/// </summary>
public class CacheRecommendation
{
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string? Action { get; set; }
    public Dictionary<string, object> Details { get; set; } = new();
}

/// <summary>
/// Cache configuration
/// </summary>
public class CacheConfiguration
{
    public MemoryCacheConfig MemoryCache { get; set; } = new();
    public DistributedCacheConfig DistributedCache { get; set; } = new();
    public IntelligentCacheConfig IntelligentCache { get; set; } = new();
}

/// <summary>
/// Memory cache configuration
/// </summary>
public class MemoryCacheConfig
{
    public long SizeLimit { get; set; } = 100 * 1024 * 1024; // 100MB
    public double CompactionPercentage { get; set; } = 0.25;
    public TimeSpan ExpirationScanFrequency { get; set; } = TimeSpan.FromMinutes(1);
}

/// <summary>
/// Distributed cache configuration
/// </summary>
public class DistributedCacheConfig
{
    public string ConnectionString { get; set; } = string.Empty;
    public string InstanceName { get; set; } = "MonitoringGrid";
    public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromHours(1);
    public bool EnableCompression { get; set; } = true;
}

/// <summary>
/// Intelligent cache configuration
/// </summary>
public class IntelligentCacheConfig
{
    public bool EnablePredictiveCaching { get; set; } = true;
    public bool EnableAccessPatternAnalysis { get; set; } = true;
    public TimeSpan OptimizationInterval { get; set; } = TimeSpan.FromMinutes(5);
    public int MaxPreloadKeys { get; set; } = 100;
    public double MinAccessFrequencyForPreload { get; set; } = 5.0;
}

/// <summary>
/// Cache warming strategy
/// </summary>
public class CacheWarmingStrategy
{
    public string Name { get; set; } = string.Empty;
    public CacheWarmingType Type { get; set; }
    public TimeSpan Interval { get; set; }
    public List<string> KeyPatterns { get; set; } = new();
    public int MaxConcurrency { get; set; } = 5;
    public bool EnableScheduledWarming { get; set; } = true;
}

/// <summary>
/// Cache warming types
/// </summary>
public enum CacheWarmingType
{
    Scheduled,
    OnDemand,
    Predictive,
    EventDriven
}

/// <summary>
/// Cache invalidation strategy
/// </summary>
public class CacheInvalidationStrategy
{
    public string Name { get; set; } = string.Empty;
    public CacheInvalidationType Type { get; set; }
    public List<string> TriggerEvents { get; set; } = new();
    public List<string> KeyPatterns { get; set; } = new();
    public bool CascadeInvalidation { get; set; } = false;
}

/// <summary>
/// Cache invalidation types
/// </summary>
public enum CacheInvalidationType
{
    TimeBasedExpiration,
    EventDriven,
    PatternBased,
    DependencyBased,
    Manual
}

/// <summary>
/// Cache performance report
/// </summary>
public class CachePerformanceReport
{
    public DateTime ReportDate { get; set; }
    public TimeSpan ReportPeriod { get; set; }
    public CachePerformanceMetrics Overall { get; set; } = new();
    public Dictionary<string, CachePerformanceMetrics> ByCategory { get; set; } = new();
    public List<CachePerformanceIssue> Issues { get; set; } = new();
    public List<CacheOptimizationSuggestion> Suggestions { get; set; } = new();
}

/// <summary>
/// Cache performance metrics
/// </summary>
public class CachePerformanceMetrics
{
    public long TotalRequests { get; set; }
    public long CacheHits { get; set; }
    public long CacheMisses { get; set; }
    public double HitRatio { get; set; }
    public TimeSpan AverageHitTime { get; set; }
    public TimeSpan AverageMissTime { get; set; }
    public long TotalDataSize { get; set; }
    public double MemoryEfficiency { get; set; }
}

/// <summary>
/// Cache performance issue
/// </summary>
public class CachePerformanceIssue
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public CacheIssueSeverity Severity { get; set; }
    public string AffectedComponent { get; set; } = string.Empty;
    public Dictionary<string, object> Details { get; set; } = new();
}

/// <summary>
/// Cache issue severity
/// </summary>
public enum CacheIssueSeverity
{
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Cache optimization suggestion
/// </summary>
public class CacheOptimizationSuggestion
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public CacheOptimizationType Type { get; set; }
    public double EstimatedImpact { get; set; }
    public string Implementation { get; set; } = string.Empty;
}

/// <summary>
/// Cache optimization types
/// </summary>
public enum CacheOptimizationType
{
    ExpirationTuning,
    SizeOptimization,
    AccessPatternOptimization,
    CompressionImprovement,
    DistributionStrategy
}

/// <summary>
/// Cache health status
/// </summary>
public class CacheHealthStatus
{
    public CacheHealthLevel Level { get; set; }
    public DateTime CheckedAt { get; set; }
    public List<CacheHealthCheck> Checks { get; set; } = new();
    public double OverallScore { get; set; }
    public string Summary { get; set; } = string.Empty;
}

/// <summary>
/// Cache health levels
/// </summary>
public enum CacheHealthLevel
{
    Excellent,
    Good,
    Fair,
    Poor,
    Critical
}

/// <summary>
/// Individual cache health check
/// </summary>
public class CacheHealthCheck
{
    public string Name { get; set; } = string.Empty;
    public CacheHealthLevel Status { get; set; }
    public string Message { get; set; } = string.Empty;
    public double Score { get; set; }
    public Dictionary<string, object> Metrics { get; set; } = new();
}
