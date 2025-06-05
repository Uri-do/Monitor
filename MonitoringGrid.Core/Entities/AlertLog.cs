using System.ComponentModel.DataAnnotations;

namespace MonitoringGrid.Core.Entities;

/// <summary>
/// Represents an alert log entry
/// </summary>
public class AlertLog
{
    public long AlertId { get; set; }

    public int KpiId { get; set; }

    public DateTime TriggerTime { get; set; } = DateTime.UtcNow;

    [Required]
    [MaxLength(500)]
    public string Message { get; set; } = string.Empty;

    public string? Details { get; set; }

    /// <summary>
    /// How the alert was sent: 1=SMS, 2=Email, 3=Both
    /// </summary>
    public byte SentVia { get; set; }

    [Required]
    public string SentTo { get; set; } = string.Empty;

    public decimal? CurrentValue { get; set; }

    public decimal? HistoricalValue { get; set; }

    public decimal? DeviationPercent { get; set; }

    public bool IsResolved { get; set; } = false;

    public DateTime? ResolvedTime { get; set; }

    [MaxLength(100)]
    public string? ResolvedBy { get; set; }

    // Navigation properties
    public virtual KPI KPI { get; set; } = null!;

    // Domain methods
    public string GetSentViaName()
    {
        return SentVia switch
        {
            1 => "SMS",
            2 => "Email",
            3 => "SMS + Email",
            _ => "Unknown"
        };
    }

    public string GetSeverity()
    {
        return DeviationPercent switch
        {
            >= 50 => "Critical",
            >= 25 => "High",
            >= 10 => "Medium",
            _ => "Low"
        };
    }

    public TimeSpan? GetResolutionTime()
    {
        return ResolvedTime.HasValue ? ResolvedTime.Value - TriggerTime : null;
    }

    public void Resolve(string resolvedBy, string? notes = null)
    {
        IsResolved = true;
        ResolvedTime = DateTime.UtcNow;
        ResolvedBy = resolvedBy;

        if (!string.IsNullOrEmpty(notes))
        {
            Details = string.IsNullOrEmpty(Details) 
                ? $"Resolution: {notes}"
                : $"{Details}\n\nResolution: {notes}";
        }
    }

    public bool IsOverdue(int maxResolutionHours = 24)
    {
        if (IsResolved)
            return false;

        return DateTime.UtcNow - TriggerTime > TimeSpan.FromHours(maxResolutionHours);
    }
}
