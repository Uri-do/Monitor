using MonitoringGrid.Api.Models;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http.Features;

namespace MonitoringGrid.Api.Monitoring;

/// <summary>
/// Advanced performance monitoring service with comprehensive metrics
/// </summary>
public interface IAdvancedPerformanceMonitoring
{
    /// <summary>
    /// Records request performance metrics
    /// </summary>
    void RecordRequest(string endpoint, string method, int statusCode, long durationMs, long requestSize, long responseSize);

    /// <summary>
    /// Records database operation metrics
    /// </summary>
    void RecordDatabaseOperation(string operation, string table, long durationMs, bool success);

    /// <summary>
    /// Records cache operation metrics
    /// </summary>
    void RecordCacheOperation(string operation, string key, bool hit, long durationMs);

    /// <summary>
    /// Records external service call metrics
    /// </summary>
    void RecordExternalServiceCall(string service, string operation, long durationMs, bool success);

    /// <summary>
    /// Gets current performance snapshot
    /// </summary>
    Task<PerformanceSnapshot> GetPerformanceSnapshotAsync();

    /// <summary>
    /// Gets performance trends over time
    /// </summary>
    Task<PerformanceTrends> GetPerformanceTrendsAsync(TimeSpan period);
}

/// <summary>
/// Implementation of advanced performance monitoring
/// </summary>
public class AdvancedPerformanceMonitoring : IAdvancedPerformanceMonitoring
{
    private readonly Meter _meter;
    private readonly ILogger<AdvancedPerformanceMonitoring> _logger;
    
    // Counters
    private readonly Counter<long> _requestCounter;
    private readonly Counter<long> _errorCounter;
    private readonly Counter<long> _databaseOperationCounter;
    private readonly Counter<long> _cacheOperationCounter;
    private readonly Counter<long> _externalServiceCounter;
    
    // Histograms
    private readonly Histogram<double> _requestDuration;
    private readonly Histogram<double> _databaseDuration;
    private readonly Histogram<double> _cacheDuration;
    private readonly Histogram<double> _externalServiceDuration;
    
    // Gauges (using ObservableGauge)
    private readonly ObservableGauge<long> _activeConnections;
    private readonly ObservableGauge<long> _memoryUsage;
    private readonly ObservableGauge<double> _cpuUsage;
    
    // Performance tracking
    private readonly ConcurrentDictionary<string, PerformanceMetrics> _endpointMetrics = new();
    private readonly ConcurrentDictionary<string, DatabaseMetrics> _databaseMetrics = new();
    private readonly ConcurrentDictionary<string, CacheMetrics> _cacheMetrics = new();

    public AdvancedPerformanceMonitoring(ILogger<AdvancedPerformanceMonitoring> logger)
    {
        _logger = logger;
        _meter = new Meter("MonitoringGrid.Api", "1.0.0");

        // Initialize counters
        _requestCounter = _meter.CreateCounter<long>("api_requests_total", "requests", "Total number of API requests");
        _errorCounter = _meter.CreateCounter<long>("api_errors_total", "errors", "Total number of API errors");
        _databaseOperationCounter = _meter.CreateCounter<long>("database_operations_total", "operations", "Total database operations");
        _cacheOperationCounter = _meter.CreateCounter<long>("cache_operations_total", "operations", "Total cache operations");
        _externalServiceCounter = _meter.CreateCounter<long>("external_service_calls_total", "calls", "Total external service calls");

        // Initialize histograms
        _requestDuration = _meter.CreateHistogram<double>("api_request_duration_ms", "ms", "API request duration in milliseconds");
        _databaseDuration = _meter.CreateHistogram<double>("database_operation_duration_ms", "ms", "Database operation duration");
        _cacheDuration = _meter.CreateHistogram<double>("cache_operation_duration_ms", "ms", "Cache operation duration");
        _externalServiceDuration = _meter.CreateHistogram<double>("external_service_duration_ms", "ms", "External service call duration");

        // Initialize gauges
        _activeConnections = _meter.CreateObservableGauge<long>("active_connections", "connections", "Number of active connections");
        _memoryUsage = _meter.CreateObservableGauge<long>("memory_usage_bytes", "bytes", "Current memory usage");
        _cpuUsage = _meter.CreateObservableGauge<double>("cpu_usage_percent", "percent", "Current CPU usage percentage");
    }

