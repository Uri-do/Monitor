namespace MonitoringGrid.Api.DTOs.Hubs;

/// <summary>
/// DTO for alert notifications sent via SignalR
/// </summary>
public class AlertNotificationDto
{
    public long AlertId { get; set; }
    public long IndicatorId { get; set; }
    public string IndicatorName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public DateTime TriggerTime { get; set; }
    public decimal? CurrentValue { get; set; }
    public decimal? DeviationPercent { get; set; }
    public string Owner { get; set; } = string.Empty;
}

/// <summary>
/// DTO for indicator status updates sent via SignalR
/// </summary>
public class IndicatorStatusUpdateDto
{
    public long IndicatorId { get; set; }
    public long IndicatorID { get; set; } // Legacy property name for compatibility
    public string IndicatorName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsRunning { get; set; }
    public bool IsCurrentlyRunning { get; set; } // Legacy property name for compatibility
    public DateTime? LastRun { get; set; }
    public string? LastRunResult { get; set; }
    public DateTime? NextRun { get; set; }
    public string Owner { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public decimal? LastValue { get; set; }
    public bool IsSignificantChange { get; set; }
    public string? ChangeReason { get; set; }
}

/// <summary>
/// DTO for dashboard updates sent via SignalR
/// </summary>
public class DashboardUpdateDto
{
    public int TotalIndicators { get; set; }
    public int ActiveIndicators { get; set; }
    public int RunningIndicators { get; set; }
    public int DueIndicators { get; set; }
    public int AlertsToday { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// DTO for system status updates sent via SignalR
/// </summary>
public class SystemStatusDto
{
    public string ServiceName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime LastHeartbeat { get; set; }
    public string? ErrorMessage { get; set; }
    public int ProcessedIndicators { get; set; }
    public int AlertsSent { get; set; }
}

/// <summary>
/// DTO for worker status updates sent via SignalR
/// </summary>
public class WorkerStatusUpdateDto
{
    public string WorkerName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsRunning { get; set; }
    public DateTime? StartTime { get; set; }
    public TimeSpan? Uptime { get; set; }
    public int ProcessedIndicators { get; set; }
    public int SuccessfulExecutions { get; set; }
    public int FailedExecutions { get; set; }
    public DateTime LastActivity { get; set; }
}

/// <summary>
/// DTO for countdown updates sent via SignalR
/// </summary>
public class CountdownUpdateDto
{
    public List<NextIndicatorDto> NextIndicators { get; set; } = new();
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// DTO for next scheduled indicator
/// </summary>
public class NextIndicatorDto
{
    public long IndicatorId { get; set; }
    public string IndicatorName { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public DateTime ScheduledTime { get; set; }
    public int SecondsUntilDue { get; set; }
    public string Priority { get; set; } = string.Empty;
    public bool IsOverdue { get; set; }
}
