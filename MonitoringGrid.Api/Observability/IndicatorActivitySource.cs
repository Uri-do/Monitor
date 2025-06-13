using System.Diagnostics;

namespace MonitoringGrid.Api.Observability;

/// <summary>
/// Custom activity source for Indicator execution tracing
/// </summary>
public static class IndicatorActivitySource
{
    public static readonly ActivitySource Source = new("MonitoringGrid.IndicatorExecution", "1.0.0");

    /// <summary>
    /// Start an Indicator execution activity
    /// </summary>
    public static Activity? StartIndicatorExecution(long indicatorID, string indicator)
    {
        return Source.StartActivity("ExecuteIndicator", ActivityKind.Internal)
            ?.SetTag("indicator.id", indicatorID)
            ?.SetTag("indicator.name", indicator)
            ?.SetTag("operation.type", "indicator_execution");
    }

    /// <summary>
    /// Start an Indicator validation activity
    /// </summary>
    public static Activity? StartIndicatorValidation(long indicatorID, string indicator)
    {
        return Source.StartActivity("ValidateIndicator", ActivityKind.Internal)
            ?.SetTag("indicator.id", indicatorID)
            ?.SetTag("indicator.name", indicator)
            ?.SetTag("operation.type", "indicator_validation");
    }

    /// <summary>
    /// Start a database query activity for Indicator
    /// </summary>
    public static Activity? StartDatabaseQuery(long indicatorID, string storedProcedure)
    {
        return Source.StartActivity("ExecuteStoredProcedure", ActivityKind.Client)
            ?.SetTag("indicator.id", indicatorID)
            ?.SetTag("db.operation", "stored_procedure")
            ?.SetTag("db.sql.table", storedProcedure)
            ?.SetTag("operation.type", "database_query");
    }

    /// <summary>
    /// Start an alert processing activity
    /// </summary>
    public static Activity? StartAlertProcessing(long indicatorID, string alertType)
    {
        return Source.StartActivity("ProcessAlert", ActivityKind.Internal)
            ?.SetTag("indicator.id", indicatorID)
            ?.SetTag("alert.type", alertType)
            ?.SetTag("operation.type", "alert_processing");
    }

    /// <summary>
    /// Start a notification sending activity
    /// </summary>
    public static Activity? StartNotificationSending(string channel, string recipient)
    {
        return Source.StartActivity("SendNotification", ActivityKind.Producer)
            ?.SetTag("notification.channel", channel)
            ?.SetTag("notification.recipient", recipient)
            ?.SetTag("operation.type", "notification_sending");
    }

    /// <summary>
    /// Add error information to current activity
    /// </summary>
    public static void RecordError(Activity? activity, Exception exception)
    {
        activity?.SetStatus(ActivityStatusCode.Error, exception.Message)
            ?.SetTag("error.type", exception.GetType().Name)
            ?.SetTag("error.message", exception.Message);
    }

    /// <summary>
    /// Add success information to current activity
    /// </summary>
    public static void RecordSuccess(Activity? activity, string? result = null)
    {
        activity?.SetStatus(ActivityStatusCode.Ok)
            ?.SetTag("operation.success", true);
        
        if (!string.IsNullOrEmpty(result))
        {
            activity?.SetTag("operation.result", result);
        }
    }

    /// <summary>
    /// Add performance metrics to activity
    /// </summary>
    public static void RecordPerformanceMetrics(Activity? activity, long durationMs, int? recordCount = null)
    {
        activity?.SetTag("performance.duration_ms", durationMs)
            ?.SetTag("performance.is_slow", durationMs > 1000);

        if (recordCount.HasValue)
        {
            activity?.SetTag("performance.record_count", recordCount.Value);
        }
    }
}

/// <summary>
/// Custom activity source for API operations
/// </summary>
public static class ApiActivitySource
{
    public static readonly ActivitySource Source = new("MonitoringGrid.Api", "1.0.0");

    /// <summary>
    /// Start a bulk operation activity
    /// </summary>
    public static Activity? StartBulkOperation(string operationType, int itemCount)
    {
        return Source.StartActivity($"BulkOperation.{operationType}", ActivityKind.Internal)
            ?.SetTag("bulk.operation_type", operationType)
            ?.SetTag("bulk.item_count", itemCount)
            ?.SetTag("operation.type", "bulk_operation");
    }

    /// <summary>
    /// Start a dashboard data aggregation activity
    /// </summary>
    public static Activity? StartDashboardAggregation(string dashboardType)
    {
        return Source.StartActivity("AggregateDashboardData", ActivityKind.Internal)
            ?.SetTag("dashboard.type", dashboardType)
            ?.SetTag("operation.type", "dashboard_aggregation");
    }

    /// <summary>
    /// Start an analytics calculation activity
    /// </summary>
    public static Activity? StartAnalyticsCalculation(string calculationType, long? indicatorId = null)
    {
        var activity = Source.StartActivity($"CalculateAnalytics.{calculationType}", ActivityKind.Internal)
            ?.SetTag("analytics.calculation_type", calculationType)
            ?.SetTag("operation.type", "analytics_calculation");

        if (indicatorId.HasValue)
        {
            activity?.SetTag("indicator.id", indicatorId.Value);
        }

        return activity;
    }
}
