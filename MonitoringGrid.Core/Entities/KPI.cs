using System.ComponentModel.DataAnnotations;
using MonitoringGrid.Core.Common;
using MonitoringGrid.Core.Events;
using MonitoringGrid.Core.Exceptions;

namespace MonitoringGrid.Core.Entities;

/// <summary>
/// Represents a Key Performance Indicator configuration (Aggregate Root)
/// </summary>
public class KPI : AggregateRoot
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
    /// Time window in minutes for data analysis (how far back to look for data)
    /// </summary>
    public int LastMinutes { get; set; } = 1440; // Default to 24 hours

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
        MarkAsModified();
    }

    /// <summary>
    /// Executes the KPI and raises appropriate domain events
    /// </summary>
    public void MarkAsExecuted(bool wasSuccessful, decimal? currentValue = null,
        decimal? historicalValue = null, string? errorMessage = null)
    {
        UpdateLastRun();

        // Raise domain event
        var executedEvent = new KpiExecutedEvent(
            KpiId, Indicator, Owner, wasSuccessful,
            currentValue, historicalValue, errorMessage);

        AddDomainEvent(executedEvent);

        // If there's a threshold breach, raise additional event
        if (wasSuccessful && currentValue.HasValue && historicalValue.HasValue)
        {
            var deviation = CalculateDeviation(currentValue.Value, historicalValue.Value);
            if (deviation > Deviation)
            {
                var severity = DetermineSeverity(deviation);
                var thresholdEvent = new KpiThresholdBreachedEvent(
                    KpiId, Indicator, currentValue.Value, historicalValue.Value, deviation, severity);

                AddDomainEvent(thresholdEvent);
            }
        }
    }

    /// <summary>
    /// Deactivates the KPI
    /// </summary>
    public void Deactivate(string reason, string deactivatedBy)
    {
        if (!IsActive)
            throw new BusinessRuleViolationException("KPI Deactivation", "KPI is already inactive");

        IsActive = false;
        ModifiedDate = DateTime.UtcNow;
        MarkAsModified();

        // Raise domain event
        var deactivatedEvent = new KpiDeactivatedEvent(KpiId, Indicator, reason, deactivatedBy);
        AddDomainEvent(deactivatedEvent);
    }

    /// <summary>
    /// Updates KPI configuration
    /// </summary>
    public void UpdateConfiguration(string updatedBy)
    {
        ModifiedDate = DateTime.UtcNow;
        MarkAsModified();

        // Raise domain event
        var updatedEvent = new KpiUpdatedEvent(this, updatedBy);
        AddDomainEvent(updatedEvent);
    }

    /// <summary>
    /// Validates the KPI configuration
    /// </summary>
    public void ValidateConfiguration()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Indicator))
            errors.Add("Indicator is required");

        if (string.IsNullOrWhiteSpace(Owner))
            errors.Add("Owner is required");

        if (Priority < 1 || Priority > 2)
            errors.Add("Priority must be 1 (SMS + Email) or 2 (Email Only)");

        if (Frequency <= 0)
            errors.Add("Frequency must be greater than 0");

        if (Deviation < 0 || Deviation > 100)
            errors.Add("Deviation must be between 0 and 100");

        if (string.IsNullOrWhiteSpace(SpName))
            errors.Add("Stored procedure name is required");

        if (errors.Any())
            throw new KpiValidationException(Indicator, errors);
    }

    private decimal CalculateDeviation(decimal current, decimal historical)
    {
        if (historical == 0)
            return 0;

        return Math.Abs((current - historical) / historical) * 100;
    }

    private string DetermineSeverity(decimal deviation)
    {
        return deviation switch
        {
            >= 75 => "Critical",
            >= 50 => "High",
            >= 25 => "Medium",
            >= 10 => "Low",
            _ => "Minimal"
        };
    }
}
