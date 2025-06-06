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

    // Comprehensive audit fields
    [MaxLength(100)]
    public string? ExecutedBy { get; set; }

    [MaxLength(50)]
    public string? ExecutionMethod { get; set; } // 'Manual', 'Scheduled', 'API'

    public string? SqlCommand { get; set; }

    public string? SqlParameters { get; set; }

    public string? RawResponse { get; set; }

    public int? ExecutionTimeMs { get; set; }

    [MaxLength(500)]
    public string? ConnectionString { get; set; }

    [MaxLength(100)]
    public string? DatabaseName { get; set; }

    [MaxLength(100)]
    public string? ServerName { get; set; }

    public bool IsSuccessful { get; set; } = true;

    public string? ErrorMessage { get; set; }

    public decimal? DeviationPercent { get; set; }

    public decimal? HistoricalValue { get; set; }

    public bool ShouldAlert { get; set; } = false;

    public bool AlertSent { get; set; } = false;

    [MaxLength(100)]
    public string? SessionId { get; set; }

    [MaxLength(500)]
    public string? UserAgent { get; set; }

    [MaxLength(50)]
    public string? IpAddress { get; set; }

    public string? ExecutionContext { get; set; } // JSON with additional context

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
