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

/// <summary>
/// Data Transfer Object for Collector Statistics used in indicator execution
/// </summary>
public class CollectorStatisticDto
{
    public long CollectorID { get; set; }
    public string CollectorName { get; set; } = string.Empty;
    public string? ItemName { get; set; }
    public DateTime Timestamp { get; set; }
    public decimal? Value { get; set; }
    public decimal? Total { get; set; }
    public decimal? Marked { get; set; }
    public decimal? MarkedPercent { get; set; }
    public string? Unit { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}
