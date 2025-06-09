using MonitoringGrid.Core.Enums;

namespace MonitoringGrid.Core.Models;

/// <summary>
/// Escalation rule configuration
/// </summary>
public class EscalationRule
{
    public int EscalationRuleId { get; set; }
    public int KpiId { get; set; }
    public int Level { get; set; } // 1, 2, 3 (escalation levels)
    public int DelayMinutes { get; set; } // Time before escalating
    public List<int> ContactIds { get; set; } = new();
    public bool SendSms { get; set; }
    public bool SendEmail { get; set; }
    public string? CustomMessage { get; set; }
    public bool IsActive { get; set; } = true;
}

// AlertEscalation moved to Entities folder

/// <summary>
/// Advanced alert configuration
/// </summary>
public class AdvancedAlertConfiguration
{
    /// <summary>
    /// Master switch to enable/disable all enhanced alert features
    /// </summary>
    public bool EnableEnhancedFeatures { get; set; } = false;

    public bool EnableEscalation { get; set; } = true;
    public bool EnableAutoResolution { get; set; } = true;
    public int AutoResolutionMinutes { get; set; } = 60;
    public bool EnableAlertSuppression { get; set; } = true;
    public int SuppressionMinutes { get; set; } = 30;
    public bool EnableBusinessHoursOnly { get; set; } = false;
    public bool EnableHolidaySupport { get; set; } = false;
    public TimeSpan BusinessHoursStart { get; set; } = new(9, 0, 0);
    public TimeSpan BusinessHoursEnd { get; set; } = new(17, 0, 0);
    public List<DayOfWeek> BusinessDays { get; set; } = new()
    {
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
        DayOfWeek.Thursday, DayOfWeek.Friday
    };
    public List<Holiday> Holidays { get; set; } = new();
}

/// <summary>
/// Represents a holiday for business hours calculation
/// </summary>
public class Holiday
{
    public DateTime Date { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsRecurring { get; set; } = false;
}

// Enums moved to MonitoringGrid.Core.Enums.CoreEnums

// AlertAcknowledgment moved to Entities folder

/// <summary>
/// Notification template
/// </summary>
public class NotificationTemplate
{
    public int TemplateId { get; set; }
    public string Name { get; set; } = string.Empty;
    public NotificationChannel Channel { get; set; }
    public AlertSeverity Severity { get; set; }
    public string SubjectTemplate { get; set; } = string.Empty;
    public string BodyTemplate { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// Alert metrics for analytics
/// </summary>
public class AlertMetrics
{
    public DateTime Date { get; set; }
    public int TotalAlerts { get; set; }
    public int CriticalAlerts { get; set; }
    public int ResolvedAlerts { get; set; }
    public int EscalatedAlerts { get; set; }
    public int AcknowledgedAlerts { get; set; }
    public double AverageResolutionTimeMinutes { get; set; }
    public double AverageAcknowledgmentTimeMinutes { get; set; }
    public Dictionary<string, int> AlertsByKpi { get; set; } = new();
    public Dictionary<string, int> AlertsBySeverity { get; set; } = new();
}

/// <summary>
/// Alert trend analysis
/// </summary>
public class AlertTrendAnalysis
{
    public string KpiIndicator { get; set; } = string.Empty;
    public int AlertCount { get; set; }
    public double TrendPercentage { get; set; }
    public bool IsIncreasing { get; set; }
    public DateTime AnalysisDate { get; set; }
    public List<DateTime> AlertDates { get; set; } = new();
    public string TrendDescription { get; set; } = string.Empty;
}

// AlertSuppressionRule moved to Entities folder

// WebhookConfiguration moved to WebhookModels.cs
