using Microsoft.EntityFrameworkCore;
using MonitoringGrid.Infrastructure.Data;
using MonitoringGrid.Api.Middleware;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Diagnostics;

namespace MonitoringGrid.Api.Services;

/// <summary>
/// Database optimization service for query performance monitoring and connection management
/// </summary>
public interface IDatabaseOptimizationService
{
    /// <summary>
    /// Monitors query performance and logs slow queries
    /// </summary>
    Task<T> ExecuteWithMonitoringAsync<T>(Func<Task<T>> operation, string operationName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets database performance metrics
    /// </summary>
    Task<DatabasePerformanceMetrics> GetPerformanceMetricsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Optimizes database connections and pooling
    /// </summary>
    Task OptimizeConnectionPoolAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyzes and suggests query optimizations
    /// </summary>
    Task<List<QueryOptimizationSuggestion>> AnalyzeQueryPerformanceAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes database maintenance tasks
    /// </summary>
    Task ExecuteMaintenanceTasksAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets connection pool statistics
    /// </summary>
    ConnectionPoolStatistics GetConnectionPoolStatistics();
}

/// <summary>
/// Implementation of database optimization service
/// </summary>
public class DatabaseOptimizationService : IDatabaseOptimizationService
{
    private readonly MonitoringContext _context;
    private readonly ILogger<DatabaseOptimizationService> _logger;
    private readonly ICorrelationIdService _correlationIdService;
    private readonly DatabasePerformanceTracker _performanceTracker;
    private readonly ConnectionPoolStatistics _connectionStats;

    // Performance thresholds
    private readonly TimeSpan _slowQueryThreshold = TimeSpan.FromSeconds(2);
    private readonly TimeSpan _verySlowQueryThreshold = TimeSpan.FromSeconds(5);

    public DatabaseOptimizationService(
        MonitoringContext context,
        ILogger<DatabaseOptimizationService> logger,
        ICorrelationIdService correlationIdService)
    {
        _context = context;
        _logger = logger;
        _correlationIdService = correlationIdService;
        _performanceTracker = new DatabasePerformanceTracker();
        _connectionStats = new ConnectionPoolStatistics();
    }

    /// <summary>
    /// Executes database operations with performance monitoring
    /// </summary>
    public async Task<T> ExecuteWithMonitoringAsync<T>(Func<Task<T>> operation, string operationName, CancellationToken cancellationToken = default)
    {
        var correlationId = _correlationIdService.GetCorrelationId();
        var stopwatch = Stopwatch.StartNew();
        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogDebug("Starting database operation: {OperationName} [{CorrelationId}]", operationName, correlationId);

            var result = await operation();

            stopwatch.Stop();
            var duration = stopwatch.Elapsed;

            // Record performance metrics
            _performanceTracker.RecordOperation(operationName, duration, true);

            // Log slow queries
            if (duration > _verySlowQueryThreshold)
            {
                _logger.LogWarning("Very slow database operation: {OperationName} took {Duration}ms [{CorrelationId}]",
                    operationName, duration.TotalMilliseconds, correlationId);
            }
            else if (duration > _slowQueryThreshold)
            {
                _logger.LogInformation("Slow database operation: {OperationName} took {Duration}ms [{CorrelationId}]",
                    operationName, duration.TotalMilliseconds, correlationId);
            }
            else
            {
                _logger.LogDebug("Database operation completed: {OperationName} took {Duration}ms [{CorrelationId}]",
                    operationName, duration.TotalMilliseconds, correlationId);
            }

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _performanceTracker.RecordOperation(operationName, stopwatch.Elapsed, false);

            _logger.LogError(ex, "Database operation failed: {OperationName} after {Duration}ms [{CorrelationId}]",
                operationName, stopwatch.ElapsedMilliseconds, correlationId);

            throw;
        }
    }

    /// <summary>
    /// Gets comprehensive database performance metrics
    /// </summary>
    public async Task<DatabasePerformanceMetrics> GetPerformanceMetricsAsync(CancellationToken cancellationToken = default)
    {
        var correlationId = _correlationIdService.GetCorrelationId();

        try
        {
            var metrics = new DatabasePerformanceMetrics
            {
                Timestamp = DateTime.UtcNow,
                CorrelationId = correlationId
            };

            // Get basic performance stats
            metrics.OperationStats = _performanceTracker.GetStatistics();
            metrics.ConnectionStats = GetConnectionPoolStatistics();

            // Get database-specific metrics
            await PopulateDatabaseSpecificMetricsAsync(metrics, cancellationToken);

            return metrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get database performance metrics [{CorrelationId}]", correlationId);
            throw;
        }
    }

