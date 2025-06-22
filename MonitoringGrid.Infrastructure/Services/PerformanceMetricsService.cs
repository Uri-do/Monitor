using Microsoft.Extensions.Logging;
using MonitoringGrid.Core.Interfaces;

namespace MonitoringGrid.Infrastructure.Services;

/// <summary>
/// Service for collecting and storing performance metrics
/// </summary>
public class PerformanceMetricsService : IPerformanceMetricsService
{
    private readonly ILogger<PerformanceMetricsService> _logger;
    private readonly ICacheService _cacheService;
    private readonly Dictionary<string, List<MetricEntry>> _metrics = new();
    private readonly object _lock = new();

    public PerformanceMetricsService(
        ILogger<PerformanceMetricsService> logger,
        ICacheService cacheService)
    {
        _logger = logger;
        _cacheService = cacheService;
    }

    public async Task RecordMetricAsync(string metricName, TimeSpan duration, bool success = true, CancellationToken cancellationToken = default)
    {
        try
        {
            var entry = new MetricEntry
            {
                Timestamp = DateTime.UtcNow,
                Duration = duration,
                Success = success
            };

            lock (_lock)
            {
                if (!_metrics.TryGetValue(metricName, out var entries))
                {
                    entries = new List<MetricEntry>();
                    _metrics[metricName] = entries;
                }

                entries.Add(entry);

                // Keep only last 1000 entries per metric to prevent memory issues
                if (entries.Count > 1000)
                {
                    entries.RemoveRange(0, 100);
                }
            }

            _logger.LogDebug("Recorded metric {MetricName}: {Duration}ms, Success: {Success}", 
                metricName, duration.TotalMilliseconds, success);

            // Cache the latest metric for quick access
            await _cacheService.SetAsync(
                $"performance:latest:{metricName}",
                entry,
                CacheExpirations.Short,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording metric {MetricName}", metricName);
        }
    }

    public async Task<string?> GetMetricAsync(string metricName, CancellationToken cancellationToken = default)
    {
        try
        {
            // Try cache first
            var cached = await _cacheService.GetAsync<MetricEntry>($"performance:latest:{metricName}", cancellationToken);
            if (cached != null)
            {
                return $"Latest: {cached.Duration.TotalMilliseconds}ms at {cached.Timestamp:yyyy-MM-dd HH:mm:ss}";
            }

            lock (_lock)
            {
                if (_metrics.TryGetValue(metricName, out var entries) && entries.Any())
                {
                    var latest = entries.Last();
                    return $"Latest: {latest.Duration.TotalMilliseconds}ms at {latest.Timestamp:yyyy-MM-dd HH:mm:ss}";
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting metric {MetricName}", metricName);
            return null;
        }
    }

    public async Task<IEnumerable<string>> GetMetricsAsync(DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default)
    {
        try
        {
            var results = new List<string>();

            lock (_lock)
            {
                foreach (var kvp in _metrics)
                {
                    var metricName = kvp.Key;
                    var entries = kvp.Value.Where(e => e.Timestamp >= startTime && e.Timestamp <= endTime).ToList();

                    if (entries.Any())
                    {
                        var avgDuration = entries.Average(e => e.Duration.TotalMilliseconds);
                        var successRate = entries.Count(e => e.Success) * 100.0 / entries.Count;
                        
                        results.Add($"{metricName}: {entries.Count} calls, avg {avgDuration:F2}ms, {successRate:F1}% success");
                    }
                }
            }

            await Task.CompletedTask; // For async consistency
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting metrics for time range");
            return new List<string>();
        }
    }

    public async Task<string> GetStatisticsAsync(string metricName, DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default)
    {
        try
        {
            lock (_lock)
            {
                if (!_metrics.TryGetValue(metricName, out var allEntries))
                {
                    return $"No data found for metric: {metricName}";
                }

                var entries = allEntries.Where(e => e.Timestamp >= startTime && e.Timestamp <= endTime).ToList();

                if (!entries.Any())
                {
                    return $"No data found for metric {metricName} in the specified time range";
                }

                var totalCalls = entries.Count;
                var successfulCalls = entries.Count(e => e.Success);
                var failedCalls = totalCalls - successfulCalls;
                var avgDuration = entries.Average(e => e.Duration.TotalMilliseconds);
                var minDuration = entries.Min(e => e.Duration.TotalMilliseconds);
                var maxDuration = entries.Max(e => e.Duration.TotalMilliseconds);
                var successRate = successfulCalls * 100.0 / totalCalls;

                return $"Metric: {metricName}\n" +
                       $"Total Calls: {totalCalls}\n" +
                       $"Successful: {successfulCalls}\n" +
                       $"Failed: {failedCalls}\n" +
                       $"Success Rate: {successRate:F1}%\n" +
                       $"Average Duration: {avgDuration:F2}ms\n" +
                       $"Min Duration: {minDuration:F2}ms\n" +
                       $"Max Duration: {maxDuration:F2}ms";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting statistics for metric {MetricName}", metricName);
            return $"Error retrieving statistics for {metricName}: {ex.Message}";
        }
    }
}

/// <summary>
/// Internal metric entry for tracking individual measurements
/// </summary>
internal class MetricEntry
{
    public DateTime Timestamp { get; set; }
    public TimeSpan Duration { get; set; }
    public bool Success { get; set; }
}
