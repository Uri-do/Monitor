using Microsoft.AspNetCore.Mvc.Filters;
using System.Diagnostics;

namespace MonitoringGrid.Api.Filters;

/// <summary>
/// Enhanced performance monitoring filter with detailed metrics collection
/// </summary>
public class PerformanceMonitoringFilter : IAsyncActionFilter
{
    private readonly ILogger<PerformanceMonitoringFilter> _logger;
    private readonly IPerformanceMetricsService _metricsService;

    public PerformanceMonitoringFilter(
        ILogger<PerformanceMonitoringFilter> logger,
        IPerformanceMetricsService metricsService)
    {
        _logger = logger;
        _metricsService = metricsService;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var stopwatch = Stopwatch.StartNew();
        var actionName = $"{context.Controller.GetType().Name}.{context.ActionDescriptor.DisplayName}";
        var requestId = context.HttpContext.TraceIdentifier;

        var performanceContext = new PerformanceContext
        {
            ActionName = actionName,
            RequestId = requestId,
            StartTime = DateTime.UtcNow,
            HttpMethod = context.HttpContext.Request.Method,
            Path = context.HttpContext.Request.Path,
            QueryString = context.HttpContext.Request.QueryString.ToString(),
            UserAgent = context.HttpContext.Request.Headers["User-Agent"].ToString(),
            ClientIpAddress = GetClientIpAddress(context.HttpContext)
        };

        _logger.LogDebug("Starting performance monitoring for {ActionName} [{RequestId}]", actionName, requestId);

        Exception? exception = null;
        ActionExecutedContext? executedContext = null;

        try
        {
            executedContext = await next();
            exception = executedContext.Exception;
        }
        catch (Exception ex)
        {
            exception = ex;
            throw;
        }
        finally
        {
            stopwatch.Stop();
            performanceContext.EndTime = DateTime.UtcNow;
            performanceContext.Duration = stopwatch.Elapsed;
            performanceContext.Success = exception == null;
            performanceContext.StatusCode = executedContext?.HttpContext.Response.StatusCode ?? 500;
            performanceContext.Exception = exception;

            await RecordPerformanceMetrics(performanceContext);
        }
    }

    private async Task RecordPerformanceMetrics(PerformanceContext context)
    {
        var metrics = new PerformanceMetrics
        {
            ActionName = context.ActionName,
            RequestId = context.RequestId,
            Duration = context.Duration,
            Success = context.Success,
            StatusCode = context.StatusCode,
            Timestamp = context.StartTime,
            HttpMethod = context.HttpMethod,
            Path = context.Path,
            ClientIpAddress = context.ClientIpAddress,
            MemoryUsageMB = GC.GetTotalMemory(false) / 1024 / 1024,
            ThreadCount = Process.GetCurrentProcess().Threads.Count
        };

        // Log performance metrics
        if (context.Duration.TotalMilliseconds > 1000) // Slow request threshold
        {
            _logger.LogWarning("Slow request detected: {ActionName} took {Duration}ms [{RequestId}]",
                context.ActionName, context.Duration.TotalMilliseconds, context.RequestId);
        }
        else
        {
            _logger.LogDebug("Request completed: {ActionName} took {Duration}ms [{RequestId}]",
                context.ActionName, context.Duration.TotalMilliseconds, context.RequestId);
        }

        // Record metrics
        await _metricsService.RecordPerformanceMetricsAsync(metrics);

        // Record exception if any
        if (context.Exception != null)
        {
            _logger.LogError(context.Exception, "Exception in {ActionName} [{RequestId}]",
                context.ActionName, context.RequestId);
        }
    }

