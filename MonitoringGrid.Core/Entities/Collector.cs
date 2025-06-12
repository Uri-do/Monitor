using MonitoringGrid.Core.Common;

namespace MonitoringGrid.Core.Entities;

/// <summary>
/// Represents a collector from ProgressPlayDB.stats.tbl_Monitor_StatisticsCollectors
/// This is a read-only entity for integration purposes
/// </summary>
public class Collector
{
    public long CollectorId { get; set; }
    public string CollectorCode { get; set; } = string.Empty;
    public string CollectorDesc { get; set; } = string.Empty;
    public int FrequencyMinutes { get; set; }
    public int LastMinutes { get; set; }
    public string StoreProcedure { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime UpdatedDate { get; set; }
    public DateTime? LastRun { get; set; }
    public string? LastRunResult { get; set; }

    // Navigation properties
    public virtual ICollection<CollectorStatistic> Statistics { get; set; } = new List<CollectorStatistic>();
}

/// <summary>
/// Represents statistics data from ProgressPlayDB.stats.tbl_Monitor_Statistics
/// This is a read-only entity for integration purposes
/// </summary>
public class CollectorStatistic
{
    public long StatisticId { get; set; }
    public long CollectorId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public int Total { get; set; }
    public int Marked { get; set; }
    public decimal MarkedPercent { get; set; }
    public DateTime Day { get; set; }
    public int Hour { get; set; }
    public DateTime CreatedDate { get; set; }

    // Navigation properties
    public virtual Collector? Collector { get; set; }
}

/// <summary>
/// DTO for collector selection in UI
/// </summary>
public class CollectorDto
{
    public long CollectorId { get; set; }
    public string CollectorCode { get; set; } = string.Empty;
    public string CollectorDesc { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public List<string> AvailableItems { get; set; } = new();
}

/// <summary>
/// DTO for collector statistics
/// </summary>
public class CollectorStatisticDto
{
    public string ItemName { get; set; } = string.Empty;
    public int Total { get; set; }
    public int Marked { get; set; }
    public decimal MarkedPercent { get; set; }
    public DateTime Day { get; set; }
    public int Hour { get; set; }
}
