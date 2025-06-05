using System.ComponentModel.DataAnnotations;

namespace MonitoringGrid.Core.Entities;

/// <summary>
/// Represents a Key Performance Indicator configuration
/// </summary>
public class KPI
{
    public int KpiId { get; set; }

    [Required]
    [MaxLength(255)]
    public string Indicator { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Owner { get; set; } = string.Empty;

    /// <summary>
    /// Priority level: 1 = SMS, 2 = Email
    /// </summary>
    public byte Priority { get; set; }

    /// <summary>
    /// Frequency in minutes
    /// </summary>
    public int Frequency { get; set; }

    /// <summary>
    /// Acceptable deviation percentage
    /// </summary>
    public decimal Deviation { get; set; }

    [Required]
    [MaxLength(255)]
    public string SpName { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string SubjectTemplate { get; set; } = string.Empty;

    [Required]
    public string DescriptionTemplate { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime? LastRun { get; set; }

    /// <summary>
    /// Cooldown period in minutes to prevent alert flooding
    /// </summary>
    public int CooldownMinutes { get; set; } = 30;

    /// <summary>
    /// Minimum threshold for absolute value checking
    /// </summary>
    public decimal? MinimumThreshold { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<KpiContact> KpiContacts { get; set; } = new List<KpiContact>();
    public virtual ICollection<AlertLog> AlertLogs { get; set; } = new List<AlertLog>();
    public virtual ICollection<HistoricalData> HistoricalData { get; set; } = new List<HistoricalData>();

    // Domain methods
    public bool IsDue()
    {
        if (!IsActive || !LastRun.HasValue)
            return true;

        var nextRun = LastRun.Value.AddMinutes(Frequency);
        return DateTime.UtcNow >= nextRun;
    }

    public DateTime? GetNextRunTime()
    {
        return LastRun?.AddMinutes(Frequency);
    }

    public bool IsInCooldown()
    {
        if (!LastRun.HasValue)
            return false;

        var cooldownEnd = LastRun.Value.AddMinutes(CooldownMinutes);
        return DateTime.UtcNow < cooldownEnd;
    }

    public string GetPriorityName()
    {
        return Priority switch
        {
            1 => "SMS + Email",
            2 => "Email Only",
            _ => "Unknown"
        };
    }

    public void UpdateLastRun()
    {
        LastRun = DateTime.UtcNow;
        ModifiedDate = DateTime.UtcNow;
    }
}
