using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MonitoringGrid.Models;

/// <summary>
/// Represents a Key Performance Indicator configuration
/// </summary>
[Table("KPIs", Schema = "monitoring")]
public class KPI
{
    [Key]
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
    [Column(TypeName = "decimal(5,2)")]
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
    [Column(TypeName = "decimal(18,2)")]
    public decimal? MinimumThreshold { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<KpiContact> KpiContacts { get; set; } = new List<KpiContact>();
    public virtual ICollection<AlertLog> AlertLogs { get; set; } = new List<AlertLog>();
    public virtual ICollection<HistoricalData> HistoricalData { get; set; } = new List<HistoricalData>();
}

/// <summary>
/// Represents a contact for notifications
/// </summary>
[Table("Contacts", Schema = "monitoring")]
public class Contact
{
    [Key]
    public int ContactId { get; set; }

    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? Email { get; set; }

    [MaxLength(50)]
    public string? Phone { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<KpiContact> KpiContacts { get; set; } = new List<KpiContact>();
}

/// <summary>
/// Many-to-many relationship between KPIs and Contacts
/// </summary>
[Table("KpiContacts", Schema = "monitoring")]
public class KpiContact
{
    public int KpiId { get; set; }
    public int ContactId { get; set; }

    // Navigation properties
    public virtual KPI KPI { get; set; } = null!;
    public virtual Contact Contact { get; set; } = null!;
}

/// <summary>
/// Represents an alert log entry
/// </summary>
[Table("AlertLogs", Schema = "monitoring")]
public class AlertLog
{
    [Key]
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

    [Column(TypeName = "decimal(18,2)")]
    public decimal? CurrentValue { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? HistoricalValue { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal? DeviationPercent { get; set; }

    public bool IsResolved { get; set; } = false;

    public DateTime? ResolvedTime { get; set; }

    [MaxLength(100)]
    public string? ResolvedBy { get; set; }

    // Navigation properties
    public virtual KPI KPI { get; set; } = null!;
}

/// <summary>
/// Stores historical data for trend analysis
/// </summary>
[Table("HistoricalData", Schema = "monitoring")]
public class HistoricalData
{
    [Key]
    public long HistoricalId { get; set; }

    public int KpiId { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "decimal(18,2)")]
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
}

/// <summary>
/// System configuration settings
/// </summary>
[Table("Config", Schema = "monitoring")]
public class Config
{
    [Key]
    [MaxLength(50)]
    public string ConfigKey { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string ConfigValue { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// System status tracking
/// </summary>
[Table("SystemStatus", Schema = "monitoring")]
public class SystemStatus
{
    [Key]
    public int StatusId { get; set; }

    [Required]
    [MaxLength(100)]
    public string ServiceName { get; set; } = string.Empty;

    public DateTime LastHeartbeat { get; set; } = DateTime.UtcNow;

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }

    public int ProcessedKpis { get; set; } = 0;

    public int AlertsSent { get; set; } = 0;
}

/// <summary>
/// Result of KPI execution
/// </summary>
public class KpiExecutionResult
{
    public string Key { get; set; } = string.Empty;
    public decimal CurrentValue { get; set; }
    public decimal HistoricalValue { get; set; }
    public decimal DeviationPercent { get; set; }
    public bool ShouldAlert { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Result of alert sending operation
/// </summary>
public class AlertResult
{
    public int EmailsSent { get; set; }
    public int SmsSent { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();
    public bool Success => Errors.Count == 0;
}
