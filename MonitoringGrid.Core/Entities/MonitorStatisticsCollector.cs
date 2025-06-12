using System.ComponentModel.DataAnnotations;

namespace MonitoringGrid.Core.Entities;

/// <summary>
/// Represents monitoring statistics collector configuration from ProgressPlayDBTest.stats.tbl_Monitor_StatisticsCollectors
/// </summary>
public class MonitorStatisticsCollector
{
    public long ID { get; set; }

    [Required]
    public long CollectorID { get; set; }

    [MaxLength(500)]
    public string? CollectorCode { get; set; }

    [MaxLength(500)]
    public string? CollectorDesc { get; set; }

    [Required]
    public int FrequencyMinutes { get; set; }

    public int? LastMinutes { get; set; }

    [MaxLength(50)]
    public string? StoreProcedure { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public DateTime? LastRun { get; set; }

    [MaxLength(500)]
    public string? LastRunResult { get; set; }

    // Navigation properties
    public virtual ICollection<MonitorStatistics> Statistics { get; set; } = new List<MonitorStatistics>();

    // Note: Indicators relationship removed since Indicator is in a different database (PopAI)
}