    /// <summary>
    /// Optimizes database connection pooling
    /// </summary>
    public async Task OptimizeConnectionPoolAsync(CancellationToken cancellationToken = default)
    {
        var correlationId = _correlationIdService.GetCorrelationId();

        try
        {
            _logger.LogInformation("Starting connection pool optimization [{CorrelationId}]", correlationId);

            // Clear connection pool to remove stale connections
            await using var connection = _context.Database.GetDbConnection();
            if (connection is SqlConnection sqlConnection)
            {
                SqlConnection.ClearPool(sqlConnection);
                _connectionStats.RecordPoolClear();
            }

            // Test connection health
            await _context.Database.OpenConnectionAsync(cancellationToken);
            await _context.Database.CloseConnectionAsync();

            _logger.LogInformation("Connection pool optimization completed [{CorrelationId}]", correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connection pool optimization failed [{CorrelationId}]", correlationId);
            throw;
        }
    }

    /// <summary>
    /// Analyzes query performance and provides optimization suggestions
    /// </summary>
    public async Task<List<QueryOptimizationSuggestion>> AnalyzeQueryPerformanceAsync(CancellationToken cancellationToken = default)
    {
        var correlationId = _correlationIdService.GetCorrelationId();
        var suggestions = new List<QueryOptimizationSuggestion>();

        try
        {
            _logger.LogInformation("Starting query performance analysis [{CorrelationId}]", correlationId);

            // Analyze slow operations
            var slowOperations = _performanceTracker.GetSlowOperations(_slowQueryThreshold);
            
            foreach (var operation in slowOperations)
            {
                suggestions.Add(new QueryOptimizationSuggestion
                {
                    OperationName = operation.Name,
                    AverageExecutionTime = operation.AverageExecutionTime,
                    ExecutionCount = operation.ExecutionCount,
                    Suggestion = GenerateOptimizationSuggestion(operation),
                    Priority = operation.AverageExecutionTime > _verySlowQueryThreshold ? OptimizationPriority.High : OptimizationPriority.Medium
                });
            }

            // Check for missing indexes (simplified analysis)
            var indexSuggestions = await AnalyzeMissingIndexesAsync(cancellationToken);
            suggestions.AddRange(indexSuggestions);

            _logger.LogInformation("Query performance analysis completed. Found {SuggestionCount} optimization opportunities [{CorrelationId}]",
                suggestions.Count, correlationId);

            return suggestions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Query performance analysis failed [{CorrelationId}]", correlationId);
            throw;
        }
    }

    /// <summary>
    /// Executes routine database maintenance tasks
    /// </summary>
    public async Task ExecuteMaintenanceTasksAsync(CancellationToken cancellationToken = default)
    {
        var correlationId = _correlationIdService.GetCorrelationId();

        try
        {
            _logger.LogInformation("Starting database maintenance tasks [{CorrelationId}]", correlationId);

            var maintenanceTasks = new List<Task>
            {
                UpdateStatisticsAsync(cancellationToken),
                RebuildFragmentedIndexesAsync(cancellationToken),
                CleanupOldDataAsync(cancellationToken)
            };

            await Task.WhenAll(maintenanceTasks);

            _logger.LogInformation("Database maintenance tasks completed [{CorrelationId}]", correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database maintenance tasks failed [{CorrelationId}]", correlationId);
            throw;
        }
    }

    /// <summary>
    /// Gets current connection pool statistics
    /// </summary>
    public ConnectionPoolStatistics GetConnectionPoolStatistics()
    {
        return _connectionStats.Clone();
    }

    /// <summary>
    /// Populates database-specific performance metrics
    /// </summary>
    private async Task PopulateDatabaseSpecificMetricsAsync(DatabasePerformanceMetrics metrics, CancellationToken cancellationToken)
    {
        try
        {
            // Get database size information
            var sizeQuery = @"
                SELECT 
                    SUM(CAST(FILEPROPERTY(name, 'SpaceUsed') AS bigint) * 8192.) / 1024 / 1024 as UsedSpaceMB,
                    SUM(size * 8192.) / 1024 / 1024 as AllocatedSpaceMB
                FROM sys.database_files 
                WHERE type = 0";

            await using var command = _context.Database.GetDbConnection().CreateCommand();
            command.CommandText = sizeQuery;
            
            if (_context.Database.GetDbConnection().State != ConnectionState.Open)
                await _context.Database.OpenConnectionAsync(cancellationToken);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                metrics.DatabaseSizeMB = reader.GetDouble("AllocatedSpaceMB");
                metrics.UsedSpaceMB = reader.GetDouble("UsedSpaceMB");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get database size metrics");
        }
    }

    /// <summary>
    /// Generates optimization suggestions based on operation performance
    /// </summary>
    private string GenerateOptimizationSuggestion(OperationPerformance operation)
    {
        if (operation.Name.Contains("GetKpis", StringComparison.OrdinalIgnoreCase))
        {
            return "Consider adding indexes on frequently queried KPI columns (Indicator, IsActive, Priority). Implement pagination for large result sets.";
        }
        
        if (operation.Name.Contains("Execute", StringComparison.OrdinalIgnoreCase))
        {
            return "Review KPI SQL queries for optimization opportunities. Consider query result caching for frequently executed KPIs.";
        }

        if (operation.Name.Contains("Alert", StringComparison.OrdinalIgnoreCase))
        {
            return "Consider archiving old alert data and adding indexes on timestamp columns for better performance.";
        }

        return "Review query execution plan and consider adding appropriate indexes or optimizing query structure.";
    }

    /// <summary>
    /// Analyzes missing indexes and provides suggestions
    /// </summary>
    private async Task<List<QueryOptimizationSuggestion>> AnalyzeMissingIndexesAsync(CancellationToken cancellationToken)
    {
        var suggestions = new List<QueryOptimizationSuggestion>();

        try
        {
            // This is a simplified analysis - in production, you'd use more sophisticated index analysis
            var commonIndexSuggestions = new[]
            {
                new QueryOptimizationSuggestion
                {
                    OperationName = "KPI Queries",
                    Suggestion = "Consider adding composite index on (IsActive, Priority, ModifiedDate) for KPI table",
                    Priority = OptimizationPriority.Medium
                },
                new QueryOptimizationSuggestion
                {
                    OperationName = "Alert Queries",
                    Suggestion = "Consider adding index on (KpiId, CreatedDate) for AlertLog table",
                    Priority = OptimizationPriority.Low
                }
            };

            suggestions.AddRange(commonIndexSuggestions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to analyze missing indexes");
        }

        return suggestions;
    }

    /// <summary>
    /// Updates database statistics for better query optimization
    /// </summary>
    private async Task UpdateStatisticsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var updateStatsQuery = "EXEC sp_updatestats";
            await _context.Database.ExecuteSqlRawAsync(updateStatsQuery, cancellationToken);
            _logger.LogDebug("Database statistics updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update database statistics");
        }
    }

    /// <summary>
    /// Rebuilds fragmented indexes
    /// </summary>
    private async Task RebuildFragmentedIndexesAsync(CancellationToken cancellationToken)
    {
        try
        {
            // This is a simplified approach - in production, you'd analyze fragmentation levels first
            var rebuildQuery = @"
                DECLARE @sql NVARCHAR(MAX) = ''
                SELECT @sql = @sql + 'ALTER INDEX ' + i.name + ' ON ' + SCHEMA_NAME(t.schema_id) + '.' + t.name + ' REBUILD;' + CHAR(13)
                FROM sys.indexes i
                INNER JOIN sys.tables t ON i.object_id = t.object_id
                WHERE i.type > 0 AND i.is_disabled = 0
                
                EXEC sp_executesql @sql";

            // Only run during maintenance windows
            if (DateTime.UtcNow.Hour >= 2 && DateTime.UtcNow.Hour <= 4)
            {
                await _context.Database.ExecuteSqlRawAsync(rebuildQuery, cancellationToken);
                _logger.LogDebug("Index rebuild completed successfully");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to rebuild indexes");
        }
    }

    /// <summary>
    /// Cleans up old data to maintain performance
    /// </summary>
    private async Task CleanupOldDataAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Archive old alert logs (older than 90 days)
            var cutoffDate = DateTime.UtcNow.AddDays(-90);
            var deletedCount = await _context.Database.ExecuteSqlRawAsync(
                "DELETE FROM AlertLogs WHERE CreatedDate < {0}", cutoffDate);

            if (deletedCount > 0)
            {
                _logger.LogInformation("Cleaned up {DeletedCount} old alert log records", deletedCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cleanup old data");
        }
    }
}

/// <summary>
/// Database performance metrics container
/// </summary>
public class DatabasePerformanceMetrics
{
    public DateTime Timestamp { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public double DatabaseSizeMB { get; set; }
    public double UsedSpaceMB { get; set; }
    public Dictionary<string, OperationPerformance> OperationStats { get; set; } = new();
    public ConnectionPoolStatistics ConnectionStats { get; set; } = new();
}

/// <summary>
/// Query optimization suggestion
/// </summary>
public class QueryOptimizationSuggestion
{
    public string OperationName { get; set; } = string.Empty;
    public string Suggestion { get; set; } = string.Empty;
    public OptimizationPriority Priority { get; set; }
    public TimeSpan? AverageExecutionTime { get; set; }
    public int ExecutionCount { get; set; }
}

/// <summary>
/// Optimization priority levels
/// </summary>
public enum OptimizationPriority
{
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Connection pool statistics
/// </summary>
public class ConnectionPoolStatistics
{
    private long _poolClears;
    private long _connectionFailures;
    private DateTime _lastPoolClear = DateTime.MinValue;

    public long PoolClears => _poolClears;
    public long ConnectionFailures => _connectionFailures;
    public DateTime LastPoolClear => _lastPoolClear;

    public void RecordPoolClear()
    {
        Interlocked.Increment(ref _poolClears);
        _lastPoolClear = DateTime.UtcNow;
    }

    public void RecordConnectionFailure()
    {
        Interlocked.Increment(ref _connectionFailures);
    }

    public ConnectionPoolStatistics Clone()
    {
        return new ConnectionPoolStatistics
        {
            _poolClears = _poolClears,
            _connectionFailures = _connectionFailures,
            _lastPoolClear = _lastPoolClear
        };
    }
}

/// <summary>
/// Database performance tracker
/// </summary>
public class DatabasePerformanceTracker
{
    private readonly Dictionary<string, OperationPerformance> _operations = new();
    private readonly object _lock = new();

    public void RecordOperation(string operationName, TimeSpan duration, bool success)
    {
        lock (_lock)
        {
            if (!_operations.TryGetValue(operationName, out var operation))
            {
                operation = new OperationPerformance { Name = operationName };
                _operations[operationName] = operation;
            }

            operation.RecordExecution(duration, success);
        }
    }

    public Dictionary<string, OperationPerformance> GetStatistics()
    {
        lock (_lock)
        {
            return _operations.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Clone());
        }
    }

    public List<OperationPerformance> GetSlowOperations(TimeSpan threshold)
    {
        lock (_lock)
        {
            return _operations.Values
                .Where(op => op.AverageExecutionTime > threshold)
                .OrderByDescending(op => op.AverageExecutionTime)
                .Select(op => op.Clone())
                .ToList();
        }
    }
}

/// <summary>
/// Individual operation performance tracking
/// </summary>
public class OperationPerformance
{
    public string Name { get; set; } = string.Empty;
    public int ExecutionCount { get; private set; }
    public int SuccessCount { get; private set; }
    public TimeSpan TotalExecutionTime { get; private set; }
    public TimeSpan MinExecutionTime { get; private set; } = TimeSpan.MaxValue;
    public TimeSpan MaxExecutionTime { get; private set; }
    public TimeSpan AverageExecutionTime => ExecutionCount > 0 ? TimeSpan.FromTicks(TotalExecutionTime.Ticks / ExecutionCount) : TimeSpan.Zero;
    public double SuccessRate => ExecutionCount > 0 ? (double)SuccessCount / ExecutionCount : 0;

    public void RecordExecution(TimeSpan duration, bool success)
    {
        ExecutionCount++;
        TotalExecutionTime = TotalExecutionTime.Add(duration);
        
        if (duration < MinExecutionTime)
            MinExecutionTime = duration;
        
        if (duration > MaxExecutionTime)
            MaxExecutionTime = duration;

        if (success)
            SuccessCount++;
    }

    public OperationPerformance Clone()
    {
        return new OperationPerformance
        {
            Name = Name,
            ExecutionCount = ExecutionCount,
            SuccessCount = SuccessCount,
            TotalExecutionTime = TotalExecutionTime,
            MinExecutionTime = MinExecutionTime,
            MaxExecutionTime = MaxExecutionTime
        };
    }
}
