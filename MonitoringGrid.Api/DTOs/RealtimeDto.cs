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

// Note: Indicator execution DTOs are now defined in IndicatorRealtimeDtos.cs
// This file contains legacy KPI DTOs and shared real-time DTOs

/// <summary>
/// DTO for countdown updates
/// </summary>
public class CountdownUpdateDto
{
    public long NextIndicatorID { get; set; }
    public string IndicatorName { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public int SecondsUntilDue { get; set; }
    public string ScheduledTime { get; set; } = string.Empty;
}

// Note: Next Indicator and Running Indicator DTOs are now defined in IndicatorRealtimeDtos.cs

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
