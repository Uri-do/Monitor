using Microsoft.Extensions.Logging;
using MonitoringGrid.Core.Interfaces;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace MonitoringGrid.Infrastructure.Services;

/// <summary>
/// Implementation of performance metrics collector
/// </summary>
public class PerformanceMetricsCollector : IPerformanceMetricsCollector
{
    private readonly ILogger<PerformanceMetricsCollector> _logger;
    private readonly ConcurrentDictionary<string, EndpointMetrics> _endpointMetrics = new();
    private readonly ConcurrentDictionary<string, OperationMetrics> _databaseMetrics = new();
    private readonly ConcurrentDictionary<string, CacheOperationMetrics> _cacheMetrics = new();
    private readonly ConcurrentDictionary<string, double> _customMetrics = new();
    
    private long _totalRequests = 0;
    private long _totalDatabaseOperations = 0;
    private long _totalCacheOperations = 0;
    private long _totalResponseTime = 0;
    private long _totalDatabaseTime = 0;
    private long _totalCacheTime = 0;
    private long _errorCount = 0;
    private long _databaseErrorCount = 0;
    private long _cacheHitCount = 0;
    
    private readonly DateTime _startTime = DateTime.UtcNow;

    public PerformanceMetricsCollector(ILogger<PerformanceMetricsCollector> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void RecordRequest(string endpoint, string method, int statusCode, long durationMs, long requestSize = 0, long responseSize = 0)
    {
        try
        {
            Interlocked.Increment(ref _totalRequests);
            Interlocked.Add(ref _totalResponseTime, durationMs);

            if (statusCode >= 400)
            {
                Interlocked.Increment(ref _errorCount);
            }

            var key = $"{method} {endpoint}";
            _endpointMetrics.AddOrUpdate(key,
                new EndpointMetrics
                {
                    TotalRequests = 1,
                    AverageResponseTime = durationMs,
                    ErrorCount = statusCode >= 400 ? 1 : 0,
                    LastRequestTime = DateTime.UtcNow
                },
                (k, existing) =>
                {
                    var newTotal = existing.TotalRequests + 1;
                    var newAverage = (existing.AverageResponseTime * existing.TotalRequests + durationMs) / newTotal;
                    
                    return new EndpointMetrics
                    {
                        TotalRequests = newTotal,
                        AverageResponseTime = newAverage,
                        ErrorCount = existing.ErrorCount + (statusCode >= 400 ? 1 : 0),
                        LastRequestTime = DateTime.UtcNow
                    };
                });

            _logger.LogDebug("Recorded request metric: {Endpoint} {Method} {StatusCode} {Duration}ms", 
                endpoint, method, statusCode, durationMs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording request metric");
        }
    }

    public void RecordDatabaseOperation(string operation, long durationMs, bool success = true)
    {
        try
        {
            Interlocked.Increment(ref _totalDatabaseOperations);
            Interlocked.Add(ref _totalDatabaseTime, durationMs);

            if (!success)
            {
                Interlocked.Increment(ref _databaseErrorCount);
            }

            _databaseMetrics.AddOrUpdate(operation,
                new OperationMetrics
                {
                    TotalOperations = 1,
                    AverageOperationTime = durationMs,
                    SuccessCount = success ? 1 : 0,
                    LastOperationTime = DateTime.UtcNow
                },
                (k, existing) =>
                {
                    var newTotal = existing.TotalOperations + 1;
                    var newAverage = (existing.AverageOperationTime * existing.TotalOperations + durationMs) / newTotal;
                    
                    return new OperationMetrics
                    {
                        TotalOperations = newTotal,
                        AverageOperationTime = newAverage,
                        SuccessCount = existing.SuccessCount + (success ? 1 : 0),
                        LastOperationTime = DateTime.UtcNow
                    };
                });

            _logger.LogDebug("Recorded database operation metric: {Operation} {Duration}ms {Success}", 
                operation, durationMs, success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording database operation metric");
        }
    }

    public void RecordCacheOperation(string operation, long durationMs, bool hit = true)
    {
        try
        {
            Interlocked.Increment(ref _totalCacheOperations);
            Interlocked.Add(ref _totalCacheTime, durationMs);

            if (hit)
            {
                Interlocked.Increment(ref _cacheHitCount);
            }

            _cacheMetrics.AddOrUpdate(operation,
                new CacheOperationMetrics
                {
                    TotalOperations = 1,
                    HitCount = hit ? 1 : 0,
                    AverageOperationTime = durationMs,
                    LastOperationTime = DateTime.UtcNow
                },
                (k, existing) =>
                {
                    var newTotal = existing.TotalOperations + 1;
                    var newAverage = (existing.AverageOperationTime * existing.TotalOperations + durationMs) / newTotal;
                    
                    return new CacheOperationMetrics
                    {
                        TotalOperations = newTotal,
                        HitCount = existing.HitCount + (hit ? 1 : 0),
                        AverageOperationTime = newAverage,
                        LastOperationTime = DateTime.UtcNow
                    };
                });

            _logger.LogDebug("Recorded cache operation metric: {Operation} {Duration}ms {Hit}", 
                operation, durationMs, hit);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording cache operation metric");
        }
    }

    public void RecordCustomMetric(string name, double value, Dictionary<string, string>? tags = null)
    {
        try
        {
            _customMetrics.AddOrUpdate(name, value, (k, existing) => value);
            
            _logger.LogDebug("Recorded custom metric: {Name} = {Value}", name, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording custom metric");
        }
    }

    public async Task<PerformanceMetrics> GetMetricsAsync()
    {
        try
        {
            var uptime = DateTime.UtcNow - _startTime;
            var uptimeSeconds = uptime.TotalSeconds;

            var metrics = new PerformanceMetrics
            {
                CollectedAt = DateTime.UtcNow,
                Requests = new RequestMetrics
                {
                    TotalRequests = _totalRequests,
                    AverageResponseTime = _totalRequests > 0 ? (double)_totalResponseTime / _totalRequests : 0,
                    RequestsPerSecond = uptimeSeconds > 0 ? _totalRequests / uptimeSeconds : 0,
                    ErrorRate = _totalRequests > 0 ? (double)_errorCount / _totalRequests * 100 : 0,
                    ByEndpoint = new Dictionary<string, EndpointMetrics>(_endpointMetrics)
                },
                Database = new DatabaseMetrics
                {
                    TotalOperations = _totalDatabaseOperations,
                    AverageOperationTime = _totalDatabaseOperations > 0 ? (double)_totalDatabaseTime / _totalDatabaseOperations : 0,
                    SuccessRate = _totalDatabaseOperations > 0 ? (double)(_totalDatabaseOperations - _databaseErrorCount) / _totalDatabaseOperations * 100 : 0,
                    OperationsPerSecond = uptimeSeconds > 0 ? _totalDatabaseOperations / uptimeSeconds : 0,
                    ByOperation = new Dictionary<string, OperationMetrics>(_databaseMetrics)
                },
                Cache = new CacheMetrics
                {
                    TotalOperations = _totalCacheOperations,
                    HitRate = _totalCacheOperations > 0 ? (double)_cacheHitCount / _totalCacheOperations * 100 : 0,
                    AverageOperationTime = _totalCacheOperations > 0 ? (double)_totalCacheTime / _totalCacheOperations : 0,
                    OperationsPerSecond = uptimeSeconds > 0 ? _totalCacheOperations / uptimeSeconds : 0,
                    ByType = new Dictionary<string, CacheOperationMetrics>(_cacheMetrics)
                },
                System = await GetSystemMetricsAsync(),
                CustomMetrics = new Dictionary<string, double>(_customMetrics)
            };

            return metrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting performance metrics");
            return new PerformanceMetrics();
        }
    }

    public void ResetMetrics()
    {
        try
        {
            Interlocked.Exchange(ref _totalRequests, 0);
            Interlocked.Exchange(ref _totalDatabaseOperations, 0);
            Interlocked.Exchange(ref _totalCacheOperations, 0);
            Interlocked.Exchange(ref _totalResponseTime, 0);
            Interlocked.Exchange(ref _totalDatabaseTime, 0);
            Interlocked.Exchange(ref _totalCacheTime, 0);
            Interlocked.Exchange(ref _errorCount, 0);
            Interlocked.Exchange(ref _databaseErrorCount, 0);
            Interlocked.Exchange(ref _cacheHitCount, 0);

            _endpointMetrics.Clear();
            _databaseMetrics.Clear();
            _cacheMetrics.Clear();
            _customMetrics.Clear();

            _logger.LogInformation("Performance metrics reset");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting performance metrics");
        }
    }

    private async Task<SystemMetrics> GetSystemMetricsAsync()
    {
        try
        {
            var process = Process.GetCurrentProcess();
            
            return new SystemMetrics
            {
                CpuUsage = await GetCpuUsageAsync(),
                MemoryUsage = GC.GetTotalMemory(false),
                WorkingSet = process.WorkingSet64,
                ThreadCount = process.Threads.Count,
                HandleCount = process.HandleCount,
                GcCollections = new Dictionary<int, long>
                {
                    [0] = GC.CollectionCount(0),
                    [1] = GC.CollectionCount(1),
                    [2] = GC.CollectionCount(2)
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting system metrics");
            return new SystemMetrics();
        }
    }

    private async Task<double> GetCpuUsageAsync()
    {
        try
        {
            var process = Process.GetCurrentProcess();
            var startTime = DateTime.UtcNow;
            var startCpuUsage = process.TotalProcessorTime;
            
            await Task.Delay(500); // Wait 500ms to measure CPU usage
            
            var endTime = DateTime.UtcNow;
            var endCpuUsage = process.TotalProcessorTime;
            
            var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
            var totalMsPassed = (endTime - startTime).TotalMilliseconds;
            var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);
            
            return cpuUsageTotal * 100;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating CPU usage");
            return 0;
        }
    }
}
