using Prometheus;

namespace MonitoringGrid.Api.Observability;

/// <summary>
/// Service for recording custom metrics using Prometheus
/// </summary>
public class MetricsService
{
    // KPI Execution Metrics
    private readonly Counter _kpiExecutions = Metrics
        .CreateCounter("monitoringgrid_kpi_executions_total", 
            "Total number of KPI executions", 
            new[] { "kpi_name", "status", "owner" });

    private readonly Histogram _kpiExecutionDuration = Metrics
        .CreateHistogram("monitoringgrid_kpi_execution_duration_seconds", 
            "KPI execution duration in seconds",
            new[] { "kpi_name", "owner" });

    private readonly Gauge _activeKpis = Metrics
        .CreateGauge("monitoringgrid_active_kpis", 
            "Number of active KPIs");

    private readonly Gauge _staleKpis = Metrics
        .CreateGauge("monitoringgrid_stale_kpis", 
            "Number of stale KPIs (not executed recently)");

    // Alert Metrics
    private readonly Counter _alertsTriggered = Metrics
        .CreateCounter("monitoringgrid_alerts_triggered_total", 
            "Total number of alerts triggered", 
            new[] { "kpi_name", "severity", "owner" });

    private readonly Gauge _activeAlerts = Metrics
        .CreateGauge("monitoringgrid_active_alerts", 
            "Number of active (unresolved) alerts",
            new[] { "severity" });

    private readonly Histogram _alertResolutionTime = Metrics
        .CreateHistogram("monitoringgrid_alert_resolution_duration_seconds", 
            "Time taken to resolve alerts",
            new[] { "severity" });

    // API Performance Metrics
    private readonly Counter _apiRequests = Metrics
        .CreateCounter("monitoringgrid_api_requests_total", 
            "Total number of API requests", 
            new[] { "method", "endpoint", "status_code" });

    private readonly Histogram _apiRequestDuration = Metrics
        .CreateHistogram("monitoringgrid_api_request_duration_seconds", 
            "API request duration in seconds",
            new[] { "method", "endpoint" });

    private readonly Counter _rateLimitExceeded = Metrics
        .CreateCounter("monitoringgrid_rate_limit_exceeded_total", 
            "Total number of rate limit violations", 
            new[] { "endpoint_category", "client_type" });

    // Database Metrics
    private readonly Histogram _databaseQueryDuration = Metrics
        .CreateHistogram("monitoringgrid_database_query_duration_seconds", 
            "Database query duration in seconds",
            new[] { "operation_type", "table_name" });

    private readonly Counter _databaseErrors = Metrics
        .CreateCounter("monitoringgrid_database_errors_total", 
            "Total number of database errors", 
            new[] { "operation_type", "error_type" });

    // Business Metrics
    private readonly Gauge _systemHealth = Metrics
        .CreateGauge("monitoringgrid_system_health_score", 
            "Overall system health score (0-100)");

    private readonly Counter _bulkOperations = Metrics
        .CreateCounter("monitoringgrid_bulk_operations_total", 
            "Total number of bulk operations", 
            new[] { "operation_type", "status" });

    private readonly Histogram _bulkOperationSize = Metrics
        .CreateHistogram("monitoringgrid_bulk_operation_size", 
            "Number of items in bulk operations",
            new[] { "operation_type" });

    /// <summary>
    /// Record KPI execution metrics
    /// </summary>
    public void RecordKpiExecution(string kpiName, string owner, double durationSeconds, bool success)
    {
        var status = success ? "success" : "failure";
        _kpiExecutions.WithLabels(kpiName, status, owner).Inc();
        _kpiExecutionDuration.WithLabels(kpiName, owner).Observe(durationSeconds);
    }

    /// <summary>
    /// Update KPI status metrics
    /// </summary>
    public void UpdateKpiStatus(int activeCount, int staleCount)
    {
        _activeKpis.Set(activeCount);
        _staleKpis.Set(staleCount);
    }

