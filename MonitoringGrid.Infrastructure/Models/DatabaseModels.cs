namespace MonitoringGrid.Infrastructure.Models;

/// <summary>
/// Database health status information
/// </summary>
public class DatabaseHealthStatus
{
    /// <summary>
    /// Name of the database connection
    /// </summary>
    public string ConnectionName { get; set; } = string.Empty;

    /// <summary>
    /// Whether the database is healthy
    /// </summary>
    public bool IsHealthy { get; set; }

    /// <summary>
    /// Response time in milliseconds
    /// </summary>
    public int ResponseTimeMs { get; set; }

    /// <summary>
    /// Health check timestamp
    /// </summary>
    public DateTime CheckedAt { get; set; }

    /// <summary>
    /// Health status message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Error details if unhealthy
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Database version information
    /// </summary>
    public string? DatabaseVersion { get; set; }

    /// <summary>
    /// Database name
    /// </summary>
    public string? DatabaseName { get; set; }

    /// <summary>
    /// Additional health metrics
    /// </summary>
    public Dictionary<string, object> Metrics { get; set; } = new();
}

/// <summary>
/// Connection pool statistics
/// </summary>
public class ConnectionPoolStatistics
{
    /// <summary>
    /// Name of the connection
    /// </summary>
    public string ConnectionName { get; set; } = string.Empty;

    /// <summary>
    /// Whether the pool is initialized
    /// </summary>
    public bool IsInitialized { get; set; }

    /// <summary>
    /// Total number of connection attempts
    /// </summary>
    public long TotalConnections { get; set; }

    /// <summary>
    /// Number of currently active connections
    /// </summary>
    public int ActiveConnections { get; set; }

    /// <summary>
    /// Number of successful connections
    /// </summary>
    public long SuccessfulConnections { get; set; }

    /// <summary>
    /// Number of failed connections
    /// </summary>
    public long FailedConnections { get; set; }

    /// <summary>
    /// Average connection time in milliseconds
    /// </summary>
    public double AverageConnectionTimeMs { get; set; }

    /// <summary>
    /// Last connection attempt timestamp
    /// </summary>
    public DateTime? LastConnectionAttempt { get; set; }

    /// <summary>
    /// When the pool was created
    /// </summary>
    public DateTime PoolCreatedAt { get; set; }

    /// <summary>
    /// Success rate percentage
    /// </summary>
    public double SuccessRate => TotalConnections > 0 ? (double)SuccessfulConnections / TotalConnections * 100 : 0;

    /// <summary>
    /// Failure rate percentage
    /// </summary>
    public double FailureRate => 100 - SuccessRate;
}

/// <summary>
/// Internal connection pool information
/// </summary>
internal class ConnectionPoolInfo
{
    public string ConnectionName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public long TotalConnections { get; set; }
    public int ActiveConnections { get; set; }
    public long SuccessfulConnections { get; set; }
    public long FailedConnections { get; set; }
    public double AverageConnectionTimeMs { get; set; }
    public DateTime? LastConnectionAttempt { get; set; }
}

/// <summary>
/// Database performance metrics
/// </summary>
public class DatabasePerformanceMetrics
{
    /// <summary>
    /// When the metrics were collected
    /// </summary>
    public DateTime CollectedAt { get; set; }

    /// <summary>
    /// Database connection name
    /// </summary>
    public string ConnectionName { get; set; } = string.Empty;

    /// <summary>
    /// Number of active connections
    /// </summary>
    public int ActiveConnections { get; set; }

    /// <summary>
    /// Total number of queries executed
    /// </summary>
    public long TotalQueries { get; set; }

    /// <summary>
    /// Average query execution time in milliseconds
    /// </summary>
    public double AverageQueryTime { get; set; }

    /// <summary>
    /// Cache hit ratio percentage
    /// </summary>
    public double CacheHitRatio { get; set; }

    /// <summary>
    /// Database size in bytes
    /// </summary>
    public long DatabaseSize { get; set; }

    /// <summary>
    /// Table row counts
    /// </summary>
    public Dictionary<string, long> TableCounts { get; set; } = new();

