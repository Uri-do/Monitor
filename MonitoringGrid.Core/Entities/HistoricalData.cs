using System.ComponentModel.DataAnnotations;

namespace MonitoringGrid.Core.Entities;

/// <summary>
/// Stores historical data for trend analysis
/// </summary>
public class HistoricalData
{
    public long HistoricalId { get; set; }

    public int KpiId { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public decimal Value { get; set; }

    /// <summary>
    /// Period in minutes
    /// </summary>
    public int Period { get; set; }

    [Required]
    [MaxLength(255)]
    public string MetricKey { get; set; } = string.Empty;

    // Navigation properties
    public virtual KPI KPI { get; set; } = null!;

    // Domain methods
    public bool IsRecent(int minutes = 60)
    {
        return DateTime.UtcNow - Timestamp <= TimeSpan.FromMinutes(minutes);
    }

    public bool IsFromSamePeriod(HistoricalData other)
    {
        return Period == other.Period && MetricKey == other.MetricKey;
    }
}