    /// <summary>
    /// Record alert triggered
    /// </summary>
    public void RecordAlertTriggered(string kpiName, string owner, string severity)
    {
        _alertsTriggered.WithLabels(kpiName, severity, owner).Inc();
    }

    /// <summary>
    /// Update active alerts count
    /// </summary>
    public void UpdateActiveAlerts(int criticalCount, int highCount, int mediumCount, int lowCount)
    {
        _activeAlerts.WithLabels("critical").Set(criticalCount);
        _activeAlerts.WithLabels("high").Set(highCount);
        _activeAlerts.WithLabels("medium").Set(mediumCount);
        _activeAlerts.WithLabels("low").Set(lowCount);
    }

    /// <summary>
    /// Record alert resolution
    /// </summary>
    public void RecordAlertResolution(string severity, double resolutionTimeSeconds)
    {
        _alertResolutionTime.WithLabels(severity).Observe(resolutionTimeSeconds);
    }

    /// <summary>
    /// Record API request metrics
    /// </summary>
    public void RecordApiRequest(string method, string endpoint, int statusCode, double durationSeconds)
    {
        _apiRequests.WithLabels(method, endpoint, statusCode.ToString()).Inc();
        _apiRequestDuration.WithLabels(method, endpoint).Observe(durationSeconds);
    }

    /// <summary>
    /// Record rate limit exceeded
    /// </summary>
    public void RecordRateLimitExceeded(string endpointCategory, string clientType)
    {
        _rateLimitExceeded.WithLabels(endpointCategory, clientType).Inc();
    }

    /// <summary>
    /// Record database query metrics
    /// </summary>
    public void RecordDatabaseQuery(string operationType, string tableName, double durationSeconds)
    {
        _databaseQueryDuration.WithLabels(operationType, tableName).Observe(durationSeconds);
    }

    /// <summary>
    /// Record database error
    /// </summary>
    public void RecordDatabaseError(string operationType, string errorType)
    {
        _databaseErrors.WithLabels(operationType, errorType).Inc();
    }

    /// <summary>
    /// Update system health score
    /// </summary>
    public void UpdateSystemHealth(double healthScore)
    {
        _systemHealth.Set(healthScore);
    }

    /// <summary>
    /// Record bulk operation metrics
    /// </summary>
    public void RecordBulkOperation(string operationType, int itemCount, bool success)
    {
        var status = success ? "success" : "failure";
        _bulkOperations.WithLabels(operationType, status).Inc();
        _bulkOperationSize.WithLabels(operationType).Observe(itemCount);
    }

    /// <summary>
    /// Get current metrics summary for health checks
    /// </summary>
    public MetricsSummary GetMetricsSummary()
    {
        return new MetricsSummary
        {
            ActiveKpis = (int)_activeKpis.Value,
            StaleKpis = (int)_staleKpis.Value,
            SystemHealthScore = _systemHealth.Value,
            Timestamp = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Summary of key metrics for health checks and monitoring
/// </summary>
public class MetricsSummary
{
    public int ActiveKpis { get; set; }
    public int StaleKpis { get; set; }
    public double SystemHealthScore { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Extension methods for easier metrics recording
/// </summary>
public static class MetricsExtensions
{
    /// <summary>
    /// Record execution time using a using statement
    /// </summary>
    public static IDisposable TimeOperation(this MetricsService metrics, string operationType, string identifier)
    {
        return new OperationTimer(operationType, identifier);
    }

    private class OperationTimer : IDisposable
    {
        private readonly string _operationType;
        private readonly string _identifier;
        private readonly DateTime _startTime;

        public OperationTimer(string operationType, string identifier)
        {
            _operationType = operationType;
            _identifier = identifier;
            _startTime = DateTime.UtcNow;
        }

        public void Dispose()
        {
            var duration = DateTime.UtcNow - _startTime;
            // Could record to metrics here if needed
        }
    }
}
