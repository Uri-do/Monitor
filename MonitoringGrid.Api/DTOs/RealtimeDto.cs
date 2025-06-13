using System.ComponentModel.DataAnnotations;

namespace MonitoringGrid.Api.DTOs;

/// <summary>
/// DTO for worker status updates
/// </summary>
public class WorkerStatusUpdateDto
{
    public bool IsRunning { get; set; }
    public string Mode { get; set; } = string.Empty;
    public int? ProcessId { get; set; }
    public List<WorkerServiceDto> Services { get; set; } = new();
    public string LastHeartbeat { get; set; } = string.Empty;
    public string Uptime { get; set; } = string.Empty;
}

// Note: WorkerServiceDto is already defined in IndicatorDtos.cs

/// <summary>
/// DTO for Indicator execution started event
/// </summary>
public class IndicatorExecutionStartedDto
{
    public long IndicatorId { get; set; }
    public string IndicatorName { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public int? EstimatedDuration { get; set; }
}

/// <summary>
/// DTO for Indicator execution progress event
/// </summary>
public class IndicatorExecutionProgressDto
{
    public long IndicatorId { get; set; }
    public string IndicatorName { get; set; } = string.Empty;
    public int Progress { get; set; } // 0-100
    public string CurrentStep { get; set; } = string.Empty;
    public int ElapsedTime { get; set; }
    public int? EstimatedTimeRemaining { get; set; }
}

/// <summary>
/// DTO for Indicator execution completed event
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
}

/// <summary>
/// DTO for countdown updates
/// </summary>
public class CountdownUpdateDto
{
    public long NextIndicatorId { get; set; }
    public string IndicatorName { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public int SecondsUntilDue { get; set; }
    public string ScheduledTime { get; set; } = string.Empty;
}

/// <summary>
/// DTO for next Indicator schedule updates
/// </summary>
public class NextIndicatorScheduleUpdateDto
{
    public List<NextIndicatorDto> NextIndicators { get; set; } = new();
}

/// <summary>
/// DTO for next Indicator information
/// </summary>
public class NextIndicatorDto
{
    public long IndicatorId { get; set; }
    public string IndicatorName { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public string ScheduledTime { get; set; } = string.Empty;
    public int MinutesUntilDue { get; set; }
}

/// <summary>
/// DTO for running Indicators updates
/// </summary>
public class RunningIndicatorsUpdateDto
{
    public List<RunningIndicatorDto> RunningIndicators { get; set; } = new();
}

/// <summary>
/// DTO for running Indicator information
/// </summary>
public class RunningIndicatorDto
{
    public long IndicatorId { get; set; }
    public string IndicatorName { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public int? Progress { get; set; }
    public string? EstimatedCompletion { get; set; }
}

/// <summary>
/// DTO for real-time dashboard state
/// </summary>
public class RealtimeDashboardStateDto
{
    public WorkerStatusUpdateDto? WorkerStatus { get; set; }
    public List<RunningIndicatorDto> RunningIndicators { get; set; } = new();
    public CountdownUpdateDto? Countdown { get; set; }
    public NextIndicatorDto? NextIndicatorDue { get; set; }
    public DateTime LastUpdate { get; set; }
    public bool IsConnected { get; set; }
}

/// <summary>
/// DTO for system metrics
/// </summary>
public class SystemMetricsDto
{
    public long MemoryUsageMB { get; set; }
    public double CpuUsagePercent { get; set; }
    public int ThreadCount { get; set; }
    public int ActiveConnections { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// DTO for performance metrics
/// </summary>
public class PerformanceMetricsDto
{
    public int TotalIndicatorsExecuted { get; set; }
    public int SuccessfulExecutions { get; set; }
    public int FailedExecutions { get; set; }
    public double AverageExecutionTime { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
}
