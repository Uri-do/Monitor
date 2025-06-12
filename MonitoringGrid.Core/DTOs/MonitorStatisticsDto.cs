namespace MonitoringGrid.Core.DTOs;

/// <summary>
/// Data Transfer Object for MonitorStatistics
/// </summary>
public class MonitorStatisticsDto
{
    public DateTime Day { get; set; }
    public byte Hour { get; set; }
    public long CollectorID { get; set; }
    public string? ItemName { get; set; }
    public decimal? Total { get; set; }
    public decimal? Marked { get; set; }
    public decimal? MarkedPercent { get; set; }
    public DateTime? UpdatedDate { get; set; }
    
    // Additional computed properties
    public string DisplayTime => $"{Day:yyyy-MM-dd} {Hour:00}:00";
    public string CollectorName { get; set; } = string.Empty;
}
