using System.Diagnostics;

namespace MonitoringGrid.Api.Observability;

/// <summary>
/// Extensions for structured logging with consistent format
/// </summary>
public static class StructuredLoggingExtensions
{
    /// <summary>
    /// Log KPI execution start
    /// </summary>
    public static void LogKpiExecutionStart(this ILogger logger, int kpiId, string indicator, string owner)
    {
        logger.LogInformation("KPI execution started: {KpiId} - {Indicator} (Owner: {Owner}) [TraceId: {TraceId}]",
            kpiId, indicator, owner, Activity.Current?.TraceId);
    }

    /// <summary>
    /// Log KPI execution completion
    /// </summary>
    public static void LogKpiExecutionCompleted(this ILogger logger, int kpiId, string indicator, 
        TimeSpan duration, bool success, string? result = null)
    {
        if (success)
        {
            logger.LogInformation("KPI execution completed successfully: {KpiId} - {Indicator} " +
                "in {Duration}ms. Result: {Result} [TraceId: {TraceId}]",
                kpiId, indicator, duration.TotalMilliseconds, result, Activity.Current?.TraceId);
        }
        else
        {
            logger.LogWarning("KPI execution failed: {KpiId} - {Indicator} " +
                "after {Duration}ms. Error: {Result} [TraceId: {TraceId}]",
                kpiId, indicator, duration.TotalMilliseconds, result, Activity.Current?.TraceId);
        }
    }

    /// <summary>
    /// Log KPI execution error
    /// </summary>
    public static void LogKpiExecutionError(this ILogger logger, int kpiId, string indicator, 
        Exception exception, TimeSpan? duration = null)
    {
        logger.LogError(exception, "KPI execution error: {KpiId} - {Indicator} " +
            "{Duration} [TraceId: {TraceId}] [ErrorType: {ErrorType}]",
            kpiId, indicator, 
            duration.HasValue ? $"after {duration.Value.TotalMilliseconds}ms" : "",
            Activity.Current?.TraceId, exception.GetType().Name);
    }

    /// <summary>
    /// Log alert triggered
    /// </summary>
    public static void LogAlertTriggered(this ILogger logger, int kpiId, string indicator, 
        decimal deviationPercent, string severity, string recipient)
    {
        logger.LogWarning("Alert triggered: {KpiId} - {Indicator} " +
            "Deviation: {DeviationPercent}% (Severity: {Severity}) " +
            "Sent to: {Recipient} [TraceId: {TraceId}]",
            kpiId, indicator, deviationPercent, severity, recipient, Activity.Current?.TraceId);
    }

    /// <summary>
    /// Log alert resolution
    /// </summary>
    public static void LogAlertResolved(this ILogger logger, long alertId, int kpiId, 
        string resolvedBy, TimeSpan resolutionTime)
    {
        logger.LogInformation("Alert resolved: {AlertId} for KPI {KpiId} " +
            "by {ResolvedBy} after {ResolutionTime} [TraceId: {TraceId}]",
            alertId, kpiId, resolvedBy, resolutionTime, Activity.Current?.TraceId);
    }

    /// <summary>
    /// Log performance warning
    /// </summary>
    public static void LogPerformanceWarning(this ILogger logger, string operation, 
        TimeSpan duration, TimeSpan threshold, Dictionary<string, object>? additionalData = null)
    {
        var logData = new Dictionary<string, object>
        {
            ["Operation"] = operation,
            ["Duration"] = duration.TotalMilliseconds,
            ["Threshold"] = threshold.TotalMilliseconds,
            ["TraceId"] = Activity.Current?.TraceId.ToString() ?? "N/A"
        };

        if (additionalData != null)
        {
            foreach (var kvp in additionalData)
            {
                logData[kvp.Key] = kvp.Value;
            }
        }

        logger.LogWarning("Performance warning: {Operation} took {Duration}ms " +
            "(threshold: {Threshold}ms) [TraceId: {TraceId}] {@AdditionalData}",
            operation, duration.TotalMilliseconds, threshold.TotalMilliseconds, 
            Activity.Current?.TraceId, logData);
    }

    /// <summary>
    /// Log database operation
    /// </summary>
    public static void LogDatabaseOperation(this ILogger logger, string operation, 
        string tableName, TimeSpan duration, int? recordCount = null)
    {
        logger.LogDebug("Database operation: {Operation} on {TableName} " +
            "completed in {Duration}ms {RecordInfo} [TraceId: {TraceId}]",
            operation, tableName, duration.TotalMilliseconds,
            recordCount.HasValue ? $"({recordCount} records)" : "",
            Activity.Current?.TraceId);
    }

