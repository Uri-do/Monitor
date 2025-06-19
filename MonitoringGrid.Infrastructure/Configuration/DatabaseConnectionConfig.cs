namespace MonitoringGrid.Infrastructure.Configuration;

/// <summary>
/// Database connection configuration
/// </summary>
public class DatabaseConnectionConfig
{
    public string DefaultConnection { get; set; } = string.Empty;
    public string SourceDatabase { get; set; } = string.Empty;
    public string PopAIConnection { get; set; } = string.Empty;
    public int CommandTimeout { get; set; } = 30;
    public int MaxRetryCount { get; set; } = 3;
    public int MaxRetryDelay { get; set; } = 30;
    public bool EnableRetryOnFailure { get; set; } = true;
    public bool EnableSensitiveDataLogging { get; set; } = false;
}
