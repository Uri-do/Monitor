using System.ComponentModel.DataAnnotations;

namespace MonitoringGrid.Core.Entities;

/// <summary>
/// Represents monitoring statistics data from ProgressPlayDBTest.stats.tbl_Monitor_Statistics
/// </summary>
public class MonitorStatistics
{
    [Required]
    public DateTime Day { get; set; }

    [Required]
    public byte Hour { get; set; }

    [Required]
    public long CollectorID { get; set; }

    [MaxLength(50)]
    public string? ItemName { get; set; }

    public decimal? Total { get; set; }

    public decimal? Marked { get; set; }

    public decimal? MarkedPercent { get; set; }

    public DateTime? UpdatedDate { get; set; }

    // Navigation property
    public virtual MonitorStatisticsCollector? Collector { get; set; }
}
