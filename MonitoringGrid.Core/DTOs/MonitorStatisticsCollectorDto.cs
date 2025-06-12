namespace MonitoringGrid.Core.DTOs;

/// <summary>
/// Data Transfer Object for MonitorStatisticsCollector
/// </summary>
public class MonitorStatisticsCollectorDto
{
    public long ID { get; set; }
    public long CollectorID { get; set; }
    public string? CollectorCode { get; set; }
    public string? CollectorDesc { get; set; }
    public int FrequencyMinutes { get; set; }
    public int? LastMinutes { get; set; }
    public string? StoreProcedure { get; set; }
    public bool? IsActive { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public DateTime? LastRun { get; set; }
    public string? LastRunResult { get; set; }
    
    // Additional computed properties
    public string DisplayName => !string.IsNullOrEmpty(CollectorDesc) ? CollectorDesc : CollectorCode ?? $"Collector {CollectorID}";
    public string FrequencyDisplay => $"Every {FrequencyMinutes} minutes";
    public string LastRunDisplay => LastRun?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Never";
    public bool IsActiveStatus => IsActive ?? false;
    public string StatusDisplay => IsActiveStatus ? "Active" : "Inactive";
    
    // Statistics summary
    public int StatisticsCount { get; set; }
    public List<string> ItemNames { get; set; } = new List<string>();
}
