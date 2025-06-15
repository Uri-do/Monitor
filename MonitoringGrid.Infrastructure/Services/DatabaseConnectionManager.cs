using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MonitoringGrid.Infrastructure.Data;
using MonitoringGrid.Infrastructure.Models;
using System.Collections.Concurrent;
using System.Data;

namespace MonitoringGrid.Infrastructure.Services;

/// <summary>
/// Advanced database connection management service with connection pooling and health monitoring
/// </summary>
public interface IDatabaseConnectionManager
{
    /// <summary>
    /// Gets a database connection for the specified context
    /// </summary>
    Task<IDbConnection> GetConnectionAsync(string connectionName = "Default", CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a raw SQL query with parameters
    /// </summary>
    Task<IEnumerable<T>> ExecuteQueryAsync<T>(string sql, object? parameters = null, string connectionName = "Default", CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a raw SQL command with parameters
    /// </summary>
    Task<int> ExecuteCommandAsync(string sql, object? parameters = null, string connectionName = "Default", CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests database connectivity
    /// </summary>
    Task<DatabaseHealthStatus> TestConnectionAsync(string connectionName = "Default", CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets connection pool statistics
    /// </summary>
    Task<ConnectionPoolStatistics> GetConnectionPoolStatisticsAsync(string connectionName = "Default");

    /// <summary>
    /// Optimizes database performance
    /// </summary>
    Task OptimizeDatabaseAsync(string connectionName = "Default", CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of advanced database connection manager
/// </summary>
public class DatabaseConnectionManager : IDatabaseConnectionManager
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseConnectionManager> _logger;
    private readonly ConcurrentDictionary<string, ConnectionPoolInfo> _connectionPools = new();
    private readonly ConcurrentDictionary<string, DatabaseHealthStatus> _healthCache = new();
    private readonly Timer _healthCheckTimer;

    public DatabaseConnectionManager(
        IConfiguration configuration,
        ILogger<DatabaseConnectionManager> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Initialize health check timer (every 30 seconds)
        _healthCheckTimer = new Timer(async _ => await PerformHealthChecksAsync(), null, 
            TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));

        InitializeConnectionPools();
    }

    public async Task<IDbConnection> GetConnectionAsync(string connectionName = "Default", CancellationToken cancellationToken = default)
    {
        try
        {
            var connectionString = GetConnectionString(connectionName);
            var connection = new SqlConnection(connectionString);
            
            await connection.OpenAsync(cancellationToken);
            
            // Update connection pool statistics
            UpdateConnectionPoolStatistics(connectionName, true);
            
            _logger.LogDebug("Database connection opened for {ConnectionName}", connectionName);
            return connection;
        }
        catch (Exception ex)
        {
            UpdateConnectionPoolStatistics(connectionName, false);
            _logger.LogError(ex, "Failed to open database connection for {ConnectionName}", connectionName);
            throw;
        }
    }

    public async Task<IEnumerable<T>> ExecuteQueryAsync<T>(
        string sql, 
        object? parameters = null, 
        string connectionName = "Default", 
        CancellationToken cancellationToken = default)
    {
        using var connection = await GetConnectionAsync(connectionName, cancellationToken);
        
        try
        {
            var command = connection.CreateCommand();
            command.CommandText = sql;
            command.CommandTimeout = 30; // 30 seconds timeout

            if (parameters != null)
            {
                AddParametersToCommand(command, parameters);
            }

            var results = new List<T>();
            using var reader = await ((SqlCommand)command).ExecuteReaderAsync(cancellationToken);
            
            while (await reader.ReadAsync(cancellationToken))
            {
                var item = MapReaderToObject<T>(reader);
                results.Add(item);
            }

            _logger.LogDebug("Executed query successfully, returned {Count} results", results.Count);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute query: {Sql}", sql);
            throw;
        }
    }

    public async Task<int> ExecuteCommandAsync(
        string sql, 
        object? parameters = null, 
        string connectionName = "Default", 
        CancellationToken cancellationToken = default)
    {
        using var connection = await GetConnectionAsync(connectionName, cancellationToken);
        
        try
        {
            var command = connection.CreateCommand();
            command.CommandText = sql;
            command.CommandTimeout = 30;

            if (parameters != null)
            {
                AddParametersToCommand(command, parameters);
            }

            var result = await ((SqlCommand)command).ExecuteNonQueryAsync(cancellationToken);
            
            _logger.LogDebug("Executed command successfully, affected {Rows} rows", result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute command: {Sql}", sql);
            throw;
        }
    }

    public async Task<DatabaseHealthStatus> TestConnectionAsync(string connectionName = "Default", CancellationToken cancellationToken = default)
    {
        var healthStatus = new DatabaseHealthStatus
        {
            ConnectionName = connectionName,
            CheckedAt = DateTime.UtcNow
        };

        try
        {
            var startTime = DateTime.UtcNow;
            using var connection = await GetConnectionAsync(connectionName, cancellationToken);
            
            // Test basic connectivity
            var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            await ((SqlCommand)command).ExecuteScalarAsync(cancellationToken);
            
            var responseTime = DateTime.UtcNow - startTime;
            
            healthStatus.IsHealthy = true;
            healthStatus.ResponseTimeMs = (int)responseTime.TotalMilliseconds;
            healthStatus.Message = "Connection successful";

            // Get additional database information
            await GetDatabaseInfoAsync(connection, healthStatus, cancellationToken);
        }
        catch (Exception ex)
        {
            healthStatus.IsHealthy = false;
            healthStatus.Message = ex.Message;
            healthStatus.Error = ex.ToString();
            
            _logger.LogError(ex, "Database health check failed for {ConnectionName}", connectionName);
        }

        // Cache the health status
        _healthCache[connectionName] = healthStatus;
        
        return healthStatus;
    }

    public Task<ConnectionPoolStatistics> GetConnectionPoolStatisticsAsync(string connectionName = "Default")
    {
        if (!_connectionPools.TryGetValue(connectionName, out var poolInfo))
        {
            return Task.FromResult(new ConnectionPoolStatistics
            {
                ConnectionName = connectionName,
                IsInitialized = false
            });
        }

        return Task.FromResult(new ConnectionPoolStatistics
        {
            ConnectionName = connectionName,
            IsInitialized = true,
            TotalConnections = poolInfo.TotalConnections,
            ActiveConnections = poolInfo.ActiveConnections,
            SuccessfulConnections = poolInfo.SuccessfulConnections,
            FailedConnections = poolInfo.FailedConnections,
            AverageConnectionTimeMs = poolInfo.AverageConnectionTimeMs,
            LastConnectionAttempt = poolInfo.LastConnectionAttempt,
            PoolCreatedAt = poolInfo.CreatedAt
        });
    }

    public async Task OptimizeDatabaseAsync(string connectionName = "Default", CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting database optimization for {ConnectionName}", connectionName);

            using var connection = await GetConnectionAsync(connectionName, cancellationToken);

            // Update statistics
            await ExecuteCommandAsync("EXEC sp_updatestats", null, connectionName, cancellationToken);
            
            // Rebuild fragmented indexes (simplified version)
            var rebuildIndexesSql = @"
                DECLARE @sql NVARCHAR(MAX) = '';
                SELECT @sql = @sql + 'ALTER INDEX ' + QUOTENAME(i.name) + ' ON ' + QUOTENAME(SCHEMA_NAME(t.schema_id)) + '.' + QUOTENAME(t.name) + ' REBUILD;' + CHAR(13)
                FROM sys.indexes i
                INNER JOIN sys.tables t ON i.object_id = t.object_id
                INNER JOIN sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'LIMITED') ps ON i.object_id = ps.object_id AND i.index_id = ps.index_id
                WHERE ps.avg_fragmentation_in_percent > 30 AND i.index_id > 0;
                EXEC sp_executesql @sql;";

            await ExecuteCommandAsync(rebuildIndexesSql, null, connectionName, cancellationToken);

            _logger.LogInformation("Database optimization completed for {ConnectionName}", connectionName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database optimization failed for {ConnectionName}", connectionName);
            throw;
        }
    }

    private void InitializeConnectionPools()
    {
        var connectionStrings = _configuration.GetSection("ConnectionStrings").GetChildren();
        
        foreach (var connectionString in connectionStrings)
        {
            var poolInfo = new ConnectionPoolInfo
            {
                ConnectionName = connectionString.Key,
                CreatedAt = DateTime.UtcNow
            };
            
            _connectionPools[connectionString.Key] = poolInfo;
            _logger.LogDebug("Initialized connection pool for {ConnectionName}", connectionString.Key);
        }
    }

    private string GetConnectionString(string connectionName)
    {
        var connectionString = _configuration.GetConnectionString(connectionName);
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException($"Connection string '{connectionName}' not found");
        }
        return connectionString;
    }

    private void UpdateConnectionPoolStatistics(string connectionName, bool success)
    {
        if (_connectionPools.TryGetValue(connectionName, out var poolInfo))
        {
            poolInfo.TotalConnections++;
            poolInfo.LastConnectionAttempt = DateTime.UtcNow;
            
            if (success)
            {
                poolInfo.SuccessfulConnections++;
                poolInfo.ActiveConnections++;
            }
            else
            {
                poolInfo.FailedConnections++;
            }
        }
    }

    private void AddParametersToCommand(IDbCommand command, object parameters)
    {
        var properties = parameters.GetType().GetProperties();
        
        foreach (var property in properties)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = $"@{property.Name}";
            parameter.Value = property.GetValue(parameters) ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }
    }

    private T MapReaderToObject<T>(IDataReader reader)
    {
        // Simplified mapping - in a real implementation, you'd use a more sophisticated mapper
        if (typeof(T) == typeof(string))
        {
            return (T)(object)reader.GetString(0);
        }
        
        if (typeof(T) == typeof(int))
        {
            return (T)(object)reader.GetInt32(0);
        }
        
        if (typeof(T) == typeof(long))
        {
            return (T)(object)reader.GetInt64(0);
        }

        // For complex types, you would implement proper mapping logic
        throw new NotImplementedException($"Mapping for type {typeof(T)} is not implemented");
    }

    private async Task GetDatabaseInfoAsync(IDbConnection connection, DatabaseHealthStatus healthStatus, CancellationToken cancellationToken)
    {
        try
        {
            var command = connection.CreateCommand();
            command.CommandText = "SELECT @@VERSION";
            var version = await ((SqlCommand)command).ExecuteScalarAsync(cancellationToken);
            healthStatus.DatabaseVersion = version?.ToString();

            command.CommandText = "SELECT DB_NAME()";
            var databaseName = await ((SqlCommand)command).ExecuteScalarAsync(cancellationToken);
            healthStatus.DatabaseName = databaseName?.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get database information");
        }
    }

    private async Task PerformHealthChecksAsync()
    {
        foreach (var connectionName in _connectionPools.Keys)
        {
            try
            {
                await TestConnectionAsync(connectionName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Health check failed for connection {ConnectionName}", connectionName);
            }
        }
    }

    public void Dispose()
    {
        _healthCheckTimer?.Dispose();
    }
}