    public void RecordRequest(string endpoint, string method, int statusCode, long durationMs, long requestSize, long responseSize)
    {
        var tags = new TagList
        {
            ["endpoint"] = endpoint,
            ["method"] = method,
            ["status_code"] = statusCode.ToString(),
            ["status_class"] = GetStatusClass(statusCode)
        };

        _requestCounter.Add(1, tags);
        _requestDuration.Record(durationMs, tags);

        if (statusCode >= 400)
        {
            _errorCounter.Add(1, tags);
        }

        // Update endpoint metrics
        var key = $"{method}:{endpoint}";
        _endpointMetrics.AddOrUpdate(key, 
            new PerformanceMetrics { TotalRequests = 1, TotalDuration = durationMs, TotalRequestSize = requestSize, TotalResponseSize = responseSize },
            (k, existing) => 
            {
                existing.TotalRequests++;
                existing.TotalDuration += durationMs;
                existing.TotalRequestSize += requestSize;
                existing.TotalResponseSize += responseSize;
                existing.LastRequestTime = DateTime.UtcNow;
                if (statusCode >= 400) existing.ErrorCount++;
                return existing;
            });

        _logger.LogDebug("Recorded request: {Method} {Endpoint} - {StatusCode} in {Duration}ms", 
            method, endpoint, statusCode, durationMs);
    }

    public void RecordDatabaseOperation(string operation, string table, long durationMs, bool success)
    {
        var tags = new TagList
        {
            ["operation"] = operation,
            ["table"] = table,
            ["success"] = success.ToString()
        };

        _databaseOperationCounter.Add(1, tags);
        _databaseDuration.Record(durationMs, tags);

        // Update database metrics
        var key = $"{operation}:{table}";
        _databaseMetrics.AddOrUpdate(key,
            new DatabaseMetrics { TotalOperations = 1, TotalDuration = durationMs, SuccessCount = success ? 1 : 0 },
            (k, existing) =>
            {
                existing.TotalOperations++;
                existing.TotalDuration += durationMs;
                if (success) existing.SuccessCount++;
                existing.LastOperationTime = DateTime.UtcNow;
                return existing;
            });
    }

    public void RecordCacheOperation(string operation, string key, bool hit, long durationMs)
    {
        var tags = new TagList
        {
            ["operation"] = operation,
            ["hit"] = hit.ToString()
        };

        _cacheOperationCounter.Add(1, tags);
        _cacheDuration.Record(durationMs, tags);

        // Update cache metrics
        _cacheMetrics.AddOrUpdate(operation,
            new CacheMetrics { TotalOperations = 1, TotalDuration = durationMs, HitCount = hit ? 1 : 0 },
            (k, existing) =>
            {
                existing.TotalOperations++;
                existing.TotalDuration += durationMs;
                if (hit) existing.HitCount++;
                existing.LastOperationTime = DateTime.UtcNow;
                return existing;
            });
    }

    public void RecordExternalServiceCall(string service, string operation, long durationMs, bool success)
    {
        var tags = new TagList
        {
            ["service"] = service,
            ["operation"] = operation,
            ["success"] = success.ToString()
        };

        _externalServiceCounter.Add(1, tags);
        _externalServiceDuration.Record(durationMs, tags);
    }

    public async Task<PerformanceSnapshot> GetPerformanceSnapshotAsync()
    {
        var snapshot = new PerformanceSnapshot
        {
            Timestamp = DateTime.UtcNow,
            SystemMetrics = await GetSystemMetricsAsync(),
            EndpointMetrics = GetEndpointMetricsSnapshot(),
            DatabaseMetrics = GetDatabaseMetricsSnapshot(),
            CacheMetrics = GetCacheMetricsSnapshot()
        };

        return snapshot;
    }

