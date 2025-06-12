namespace MonitoringGrid.Api.DTOs;

/// <summary>
/// DTO for Indicator execution started real-time event
/// </summary>
public class IndicatorExecutionStartedDto
{
    public long IndicatorID { get; set; }
    public string IndicatorName { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public int? EstimatedDuration { get; set; }
    public string ExecutionContext { get; set; } = string.Empty;
}

/// <summary>
/// DTO for Indicator execution completed real-time event
/// </summary>
public class IndicatorExecutionCompletedDto
{
    public long IndicatorId { get; set; }
    public string IndicatorName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public decimal? Value { get; set; }
    public int Duration { get; set; }
    public string CompletedAt { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public bool ThresholdBreached { get; set; }
    public string ExecutionContext { get; set; } = string.Empty;
}

/// <summary>
/// DTO for Indicator countdown update real-time event
/// </summary>
public class IndicatorCountdownUpdateDto
{
    public long NextIndicatorID { get; set; }
    public string IndicatorName { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public int SecondsUntilDue { get; set; }
    public string ScheduledTime { get; set; } = string.Empty;
}



/// <summary>
/// DTO for Indicator alert real-time event
/// </summary>
public class IndicatorAlertDto
{
    public long IndicatorID { get; set; }
    public string IndicatorName { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public string AlertType { get; set; } = string.Empty; // "Threshold", "Error", "Warning"
    public string Message { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty; // "high", "medium", "low"
    public decimal? CurrentValue { get; set; }
    public decimal? ThresholdValue { get; set; }
    public string? ThresholdComparison { get; set; }
    public DateTime TriggerTime { get; set; }
    public bool RequiresAction { get; set; }
}

/// <summary>
/// DTO for dashboard Indicator summary real-time event
/// </summary>
public class IndicatorDashboardUpdateDto
{
    public int TotalIndicators { get; set; }
    public int ActiveIndicators { get; set; }
    public int RunningIndicators { get; set; }
    public int DueIndicators { get; set; }
    public int AlertsToday { get; set; }
    public List<IndicatorExecutionSummaryDto> RecentExecutions { get; set; } = new();
    public List<IndicatorStatusSummaryDto> StatusSummary { get; set; } = new();
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// DTO for Indicator status summary in dashboard
/// </summary>
public class IndicatorStatusSummaryDto
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
    public string Color { get; set; } = string.Empty; // For UI styling
}

/// <summary>
/// DTO for running Indicators update real-time event
/// </summary>
public class RunningIndicatorsUpdateDto
{
    public List<RunningIndicatorDto> RunningIndicators { get; set; } = new();
    public int TotalRunning { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// DTO for a single running Indicator
/// </summary>
public class RunningIndicatorDto
{
    public long IndicatorID { get; set; }
    public string IndicatorName { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public int DurationSeconds { get; set; }
    public string ExecutionContext { get; set; } = string.Empty;
    public int Progress { get; set; } = 0; // 0-100 percentage
}

/// <summary>
/// DTO for next scheduled Indicators update real-time event
/// </summary>
public class NextIndicatorsScheduleUpdateDto
{
    public List<NextIndicatorDto> NextIndicators { get; set; } = new();
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// DTO for next scheduled Indicator
/// </summary>
public class NextIndicatorDto
{
    public long IndicatorID { get; set; }
    public string IndicatorName { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public DateTime ScheduledTime { get; set; }
    public int SecondsUntilDue { get; set; }
    public string Priority { get; set; } = string.Empty;
    public bool IsOverdue { get; set; }
}

/// <summary>
/// DTO for Indicator execution progress real-time event
/// </summary>
public class IndicatorExecutionProgressDto
{
    public long IndicatorId { get; set; }
    public string IndicatorName { get; set; } = string.Empty;
    public int Progress { get; set; } // 0-100 percentage
    public string CurrentStep { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public int ElapsedSeconds { get; set; }
    public int? EstimatedRemainingSeconds { get; set; }
}

/// <summary>
/// DTO for Indicator threshold breach real-time event
/// </summary>
public class IndicatorThresholdBreachDto
{
    public long IndicatorId { get; set; }
    public string IndicatorName { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public decimal CurrentValue { get; set; }
    public decimal ThresholdValue { get; set; }
    public string ThresholdComparison { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public DateTime BreachTime { get; set; }
    public decimal? HistoricalAverage { get; set; }
    public string ThresholdType { get; set; } = string.Empty;
}

/// <summary>
/// DTO for Indicator system health real-time event
/// </summary>
public class IndicatorSystemHealthDto
{
    public bool IsHealthy { get; set; }
    public int TotalIndicators { get; set; }
    public int HealthyIndicators { get; set; }
    public int ErrorIndicators { get; set; }
    public int WarningIndicators { get; set; }
    public DateTime LastHealthCheck { get; set; }
    public List<string> Issues { get; set; } = new();
    public double SystemLoad { get; set; }
    public bool DatabaseConnected { get; set; }
    public bool ProgressPlayDbConnected { get; set; }
}