    /// <summary>
    /// Log bulk operation
    /// </summary>
    public static void LogBulkOperation(this ILogger logger, string operation, 
        int itemCount, TimeSpan duration, bool success)
    {
        if (success)
        {
            logger.LogInformation("Bulk operation completed: {Operation} " +
                "processed {ItemCount} items in {Duration}ms [TraceId: {TraceId}]",
                operation, itemCount, duration.TotalMilliseconds, Activity.Current?.TraceId);
        }
        else
        {
            logger.LogError("Bulk operation failed: {Operation} " +
                "failed to process {ItemCount} items after {Duration}ms [TraceId: {TraceId}]",
                operation, itemCount, duration.TotalMilliseconds, Activity.Current?.TraceId);
        }
    }

    /// <summary>
    /// Log rate limit exceeded
    /// </summary>
    public static void LogRateLimitExceeded(this ILogger logger, string clientKey, 
        string endpointCategory, int currentCount, int limit)
    {
        logger.LogWarning("Rate limit exceeded: Client {ClientKey} " +
            "exceeded {Limit} requests for {EndpointCategory} " +
            "(current: {CurrentCount}) [TraceId: {TraceId}]",
            clientKey, limit, endpointCategory, currentCount, Activity.Current?.TraceId);
    }

    /// <summary>
    /// Log security event
    /// </summary>
    public static void LogSecurityEvent(this ILogger logger, string eventType, 
        string? userId, string? ipAddress, Dictionary<string, object>? additionalData = null)
    {
        var securityData = new Dictionary<string, object>
        {
            ["EventType"] = eventType,
            ["UserId"] = userId ?? "Anonymous",
            ["IpAddress"] = ipAddress ?? "Unknown",
            ["Timestamp"] = DateTime.UtcNow,
            ["TraceId"] = Activity.Current?.TraceId.ToString() ?? "N/A"
        };

        if (additionalData != null)
        {
            foreach (var kvp in additionalData)
            {
                securityData[kvp.Key] = kvp.Value;
            }
        }

        logger.LogWarning("Security event: {EventType} " +
            "User: {UserId} IP: {IpAddress} [TraceId: {TraceId}] {@SecurityData}",
            eventType, userId ?? "Anonymous", ipAddress ?? "Unknown", 
            Activity.Current?.TraceId, securityData);
    }

    /// <summary>
    /// Log system health update
    /// </summary>
    public static void LogSystemHealthUpdate(this ILogger logger, string component, 
        string status, double? healthScore = null, Dictionary<string, object>? metrics = null)
    {
        var healthData = new Dictionary<string, object>
        {
            ["Component"] = component,
            ["Status"] = status,
            ["Timestamp"] = DateTime.UtcNow,
            ["TraceId"] = Activity.Current?.TraceId.ToString() ?? "N/A"
        };

        if (healthScore.HasValue)
        {
            healthData["HealthScore"] = healthScore.Value;
        }

        if (metrics != null)
        {
            foreach (var kvp in metrics)
            {
                healthData[kvp.Key] = kvp.Value;
            }
        }

        logger.LogInformation("System health update: {Component} is {Status} " +
            "{HealthScore} [TraceId: {TraceId}] {@HealthData}",
            component, status, 
            healthScore.HasValue ? $"(score: {healthScore:F2})" : "",
            Activity.Current?.TraceId, healthData);
    }

    /// <summary>
    /// Log external service call
    /// </summary>
    public static void LogExternalServiceCall(this ILogger logger, string serviceName, 
        string operation, TimeSpan duration, bool success, string? errorMessage = null)
    {
        if (success)
        {
            logger.LogInformation("External service call successful: {ServiceName}.{Operation} " +
                "completed in {Duration}ms [TraceId: {TraceId}]",
                serviceName, operation, duration.TotalMilliseconds, Activity.Current?.TraceId);
        }
        else
        {
            logger.LogError("External service call failed: {ServiceName}.{Operation} " +
                "failed after {Duration}ms. Error: {ErrorMessage} [TraceId: {TraceId}]",
                serviceName, operation, duration.TotalMilliseconds, errorMessage, Activity.Current?.TraceId);
        }
    }
}