    public async Task<PerformanceTrends> GetPerformanceTrendsAsync(TimeSpan period)
    {
        // This would typically query a time-series database
        // For now, return current metrics as trends
        var trends = new PerformanceTrends
        {
            Period = period,
            StartTime = DateTime.UtcNow - period,
            EndTime = DateTime.UtcNow,
            RequestTrends = new List<RequestTrendPoint>(),
            ResponseTimeTrends = new List<ResponseTimeTrendPoint>(),
            ErrorRateTrends = new List<ErrorRateTrendPoint>()
        };

        return trends;
    }

    private static string GetStatusClass(int statusCode)
    {
        return statusCode switch
        {
            >= 200 and < 300 => "2xx",
            >= 300 and < 400 => "3xx",
            >= 400 and < 500 => "4xx",
            >= 500 => "5xx",
            _ => "unknown"
        };
    }

    private async Task<SystemMetrics> GetSystemMetricsAsync()
    {
        var process = Process.GetCurrentProcess();
        
        return new SystemMetrics
        {
            MemoryUsage = GC.GetTotalMemory(false),
            WorkingSet = process.WorkingSet64,
            CpuUsage = await GetCpuUsageAsync(),
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

    private async Task<double> GetCpuUsageAsync()
    {
        // Simple CPU usage calculation
        var startTime = DateTime.UtcNow;
        var startCpuUsage = Process.GetCurrentProcess().TotalProcessorTime;
        
        await Task.Delay(100);
        
        var endTime = DateTime.UtcNow;
        var endCpuUsage = Process.GetCurrentProcess().TotalProcessorTime;
        
        var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
        var totalMsPassed = (endTime - startTime).TotalMilliseconds;
        var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);
        
        return cpuUsageTotal * 100;
    }

    private Dictionary<string, EndpointMetricsSnapshot> GetEndpointMetricsSnapshot()
    {
        return _endpointMetrics.ToDictionary(
            kvp => kvp.Key,
            kvp => new EndpointMetricsSnapshot
            {
                TotalRequests = kvp.Value.TotalRequests,
                AverageResponseTime = kvp.Value.TotalRequests > 0 ? (double)kvp.Value.TotalDuration / kvp.Value.TotalRequests : 0,
                ErrorRate = kvp.Value.TotalRequests > 0 ? (double)kvp.Value.ErrorCount / kvp.Value.TotalRequests : 0,
                RequestsPerSecond = CalculateRequestsPerSecond(kvp.Value),
                LastRequestTime = kvp.Value.LastRequestTime
            });
    }

    private Dictionary<string, DatabaseMetricsSnapshot> GetDatabaseMetricsSnapshot()
    {
        return _databaseMetrics.ToDictionary(
            kvp => kvp.Key,
            kvp => new DatabaseMetricsSnapshot
            {
                TotalOperations = kvp.Value.TotalOperations,
                AverageResponseTime = kvp.Value.TotalOperations > 0 ? (double)kvp.Value.TotalDuration / kvp.Value.TotalOperations : 0,
                SuccessRate = kvp.Value.TotalOperations > 0 ? (double)kvp.Value.SuccessCount / kvp.Value.TotalOperations : 0,
                LastOperationTime = kvp.Value.LastOperationTime
            });
    }

    private Dictionary<string, CacheMetricsSnapshot> GetCacheMetricsSnapshot()
    {
        return _cacheMetrics.ToDictionary(
            kvp => kvp.Key,
            kvp => new CacheMetricsSnapshot
            {
                TotalOperations = kvp.Value.TotalOperations,
                HitRate = kvp.Value.TotalOperations > 0 ? (double)kvp.Value.HitCount / kvp.Value.TotalOperations : 0,
                AverageResponseTime = kvp.Value.TotalOperations > 0 ? (double)kvp.Value.TotalDuration / kvp.Value.TotalOperations : 0,
                LastOperationTime = kvp.Value.LastOperationTime
            });
    }

    private double CalculateRequestsPerSecond(PerformanceMetrics metrics)
    {
        var timeSinceFirst = DateTime.UtcNow - metrics.FirstRequestTime;
        return timeSinceFirst.TotalSeconds > 0 ? metrics.TotalRequests / timeSinceFirst.TotalSeconds : 0;
    }

    public void Dispose()
    {
        _meter?.Dispose();
    }
}