    private string GetClientIpAddress(HttpContext context)
    {
        var xForwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(xForwardedFor))
        {
            return xForwardedFor.Split(',')[0].Trim();
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}

/// <summary>
/// Performance context for tracking request execution
/// </summary>
public class PerformanceContext
{
    public string ActionName { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration { get; set; }
    public bool Success { get; set; }
    public int StatusCode { get; set; }
    public string HttpMethod { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string QueryString { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public string ClientIpAddress { get; set; } = string.Empty;
    public Exception? Exception { get; set; }
}

/// <summary>
/// Performance metrics data
/// </summary>
public class PerformanceMetrics
{
    public string ActionName { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public bool Success { get; set; }
    public int StatusCode { get; set; }
    public DateTime Timestamp { get; set; }
    public string HttpMethod { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string ClientIpAddress { get; set; } = string.Empty;
    public long MemoryUsageMB { get; set; }
    public int ThreadCount { get; set; }
    public Dictionary<string, object> AdditionalMetrics { get; set; } = new();
}

/// <summary>
/// Service for collecting and storing performance metrics
/// </summary>
public interface IPerformanceMetricsService
{
    Task RecordPerformanceMetricsAsync(PerformanceMetrics metrics);
    Task<PerformanceReport> GetPerformanceReportAsync(DateTime from, DateTime to);
    Task<List<SlowRequestInfo>> GetSlowRequestsAsync(int thresholdMs = 1000, int count = 50);
    Task<Dictionary<string, PerformanceStatistics>> GetActionStatisticsAsync(DateTime from, DateTime to);
    Task<SystemPerformanceMetrics> GetSystemMetricsAsync();
    void IncrementCounter(string counterName, string? category = null);
}

/// <summary>
/// Implementation of performance metrics service
/// </summary>
public class PerformanceMetricsService : IPerformanceMetricsService
{
    private readonly ILogger<PerformanceMetricsService> _logger;
    private readonly List<PerformanceMetrics> _metrics = new();
    private readonly object _lock = new();

    public PerformanceMetricsService(ILogger<PerformanceMetricsService> logger)
    {
        _logger = logger;
    }

    public Task RecordPerformanceMetricsAsync(PerformanceMetrics metrics)
    {
        lock (_lock)
        {
            _metrics.Add(metrics);
            
            // Keep only last 10000 metrics to prevent memory issues
            if (_metrics.Count > 10000)
            {
                _metrics.RemoveRange(0, 1000);
            }
        }

        return Task.CompletedTask;
    }

    public Task<PerformanceReport> GetPerformanceReportAsync(DateTime from, DateTime to)
    {
        lock (_lock)
        {
            var filteredMetrics = _metrics.Where(m => m.Timestamp >= from && m.Timestamp <= to).ToList();

            var report = new PerformanceReport
            {
                From = from,
                To = to,
                TotalRequests = filteredMetrics.Count,
                SuccessfulRequests = filteredMetrics.Count(m => m.Success),
                FailedRequests = filteredMetrics.Count(m => !m.Success),
                AverageResponseTime = filteredMetrics.Any() ? filteredMetrics.Average(m => m.Duration.TotalMilliseconds) : 0,
                MedianResponseTime = CalculateMedian(filteredMetrics.Select(m => m.Duration.TotalMilliseconds)),
                P95ResponseTime = CalculatePercentile(filteredMetrics.Select(m => m.Duration.TotalMilliseconds), 95),
                P99ResponseTime = CalculatePercentile(filteredMetrics.Select(m => m.Duration.TotalMilliseconds), 99),
                SlowRequestsCount = filteredMetrics.Count(m => m.Duration.TotalMilliseconds > 1000),
                TopSlowActions = filteredMetrics
                    .GroupBy(m => m.ActionName)
                    .Select(g => new ActionPerformance
                    {
                        ActionName = g.Key,
                        AverageResponseTime = g.Average(m => m.Duration.TotalMilliseconds),
                        RequestCount = g.Count(),
                        ErrorCount = g.Count(m => !m.Success)
                    })
                    .OrderByDescending(a => a.AverageResponseTime)
                    .Take(10)
                    .ToList()
            };

            return Task.FromResult(report);
        }
    }

    public Task<List<SlowRequestInfo>> GetSlowRequestsAsync(int thresholdMs = 1000, int count = 50)
    {
        lock (_lock)
        {
            var slowRequests = _metrics
                .Where(m => m.Duration.TotalMilliseconds > thresholdMs)
                .OrderByDescending(m => m.Duration.TotalMilliseconds)
                .Take(count)
                .Select(m => new SlowRequestInfo
                {
                    ActionName = m.ActionName,
                    RequestId = m.RequestId,
                    Duration = m.Duration,
                    Timestamp = m.Timestamp,
                    Path = m.Path,
                    HttpMethod = m.HttpMethod,
                    StatusCode = m.StatusCode,
                    ClientIpAddress = m.ClientIpAddress
                })
                .ToList();

            return Task.FromResult(slowRequests);
        }
    }

    public Task<Dictionary<string, PerformanceStatistics>> GetActionStatisticsAsync(DateTime from, DateTime to)
    {
        lock (_lock)
        {
            var filteredMetrics = _metrics.Where(m => m.Timestamp >= from && m.Timestamp <= to);

            var statistics = filteredMetrics
                .GroupBy(m => m.ActionName)
                .ToDictionary(g => g.Key, g => new PerformanceStatistics
                {
                    ActionName = g.Key,
                    TotalRequests = g.Count(),
                    SuccessfulRequests = g.Count(m => m.Success),
                    FailedRequests = g.Count(m => !m.Success),
                    AverageResponseTime = g.Average(m => m.Duration.TotalMilliseconds),
                    MinResponseTime = g.Min(m => m.Duration.TotalMilliseconds),
                    MaxResponseTime = g.Max(m => m.Duration.TotalMilliseconds),
                    MedianResponseTime = CalculateMedian(g.Select(m => m.Duration.TotalMilliseconds)),
                    P95ResponseTime = CalculatePercentile(g.Select(m => m.Duration.TotalMilliseconds), 95),
                    RequestsPerMinute = g.Count() / Math.Max(1, (to - from).TotalMinutes)
                });

            return Task.FromResult(statistics);
        }
    }

    public Task<SystemPerformanceMetrics> GetSystemMetricsAsync()
    {
        var process = Process.GetCurrentProcess();
        
        var metrics = new SystemPerformanceMetrics
        {
            Timestamp = DateTime.UtcNow,
            CpuUsagePercent = GetCpuUsage(),
            MemoryUsageMB = GC.GetTotalMemory(false) / 1024 / 1024,
            ThreadCount = process.Threads.Count,
            HandleCount = process.HandleCount,
            WorkingSetMB = process.WorkingSet64 / 1024 / 1024,
            GCGen0Collections = GC.CollectionCount(0),
            GCGen1Collections = GC.CollectionCount(1),
            GCGen2Collections = GC.CollectionCount(2)
        };

        return Task.FromResult(metrics);
    }

    public void IncrementCounter(string counterName, string? category = null)
    {
        // Simple counter implementation - in production you might want to use a more sophisticated approach
        _logger.LogDebug("Counter incremented: {CounterName} (Category: {Category})", counterName, category ?? "default");
    }

    private double CalculateMedian(IEnumerable<double> values)
    {
        var sorted = values.OrderBy(x => x).ToList();
        if (!sorted.Any()) return 0;

        var mid = sorted.Count / 2;
        return sorted.Count % 2 == 0 
            ? (sorted[mid - 1] + sorted[mid]) / 2.0 
            : sorted[mid];
    }

    private double CalculatePercentile(IEnumerable<double> values, int percentile)
    {
        var sorted = values.OrderBy(x => x).ToList();
        if (!sorted.Any()) return 0;

        var index = (percentile / 100.0) * (sorted.Count - 1);
        var lower = (int)Math.Floor(index);
        var upper = (int)Math.Ceiling(index);

        if (lower == upper) return sorted[lower];

        var weight = index - lower;
        return sorted[lower] * (1 - weight) + sorted[upper] * weight;
    }

    private double GetCpuUsage()
    {
        // Simplified CPU usage calculation
        // In a real implementation, you'd use performance counters
        return 0.0;
    }
}

/// <summary>
/// Performance report data
/// </summary>
public class PerformanceReport
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public int TotalRequests { get; set; }
    public int SuccessfulRequests { get; set; }
    public int FailedRequests { get; set; }
    public double AverageResponseTime { get; set; }
    public double MedianResponseTime { get; set; }
    public double P95ResponseTime { get; set; }
    public double P99ResponseTime { get; set; }
    public int SlowRequestsCount { get; set; }
    public List<ActionPerformance> TopSlowActions { get; set; } = new();
}

/// <summary>
/// Action performance data
/// </summary>
public class ActionPerformance
{
    public string ActionName { get; set; } = string.Empty;
    public double AverageResponseTime { get; set; }
    public int RequestCount { get; set; }
    public int ErrorCount { get; set; }
}

/// <summary>
/// Slow request information
/// </summary>
public class SlowRequestInfo
{
    public string ActionName { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public DateTime Timestamp { get; set; }
    public string Path { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public string ClientIpAddress { get; set; } = string.Empty;
}

/// <summary>
/// Performance statistics for an action
/// </summary>
public class PerformanceStatistics
{
    public string ActionName { get; set; } = string.Empty;
    public int TotalRequests { get; set; }
    public int SuccessfulRequests { get; set; }
    public int FailedRequests { get; set; }
    public double AverageResponseTime { get; set; }
    public double MinResponseTime { get; set; }
    public double MaxResponseTime { get; set; }
    public double MedianResponseTime { get; set; }
    public double P95ResponseTime { get; set; }
    public double RequestsPerMinute { get; set; }
}

/// <summary>
/// System performance metrics
/// </summary>
public class SystemPerformanceMetrics
{
    public DateTime Timestamp { get; set; }
    public double CpuUsagePercent { get; set; }
    public long MemoryUsageMB { get; set; }
    public int ThreadCount { get; set; }
    public int HandleCount { get; set; }
    public long WorkingSetMB { get; set; }
    public int GCGen0Collections { get; set; }
    public int GCGen1Collections { get; set; }
    public int GCGen2Collections { get; set; }
}
