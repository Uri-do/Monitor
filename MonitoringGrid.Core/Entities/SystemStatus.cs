using System.ComponentModel.DataAnnotations;

namespace MonitoringGrid.Core.Entities;

/// <summary>
/// System status tracking
/// </summary>
public class SystemStatus
{
    public int StatusId { get; set; }

    [Required]
    [MaxLength(100)]
    public string ServiceName { get; set; } = string.Empty;

    public DateTime LastHeartbeat { get; set; } = DateTime.UtcNow;

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }

    public int ProcessedKpis { get; set; } = 0;

    public int AlertsSent { get; set; } = 0;

    // Domain methods
    public bool IsHealthy(int maxMinutesSinceHeartbeat = 5)
    {
        var timeSinceHeartbeat = DateTime.UtcNow - LastHeartbeat;
        return timeSinceHeartbeat.TotalMinutes <= maxMinutesSinceHeartbeat && 
               Status.Equals("Running", StringComparison.OrdinalIgnoreCase);
    }

    public TimeSpan TimeSinceLastHeartbeat()
    {
        return DateTime.UtcNow - LastHeartbeat;
    }

    public void UpdateHeartbeat(string status = "Running", string? errorMessage = null)
    {
        LastHeartbeat = DateTime.UtcNow;
        Status = status;
        ErrorMessage = errorMessage;
    }

    public void IncrementProcessedKpis()
    {
        ProcessedKpis++;
    }

    public void IncrementAlertsSent()
    {
        AlertsSent++;
    }
}
