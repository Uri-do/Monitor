using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MonitoringGrid.Api.Filters;

/// <summary>
/// Performance monitoring filter to track slow API calls
/// </summary>
public class PerformanceMonitorAttribute : ActionFilterAttribute
{
    private readonly int _slowThresholdMs;
    private readonly bool _logAllRequests;

    public PerformanceMonitorAttribute(int slowThresholdMs = 1000, bool logAllRequests = false)
    {
        _slowThresholdMs = slowThresholdMs;
        _logAllRequests = logAllRequests;
    }

    public override async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var stopwatch = Stopwatch.StartNew();
        var actionName = $"{context.Controller.GetType().Name}.{context.ActionDescriptor.DisplayName}";
        var requestId = context.HttpContext.TraceIdentifier;
        
        var logger = context.HttpContext.RequestServices
            .GetRequiredService<ILogger<PerformanceMonitorAttribute>>();

        if (_logAllRequests)
        {
            logger.LogInformation("Starting execution of {ActionName} [RequestId: {RequestId}]", 
                actionName, requestId);
        }

        var result = await next();
        stopwatch.Stop();

        var elapsedMs = stopwatch.ElapsedMilliseconds;
        
        // Add performance headers
        context.HttpContext.Response.Headers["X-Response-Time-Ms"] = elapsedMs.ToString();
        context.HttpContext.Response.Headers["X-Request-Id"] = requestId;

        // Log performance metrics
        if (elapsedMs > _slowThresholdMs)
        {
            logger.LogWarning("Slow API call detected: {ActionName} took {ElapsedMs}ms [RequestId: {RequestId}] [StatusCode: {StatusCode}]",
                actionName, elapsedMs, requestId, context.HttpContext.Response.StatusCode);
        }
        else if (_logAllRequests)
        {
            logger.LogInformation("Completed execution of {ActionName} in {ElapsedMs}ms [RequestId: {RequestId}] [StatusCode: {StatusCode}]",
                actionName, elapsedMs, requestId, context.HttpContext.Response.StatusCode);
        }

        // Record metrics for monitoring systems
        RecordPerformanceMetrics(actionName, elapsedMs, context.HttpContext.Response.StatusCode);
    }

    private static void RecordPerformanceMetrics(string actionName, long elapsedMs, int statusCode)
    {
        // This could be extended to integrate with metrics systems like Prometheus
        // For now, we'll use Activity tags for observability
        using var activity = Activity.Current;
        activity?.SetTag("action.name", actionName);
        activity?.SetTag("action.duration_ms", elapsedMs);
        activity?.SetTag("action.status_code", statusCode);
        activity?.SetTag("action.is_slow", elapsedMs > 1000);
    }
}

/// <summary>
/// Specialized performance monitor for database operations
/// </summary>
public class DatabasePerformanceMonitorAttribute : ActionFilterAttribute
{
    private readonly int _slowQueryThresholdMs;

    public DatabasePerformanceMonitorAttribute(int slowQueryThresholdMs = 500)
    {
        _slowQueryThresholdMs = slowQueryThresholdMs;
    }

    public override async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var stopwatch = Stopwatch.StartNew();
        var actionName = $"{context.Controller.GetType().Name}.{context.ActionDescriptor.DisplayName}";
        var requestId = context.HttpContext.TraceIdentifier;
        
        var logger = context.HttpContext.RequestServices
            .GetRequiredService<ILogger<DatabasePerformanceMonitorAttribute>>();

        var result = await next();
        stopwatch.Stop();

        var elapsedMs = stopwatch.ElapsedMilliseconds;

        if (elapsedMs > _slowQueryThresholdMs)
        {
            logger.LogWarning("Slow database operation detected: {ActionName} took {ElapsedMs}ms [RequestId: {RequestId}]",
                actionName, elapsedMs, requestId);
        }

        // Record database-specific metrics
        using var activity = Activity.Current;
        activity?.SetTag("db.operation", actionName);
        activity?.SetTag("db.duration_ms", elapsedMs);
        activity?.SetTag("db.is_slow", elapsedMs > _slowQueryThresholdMs);
    }
}

/// <summary>
/// Performance monitor for KPI execution operations
/// </summary>
public class KpiPerformanceMonitorAttribute : ActionFilterAttribute
{
    public override async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var stopwatch = Stopwatch.StartNew();
        var actionName = context.ActionDescriptor.DisplayName;
        var requestId = context.HttpContext.TraceIdentifier;
        
        var logger = context.HttpContext.RequestServices
            .GetRequiredService<ILogger<KpiPerformanceMonitorAttribute>>();

        // Extract KPI ID if available
        var kpiId = context.ActionArguments.ContainsKey("id") ? context.ActionArguments["id"]?.ToString() : "unknown";

        logger.LogInformation("Starting KPI operation: {ActionName} for KPI {KpiId} [RequestId: {RequestId}]", 
            actionName, kpiId, requestId);

        var result = await next();
        stopwatch.Stop();

        var elapsedMs = stopwatch.ElapsedMilliseconds;
        var statusCode = context.HttpContext.Response.StatusCode;

        logger.LogInformation("Completed KPI operation: {ActionName} for KPI {KpiId} in {ElapsedMs}ms [RequestId: {RequestId}] [StatusCode: {StatusCode}]",
            actionName, kpiId, elapsedMs, requestId, statusCode);

        // Record KPI-specific metrics
        using var activity = Activity.Current;
        activity?.SetTag("kpi.operation", actionName);
        activity?.SetTag("kpi.id", kpiId);
        activity?.SetTag("kpi.duration_ms", elapsedMs);
        activity?.SetTag("kpi.status_code", statusCode);
        activity?.SetTag("kpi.success", statusCode < 400);
    }
}
