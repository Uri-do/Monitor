using System.ComponentModel.DataAnnotations;

namespace MonitoringGrid.Worker.Configuration;

/// <summary>
/// Configuration settings for the MonitoringGrid Worker Service
/// </summary>
public class WorkerConfiguration
{
    public const string SectionName = "Worker";

    /// <summary>
    /// KPI monitoring configuration
    /// </summary>
    public KpiMonitoringConfiguration KpiMonitoring { get; set; } = new();

    /// <summary>
    /// Scheduled task configuration
    /// </summary>
    public ScheduledTaskConfiguration ScheduledTasks { get; set; } = new();

    /// <summary>
    /// Health check configuration
    /// </summary>
    public HealthCheckConfiguration HealthChecks { get; set; } = new();

    /// <summary>
    /// Alert processing configuration
    /// </summary>
    public AlertProcessingConfiguration AlertProcessing { get; set; } = new();

    /// <summary>
    /// Logging configuration
    /// </summary>
    public LoggingConfiguration Logging { get; set; } = new();

    /// <summary>
    /// API base URL for SignalR connection
    /// </summary>
    public string? ApiBaseUrl { get; set; } = "https://localhost:57652";
}

/// <summary>
/// KPI monitoring specific configuration
/// </summary>
public class KpiMonitoringConfiguration
{
    /// <summary>
    /// Interval between KPI monitoring cycles in seconds
    /// </summary>
    [Range(10, 3600)]
    public int IntervalSeconds { get; set; } = 60;

    /// <summary>
    /// Maximum number of KPIs to process in parallel
    /// </summary>
    [Range(1, 50)]
    public int MaxParallelKpis { get; set; } = 5;

    /// <summary>
    /// Timeout for individual KPI execution in seconds
    /// </summary>
    [Range(30, 1800)]
    public int ExecutionTimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// Whether to process only active KPIs
    /// </summary>
    public bool ProcessOnlyActiveKpis { get; set; } = true;

    /// <summary>
    /// Whether to skip KPIs that are currently running
    /// </summary>
    public bool SkipRunningKpis { get; set; } = true;
}

/// <summary>
/// Scheduled task configuration
/// </summary>
public class ScheduledTaskConfiguration
{
    /// <summary>
    /// Whether scheduled tasks are enabled
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Cron expression for cleanup tasks (default: daily at 2 AM)
    /// </summary>
    public string CleanupCronExpression { get; set; } = "0 0 2 * * ?";

    /// <summary>
    /// Cron expression for maintenance tasks (default: weekly on Sunday at 3 AM)
    /// </summary>
    public string MaintenanceCronExpression { get; set; } = "0 0 3 ? * SUN";

    /// <summary>
    /// Number of days to retain historical data
    /// </summary>
    [Range(1, 365)]
    public int HistoricalDataRetentionDays { get; set; } = 90;

    /// <summary>
    /// Number of days to retain log data
    /// </summary>
    [Range(1, 365)]
    public int LogRetentionDays { get; set; } = 30;
}

/// <summary>
/// Health check configuration
/// </summary>
public class HealthCheckConfiguration
{
    /// <summary>
    /// Interval between health checks in seconds
    /// </summary>
    [Range(30, 3600)]
    public int IntervalSeconds { get; set; } = 300;

    /// <summary>
    /// Whether to perform database health checks
    /// </summary>
    public bool CheckDatabase { get; set; } = true;

    /// <summary>
    /// Whether to perform KPI execution health checks
    /// </summary>
    public bool CheckKpiExecution { get; set; } = true;

    /// <summary>
    /// Whether to perform alert processing health checks
    /// </summary>
    public bool CheckAlertProcessing { get; set; } = true;

    /// <summary>
    /// Timeout for health checks in seconds
    /// </summary>
    [Range(5, 300)]
    public int TimeoutSeconds { get; set; } = 30;
}

/// <summary>
/// Alert processing configuration
/// </summary>
public class AlertProcessingConfiguration
{
    /// <summary>
    /// Interval between alert processing cycles in seconds
    /// </summary>
    [Range(10, 3600)]
    public int IntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum number of alerts to process in a single batch
    /// </summary>
    [Range(1, 1000)]
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Whether to enable alert escalation
    /// </summary>
    public bool EnableEscalation { get; set; } = true;

    /// <summary>
    /// Alert escalation timeout in minutes
    /// </summary>
    [Range(5, 1440)]
    public int EscalationTimeoutMinutes { get; set; } = 60;

    /// <summary>
    /// Whether to enable automatic alert resolution
    /// </summary>
    public bool EnableAutoResolution { get; set; } = true;

    /// <summary>
    /// Auto resolution timeout in minutes
    /// </summary>
    [Range(5, 1440)]
    public int AutoResolutionTimeoutMinutes { get; set; } = 120;
}

/// <summary>
/// Logging configuration
/// </summary>
public class LoggingConfiguration
{
    /// <summary>
    /// Minimum log level
    /// </summary>
    public string MinimumLevel { get; set; } = "Information";

    /// <summary>
    /// Whether to enable structured logging
    /// </summary>
    public bool EnableStructuredLogging { get; set; } = true;

    /// <summary>
    /// Whether to enable performance logging
    /// </summary>
    public bool EnablePerformanceLogging { get; set; } = true;

    /// <summary>
    /// Whether to log to console
    /// </summary>
    public bool LogToConsole { get; set; } = true;

    /// <summary>
    /// Whether to log to event log (Windows only)
    /// </summary>
    public bool LogToEventLog { get; set; } = false;

    /// <summary>
    /// Log file path (optional)
    /// </summary>
    public string? LogFilePath { get; set; }
}
