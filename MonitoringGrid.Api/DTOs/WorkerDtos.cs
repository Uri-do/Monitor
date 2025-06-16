using System.ComponentModel.DataAnnotations;

namespace MonitoringGrid.Api.DTOs;

/// <summary>
/// Worker service status
/// </summary>
public class WorkerStatusDto
{
    public bool IsRunning { get; set; }
    public string Mode { get; set; } = string.Empty; // "Manual", "Integrated", or "External"
    public int? ProcessId { get; set; }
    public DateTime? StartTime { get; set; }

    private TimeSpan? _uptime;
    public TimeSpan? Uptime
    {
        get => _uptime ?? (StartTime.HasValue ? DateTime.UtcNow - StartTime.Value.ToUniversalTime() : null);
        set => _uptime = value;
    }

    public string? UptimeFormatted { get; set; } // Human-readable uptime (e.g., "01.02:30:45")
    public int UptimeSeconds { get; set; } // Total uptime in seconds
    public DateTime? LastHeartbeat { get; set; }
    public string? CurrentActivity { get; set; }
    public DateTime? LastActivityTime { get; set; }

    // Performance metrics
    public int TotalIndicatorsProcessed { get; set; }
    public int SuccessfulExecutions { get; set; }
    public int FailedExecutions { get; set; }
    public double SuccessRate { get; set; } // Percentage (0-100)

    public List<WorkerServiceDto> Services { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Individual worker service status
/// </summary>
public class WorkerServiceDto
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // "Running", "Stopped", "Error", etc.
    public DateTime? LastActivity { get; set; }
    public string? CurrentActivity { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Description { get; set; }

    // Service-specific metrics
    public int ProcessedCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
}

/// <summary>
/// Worker action result
/// </summary>
public class WorkerActionResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? ProcessId { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Worker logs
/// </summary>
public class WorkerLogsDto
{
    public List<string> Lines { get; set; } = new();
    public int TotalLines { get; set; }
    public DateTime Timestamp { get; set; }
}


