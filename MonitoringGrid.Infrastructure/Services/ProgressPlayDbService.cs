using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Interfaces;
using System.Data;

namespace MonitoringGrid.Infrastructure.Services;

/// <summary>
/// Service implementation for ProgressPlayDB operations
/// </summary>
public class ProgressPlayDbService : IProgressPlayDbService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ProgressPlayDbService> _logger;
    private readonly IConfigurationService _configService;

    public ProgressPlayDbService(
        IConfiguration configuration,
        ILogger<ProgressPlayDbService> logger,
        IConfigurationService configService)
    {
        _configuration = configuration;
        _logger = logger;
        _configService = configService;
    }

    public async Task<List<CollectorDto>> GetCollectorsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving collectors from ProgressPlayDB");

        var connectionString = await GetConnectionStringAsync();
        var collectors = new List<CollectorDto>();

        const string sql = @"
            SELECT CollectorID, CollectorCode, CollectorDesc, IsActive
            FROM [stats].[tbl_Monitor_StatisticsCollectors]
            WHERE IsActive = 1
            ORDER BY CollectorCode";

        try
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            using var command = new SqlCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                var collector = new CollectorDto
                {
                    CollectorId = reader.GetInt32("CollectorID"),
                    CollectorCode = reader.GetString("CollectorCode"),
                    CollectorDesc = reader.GetString("CollectorDesc"),
                    IsActive = reader.GetBoolean("IsActive")
                };

                collectors.Add(collector);
            }

            // Get available items for each collector
            foreach (var collector in collectors)
            {
                collector.AvailableItems = await GetCollectorItemNamesAsync(collector.CollectorId, cancellationToken);
            }

            _logger.LogDebug("Retrieved {Count} collectors", collectors.Count);
            return collectors;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve collectors from ProgressPlayDB");
            throw;
        }
    }

    public async Task<CollectorDto?> GetCollectorByIdAsync(int collectorId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving collector {CollectorId} from ProgressPlayDB", collectorId);

        var connectionString = await GetConnectionStringAsync();

        const string sql = @"
            SELECT CollectorID, CollectorCode, CollectorDesc, IsActive
            FROM [stats].[tbl_Monitor_StatisticsCollectors]
            WHERE CollectorID = @CollectorId";

        try
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@CollectorId", collectorId);

            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (await reader.ReadAsync(cancellationToken))
            {
                var collector = new CollectorDto
                {
                    CollectorId = reader.GetInt32("CollectorID"),
                    CollectorCode = reader.GetString("CollectorCode"),
                    CollectorDesc = reader.GetString("CollectorDesc"),
                    IsActive = reader.GetBoolean("IsActive")
                };

                collector.AvailableItems = await GetCollectorItemNamesAsync(collector.CollectorId, cancellationToken);
                return collector;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve collector {CollectorId} from ProgressPlayDB", collectorId);
            throw;
        }
    }

    public async Task<List<string>> GetCollectorItemNamesAsync(int collectorId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving item names for collector {CollectorId}", collectorId);

        var connectionString = await GetConnectionStringAsync();
        var itemNames = new List<string>();

        const string sql = @"
            SELECT DISTINCT ItemName
            FROM [stats].[tbl_Monitor_Statistics]
            WHERE CollectorID = @CollectorId
            ORDER BY ItemName";

        try
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@CollectorId", collectorId);

            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                itemNames.Add(reader.GetString("ItemName"));
            }

            _logger.LogDebug("Retrieved {Count} item names for collector {CollectorId}", itemNames.Count, collectorId);
            return itemNames;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve item names for collector {CollectorId}", collectorId);
            throw;
        }
    }

    public async Task<List<CollectorStatisticDto>> GetCollectorStatisticsAsync(int collectorId, string itemName,
        int lastDays = 30, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving statistics for collector {CollectorId}, item {ItemName}, last {Days} days",
            collectorId, itemName, lastDays);

        var connectionString = await GetConnectionStringAsync();
        var statistics = new List<CollectorStatisticDto>();

        const string sql = @"
            SELECT ItemName, Total, Marked, MarkedPercent, Day, Hour
            FROM [stats].[tbl_Monitor_Statistics]
            WHERE CollectorID = @CollectorId 
                AND ItemName = @ItemName 
                AND Day >= @FromDate
            ORDER BY Day DESC, Hour DESC";

        try
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@CollectorId", collectorId);
            command.Parameters.AddWithValue("@ItemName", itemName);
            command.Parameters.AddWithValue("@FromDate", DateTime.UtcNow.Date.AddDays(-lastDays));

            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                var statistic = new CollectorStatisticDto
                {
                    ItemName = reader.GetString("ItemName"),
                    Total = reader.GetInt32("Total"),
                    Marked = reader.GetInt32("Marked"),
                    MarkedPercent = reader.GetDecimal("MarkedPercent"),
                    Day = reader.GetDateTime("Day"),
                    Hour = reader.GetInt32("Hour")
                };

                statistics.Add(statistic);
            }

            _logger.LogDebug("Retrieved {Count} statistics records", statistics.Count);
            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve statistics for collector {CollectorId}, item {ItemName}",
                collectorId, itemName);
            throw;
        }
    }

    public async Task<List<CollectorStatisticDto>> ExecuteCollectorStoredProcedureAsync(int collectorId, int lastMinutes,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Executing stored procedure for collector {CollectorId} with {LastMinutes} minutes",
            collectorId, lastMinutes);

        // First get the collector to find its stored procedure
        var collector = await GetCollectorByIdAsync(collectorId, cancellationToken);
        if (collector == null)
        {
            throw new ArgumentException($"Collector {collectorId} not found");
        }

        // Get the stored procedure name from the collector
        var connectionString = await GetConnectionStringAsync();
        string? storedProcedure = null;

        const string getSpSql = @"
            SELECT StoreProcedure
            FROM [stats].[tbl_Monitor_StatisticsCollectors]
            WHERE CollectorID = @CollectorId";

        using (var connection = new SqlConnection(connectionString))
        {
            await connection.OpenAsync(cancellationToken);
            using var command = new SqlCommand(getSpSql, connection);
            command.Parameters.AddWithValue("@CollectorId", collectorId);
            storedProcedure = (string?)await command.ExecuteScalarAsync(cancellationToken);
        }

        if (string.IsNullOrEmpty(storedProcedure))
        {
            throw new InvalidOperationException($"No stored procedure found for collector {collectorId}");
        }

        // Execute the stored procedure
        var results = new List<CollectorStatisticDto>();

        try
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            using var command = new SqlCommand(storedProcedure, connection);
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@LastMinutes", lastMinutes);

            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                var result = new CollectorStatisticDto
                {
                    ItemName = reader.GetString("ItemName"),
                    Total = reader.GetInt32("Total"),
                    Marked = reader.GetInt32("Marked"),
                    MarkedPercent = reader.FieldCount > 3 && !reader.IsDBNull("MarkedPercent") 
                        ? reader.GetDecimal("MarkedPercent") 
                        : (reader.GetInt32("Total") > 0 ? (decimal)reader.GetInt32("Marked") / reader.GetInt32("Total") * 100 : 0),
                    Day = DateTime.UtcNow.Date,
                    Hour = DateTime.UtcNow.Hour
                };

                results.Add(result);
            }

            _logger.LogDebug("Executed stored procedure {StoredProcedure}, got {Count} results",
                storedProcedure, results.Count);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute stored procedure {StoredProcedure} for collector {CollectorId}",
                storedProcedure, collectorId);
            throw;
        }
    }

    public async Task<decimal?> GetCollectorItemAverageAsync(int collectorId, string itemName, string valueType,
        int? hour = null, DateTime? fromDate = null, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting average for collector {CollectorId}, item {ItemName}, type {ValueType}",
            collectorId, itemName, valueType);

        var connectionString = await GetConnectionStringAsync();

        try
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            using var command = new SqlCommand("[stats].[stp_MonitorService_GetStatisticsAverage]", connection);
            command.CommandType = CommandType.StoredProcedure;
            
            command.Parameters.AddWithValue("@Hour", hour.HasValue ? hour.Value : DBNull.Value);
            command.Parameters.AddWithValue("@FromDate", fromDate.HasValue ? fromDate.Value : DBNull.Value);
            command.Parameters.AddWithValue("@CollectorID", collectorId);
            command.Parameters.AddWithValue("@ItemName", itemName);
            command.Parameters.AddWithValue("@ValueType", valueType);

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return result as decimal?;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get average for collector {CollectorId}, item {ItemName}",
                collectorId, itemName);
            throw;
        }
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var connectionString = await GetConnectionStringAsync();
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ProgressPlayDB connection test failed");
            return false;
        }
    }

    private async Task<string> GetConnectionStringAsync()
    {
        // Try to get from configuration service first (encrypted from PopAI.monitoring.Config)
        var configConnectionString = await _configService.GetConfigValueAsync("ProgressPlayDbConnectionString");
        if (!string.IsNullOrEmpty(configConnectionString))
        {
            _logger.LogDebug("Using ProgressPlayDB connection string from configuration service");
            return configConnectionString;
        }

        // Fallback to appsettings
        var fallbackConnectionString = _configuration.GetConnectionString("ProgressPlayDB");
        if (!string.IsNullOrEmpty(fallbackConnectionString))
        {
            _logger.LogDebug("Using ProgressPlayDB connection string from appsettings");
            return fallbackConnectionString;
        }

        throw new InvalidOperationException("ProgressPlayDB connection string not found in configuration service or appsettings");
    }
}