    /// <summary>
    /// Index fragmentation information
    /// </summary>
    public Dictionary<string, double> IndexFragmentation { get; set; } = new();

    /// <summary>
    /// Wait statistics
    /// </summary>
    public Dictionary<string, long> WaitStatistics { get; set; } = new();

    /// <summary>
    /// Blocking processes count
    /// </summary>
    public int BlockingProcesses { get; set; }

    /// <summary>
    /// Deadlock count
    /// </summary>
    public long DeadlockCount { get; set; }
}

/// <summary>
/// Database optimization result
/// </summary>
public class DatabaseOptimizationResult
{
    /// <summary>
    /// When the optimization was performed
    /// </summary>
    public DateTime OptimizedAt { get; set; }

    /// <summary>
    /// Database connection name
    /// </summary>
    public string ConnectionName { get; set; } = string.Empty;

    /// <summary>
    /// Whether the optimization was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Duration of the optimization process
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Number of indexes rebuilt
    /// </summary>
    public int IndexesRebuilt { get; set; }

    /// <summary>
    /// Number of statistics updated
    /// </summary>
    public int StatisticsUpdated { get; set; }

    /// <summary>
    /// Space reclaimed in bytes
    /// </summary>
    public long SpaceReclaimed { get; set; }

    /// <summary>
    /// Optimization details
    /// </summary>
    public List<string> Details { get; set; } = new();

    /// <summary>
    /// Any errors encountered during optimization
    /// </summary>
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Database query execution result
/// </summary>
/// <typeparam name="T">Result type</typeparam>
public class QueryExecutionResult<T>
{
    /// <summary>
    /// Query results
    /// </summary>
    public IEnumerable<T> Results { get; set; } = new List<T>();

    /// <summary>
    /// Number of rows affected (for commands)
    /// </summary>
    public int RowsAffected { get; set; }

    /// <summary>
    /// Query execution time
    /// </summary>
    public TimeSpan ExecutionTime { get; set; }

    /// <summary>
    /// Whether the query was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if unsuccessful
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Query metadata
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Database connection configuration
/// </summary>
public class DatabaseConnectionConfig
{
    /// <summary>
    /// Connection name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Connection string
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Connection timeout in seconds
    /// </summary>
    public int ConnectionTimeout { get; set; } = 30;

    /// <summary>
    /// Command timeout in seconds
    /// </summary>
    public int CommandTimeout { get; set; } = 30;

    /// <summary>
    /// Maximum retry attempts
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Retry delay in milliseconds
    /// </summary>
    public int RetryDelayMs { get; set; } = 1000;

    /// <summary>
    /// Whether to enable connection pooling
    /// </summary>
    public bool EnablePooling { get; set; } = true;

    /// <summary>
    /// Minimum pool size
    /// </summary>
    public int MinPoolSize { get; set; } = 0;

    /// <summary>
    /// Maximum pool size
    /// </summary>
    public int MaxPoolSize { get; set; } = 100;

    /// <summary>
    /// Whether to enable health checks
    /// </summary>
    public bool EnableHealthChecks { get; set; } = true;

    /// <summary>
    /// Health check interval in seconds
    /// </summary>
    public int HealthCheckIntervalSeconds { get; set; } = 30;
}

/// <summary>
/// Database migration status
/// </summary>
public class DatabaseMigrationStatus
{
    /// <summary>
    /// Database name
    /// </summary>
    public string DatabaseName { get; set; } = string.Empty;

    /// <summary>
    /// Current schema version
    /// </summary>
    public string CurrentVersion { get; set; } = string.Empty;

    /// <summary>
    /// Latest available version
    /// </summary>
    public string LatestVersion { get; set; } = string.Empty;

    /// <summary>
    /// Whether migrations are pending
    /// </summary>
    public bool HasPendingMigrations { get; set; }

    /// <summary>
    /// List of pending migrations
    /// </summary>
    public List<string> PendingMigrations { get; set; } = new();

    /// <summary>
    /// List of applied migrations
    /// </summary>
    public List<string> AppliedMigrations { get; set; } = new();

    /// <summary>
    /// Last migration applied
    /// </summary>
    public DateTime? LastMigrationDate { get; set; }
}
