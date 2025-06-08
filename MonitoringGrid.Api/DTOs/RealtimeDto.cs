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

// Note: WorkerServiceDto is already defined in KpiDtos.cs

/// <summary>
/// DTO for KPI execution started event
/// </summary>
public class KpiExecutionStartedDto
{
    public int KpiId { get; set; }
    public string Indicator { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public int? EstimatedDuration { get; set; }
}

/// <summary>
/// DTO for KPI execution progress event
/// </summary>
public class KpiExecutionProgressDto
{
    public int KpiId { get; set; }
    public string Indicator { get; set; } = string.Empty;
    public int Progress { get; set; } // 0-100
    public string CurrentStep { get; set; } = string.Empty;
    public int ElapsedTime { get; set; }
    public int? EstimatedTimeRemaining { get; set; }
}

/// <summary>
/// DTO for KPI execution completed event
/// </summary>
public class KpiExecutionCompletedDto
{
    public int KpiId { get; set; }
    public string Indicator { get; set; } = string.Empty;
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
    public int NextKpiId { get; set; }
    public string Indicator { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public int SecondsUntilDue { get; set; }
    public string ScheduledTime { get; set; } = string.Empty;
}

/// <summary>
/// DTO for next KPI schedule updates
/// </summary>
public class NextKpiScheduleUpdateDto
{
    public List<NextKpiDto> NextKpis { get; set; } = new();
}

/// <summary>
/// DTO for next KPI information
/// </summary>
public class NextKpiDto
{
    public int KpiId { get; set; }
    public string Indicator { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public string ScheduledTime { get; set; } = string.Empty;
    public int MinutesUntilDue { get; set; }
}

/// <summary>
/// DTO for running KPIs updates
/// </summary>
public class RunningKpisUpdateDto
{
    public List<RunningKpiDto> RunningKpis { get; set; } = new();
}

/// <summary>
/// DTO for running KPI information
/// </summary>
public class RunningKpiDto
{
    public int KpiId { get; set; }
    public string Indicator { get; set; } = string.Empty;
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
    public List<RunningKpiDto> RunningKpis { get; set; } = new();
    public CountdownUpdateDto? Countdown { get; set; }
    public NextKpiDto? NextKpiDue { get; set; }
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
    public int TotalKpisExecuted { get; set; }
    public int SuccessfulExecutions { get; set; }
    public int FailedExecutions { get; set; }
    public double AverageExecutionTime { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
}
