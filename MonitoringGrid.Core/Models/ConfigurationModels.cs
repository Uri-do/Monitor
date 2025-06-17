namespace MonitoringGrid.Core.Models;

/// <summary>
/// Configuration validation result
/// </summary>
public class ConfigurationValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }

    public static ConfigurationValidationResult Success() => new() { IsValid = true };
    public static ConfigurationValidationResult Error(string message) => new() { IsValid = false, ErrorMessage = message };
}

/// <summary>
/// Configuration health status
/// </summary>
public class ConfigurationHealthStatus
{
    public bool IsHealthy { get; set; }
    public int TotalConfigurations { get; set; }
    public int CachedConfigurations { get; set; }
    public int WatchedConfigurations { get; set; }
    public double CacheHitRate { get; set; }
    public DateTime LastRefresh { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Configuration cache options
/// </summary>
public class ConfigurationCacheOptions
{
    public int DefaultExpirationMinutes { get; set; } = 30;
    public int SlidingExpirationMinutes { get; set; } = 10;
    public int RefreshIntervalMinutes { get; set; } = 5;
}

/// <summary>
/// Configuration validation rules
/// </summary>
public class ConfigurationValidationRules
{
    public string? DataType { get; set; }
    public object? MinValue { get; set; }
    public object? MaxValue { get; set; }
    public string[]? AllowedValues { get; set; }
    public string? RegexPattern { get; set; }
}
