using Microsoft.Extensions.Logging;
using MonitoringGrid.Core.Interfaces;
using System.Diagnostics;

namespace MonitoringGrid.Infrastructure.Services;

/// <summary>
/// Service for monitoring application performance and providing metrics
/// </summary>
public class PerformanceMonitoringService : IPerformanceMonitoringService
{
    private readonly ILogger<PerformanceMonitoringService> _logger;
    private readonly ICacheService _cacheService;
    private readonly Dictionary<string, PerformanceCounter> _counters;
    private readonly object _lockObject = new();

    public PerformanceMonitoringService(
        ILogger<PerformanceMonitoringService> logger,
        ICacheService cacheService)
    {
        _logger = logger;
        _cacheService = cacheService;
        _counters = new Dictionary<string, PerformanceCounter>();
    }

    /// <summary>
    /// Measures execution time of an operation
    /// </summary>
    public async Task<T> MeasureAsync<T>(string operationName, Func<Task<T>> operation)
    {
        var stopwatch = Stopwatch.StartNew();
        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogDebug("Starting operation: {OperationName}", operationName);
            
            var result = await operation();
            
            stopwatch.Stop();
            var duration = stopwatch.Elapsed;
            
            await RecordMetricAsync(operationName, duration, true);
            
            _logger.LogDebug("Completed operation: {OperationName} in {Duration}ms", 
                operationName, duration.TotalMilliseconds);
            
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var duration = stopwatch.Elapsed;
            
            await RecordMetricAsync(operationName, duration, false);
            
            _logger.LogError(ex, "Failed operation: {OperationName} in {Duration}ms", 
                operationName, duration.TotalMilliseconds);
            
            throw;
        }
    }

    /// <summary>
    /// Measures execution time of a synchronous operation
    /// </summary>
    public T Measure<T>(string operationName, Func<T> operation)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogDebug("Starting operation: {OperationName}", operationName);
            
            var result = operation();
            
            stopwatch.Stop();
            var duration = stopwatch.Elapsed;
            
            _ = Task.Run(async () => await RecordMetricAsync(operationName, duration, true));
            
            _logger.LogDebug("Completed operation: {OperationName} in {Duration}ms", 
                operationName, duration.TotalMilliseconds);
            
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var duration = stopwatch.Elapsed;
            
            _ = Task.Run(async () => await RecordMetricAsync(operationName, duration, false));
            
            _logger.LogError(ex, "Failed operation: {OperationName} in {Duration}ms", 
                operationName, duration.TotalMilliseconds);
            
            throw;
        }
    }

    /// <summary>
    /// Records a custom metric
    /// </summary>
    public async Task RecordMetricAsync(string metricName, TimeSpan duration, bool success = true)
    {
        try
        {
            lock (_lockObject)
            {
                if (!_counters.TryGetValue(metricName, out var counter))
                {
                    counter = new PerformanceCounter();
                    _counters[metricName] = counter;
                }

                counter.TotalExecutions++;
                counter.TotalDuration += duration;
                
                if (success)
                {
                    counter.SuccessfulExecutions++;
                }
                else
                {
                    counter.FailedExecutions++;
                }

                if (duration > counter.MaxDuration)
                {
                    counter.MaxDuration = duration;
                }

                if (counter.MinDuration == TimeSpan.Zero || duration < counter.MinDuration)
                {
                    counter.MinDuration = duration;
                }

                counter.LastExecution = DateTime.UtcNow;
            }

            // Cache the metrics for quick retrieval
            await _cacheService.SetAsync(
                $"performance:metric:{metricName}",
                GetMetric(metricName),
                CacheExpirations.Short);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording metric: {MetricName}", metricName);
        }
    }

    /// <summary>
    /// Gets performance metrics for a specific operation
    /// </summary>
    public PerformanceMetric? GetMetric(string metricName)
    {
        lock (_lockObject)
        {
            if (!_counters.TryGetValue(metricName, out var counter))
            {
                return null;
            }

            return new PerformanceMetric
            {
                MetricName = metricName,
                TotalExecutions = counter.TotalExecutions,
                SuccessfulExecutions = counter.SuccessfulExecutions,
                FailedExecutions = counter.FailedExecutions,
                AverageDuration = counter.TotalExecutions > 0 
                    ? TimeSpan.FromTicks(counter.TotalDuration.Ticks / counter.TotalExecutions)
                    : TimeSpan.Zero,
                MinDuration = counter.MinDuration,
                MaxDuration = counter.MaxDuration,
                SuccessRate = counter.TotalExecutions > 0 
                    ? (double)counter.SuccessfulExecutions / counter.TotalExecutions * 100
                    : 0,
                LastExecution = counter.LastExecution
            };
        }
    }

    /// <summary>
    /// Gets all performance metrics
    /// </summary>
    public List<PerformanceMetric> GetAllMetrics()
    {
        lock (_lockObject)
        {
            return _counters.Keys.Select(GetMetric).Where(m => m != null).Cast<PerformanceMetric>().ToList();
        }
    }

    /// <summary>
    /// Gets system performance summary
    /// </summary>
    public async Task<SystemPerformanceSummary> GetSystemPerformanceSummaryAsync()
    {
        try
        {
            var cachedSummary = await _cacheService.GetAsync<SystemPerformanceSummary>("performance:system:summary");
            if (cachedSummary != null)
            {
                return cachedSummary;
            }

            var process = Process.GetCurrentProcess();
            var metrics = GetAllMetrics();

            var summary = new SystemPerformanceSummary
            {
                Timestamp = DateTime.UtcNow,
                ProcessId = process.Id,
                WorkingSetMemory = process.WorkingSet64,
                PrivateMemory = process.PrivateMemorySize64,
                VirtualMemory = process.VirtualMemorySize64,
                ProcessorTime = process.TotalProcessorTime,
                ThreadCount = process.Threads.Count,
                HandleCount = process.HandleCount,
                TotalOperations = metrics.Sum(m => m.TotalExecutions),
                SuccessfulOperations = metrics.Sum(m => m.SuccessfulExecutions),
                FailedOperations = metrics.Sum(m => m.FailedExecutions),
                AverageResponseTime = metrics.Any() 
                    ? TimeSpan.FromTicks((long)metrics.Average(m => m.AverageDuration.Ticks))
                    : TimeSpan.Zero,
                TopSlowOperations = metrics
                    .OrderByDescending(m => m.AverageDuration)
                    .Take(5)
                    .ToList()
            };

            await _cacheService.SetAsync("performance:system:summary", summary, CacheExpirations.Short);
            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting system performance summary");
            return new SystemPerformanceSummary { Timestamp = DateTime.UtcNow };
        }
    }

    /// <summary>
    /// Resets all performance counters
    /// </summary>
    public void ResetCounters()
    {
        lock (_lockObject)
        {
            _counters.Clear();
            _logger.LogInformation("Performance counters reset");
        }
    }

    /// <summary>
    /// Gets memory usage information
    /// </summary>
    public MemoryUsageInfo GetMemoryUsage()
    {
        try
        {
            var process = Process.GetCurrentProcess();
            var gcInfo = GC.GetTotalMemory(false);

            return new MemoryUsageInfo
            {
                WorkingSet = process.WorkingSet64,
                PrivateMemory = process.PrivateMemorySize64,
                VirtualMemory = process.VirtualMemorySize64,
                ManagedMemory = gcInfo,
                Gen0Collections = GC.CollectionCount(0),
                Gen1Collections = GC.CollectionCount(1),
                Gen2Collections = GC.CollectionCount(2)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting memory usage information");
            return new MemoryUsageInfo();
        }
    }

    /// <summary>
    /// Forces garbage collection and returns memory usage before and after
    /// </summary>
    public MemoryCleanupResult ForceGarbageCollection()
    {
        try
        {
            var beforeMemory = GC.GetTotalMemory(false);
            var beforeGen0 = GC.CollectionCount(0);
            var beforeGen1 = GC.CollectionCount(1);
            var beforeGen2 = GC.CollectionCount(2);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var afterMemory = GC.GetTotalMemory(false);
            var afterGen0 = GC.CollectionCount(0);
            var afterGen1 = GC.CollectionCount(1);
            var afterGen2 = GC.CollectionCount(2);

            var result = new MemoryCleanupResult
            {
                MemoryBefore = beforeMemory,
                MemoryAfter = afterMemory,
                MemoryFreed = beforeMemory - afterMemory,
                Gen0CollectionsTriggered = afterGen0 - beforeGen0,
                Gen1CollectionsTriggered = afterGen1 - beforeGen1,
                Gen2CollectionsTriggered = afterGen2 - beforeGen2,
                Timestamp = DateTime.UtcNow
            };

            _logger.LogInformation("Forced garbage collection freed {MemoryFreed} bytes", result.MemoryFreed);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during forced garbage collection");
            return new MemoryCleanupResult { Timestamp = DateTime.UtcNow };
        }
    }
}

/// <summary>
/// Internal performance counter for tracking metrics
/// </summary>
internal class PerformanceCounter
{
    public long TotalExecutions { get; set; }
    public long SuccessfulExecutions { get; set; }
    public long FailedExecutions { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public TimeSpan MinDuration { get; set; }
    public TimeSpan MaxDuration { get; set; }
    public DateTime LastExecution { get; set; }
}
