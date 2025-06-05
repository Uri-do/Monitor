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

/// <summary>
/// Alert escalation status
/// </summary>
public class AlertEscalation
{
    public int AlertEscalationId { get; set; }
    public int AlertId { get; set; }
    public int Level { get; set; }
    public DateTime ScheduledTime { get; set; }
    public DateTime? ExecutedTime { get; set; }
    public bool IsExecuted { get; set; }
    public bool IsCancelled { get; set; }
    public string? ExecutionResult { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Advanced alert configuration
/// </summary>
public class AdvancedAlertConfiguration
{
    public bool EnableEscalation { get; set; } = true;
    public bool EnableAutoResolution { get; set; } = true;
    public int AutoResolutionMinutes { get; set; } = 60;
    public bool EnableAlertSuppression { get; set; } = true;
    public int SuppressionMinutes { get; set; } = 30;
    public bool EnableBusinessHoursOnly { get; set; } = false;
    public TimeSpan BusinessHoursStart { get; set; } = new(9, 0, 0);
    public TimeSpan BusinessHoursEnd { get; set; } = new(17, 0, 0);
    public List<DayOfWeek> BusinessDays { get; set; } = new() 
    { 
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, 
        DayOfWeek.Thursday, DayOfWeek.Friday 
    };
    public bool EnableHolidaySupport { get; set; } = false;
    public List<DateTime> Holidays { get; set; } = new();
}

/// <summary>
/// Alert severity levels
/// </summary>
public enum AlertSeverity
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4,
    Emergency = 5
}

/// <summary>
/// Alert notification channels
/// </summary>
public enum NotificationChannel
{
    Email = 1,
    Sms = 2,
    Push = 3,
    Webhook = 4,
    Slack = 5,
    Teams = 6
}

/// <summary>
/// Alert acknowledgment
/// </summary>
public class AlertAcknowledgment
{
    public int AlertAcknowledgmentId { get; set; }
    public int AlertId { get; set; }
    public string AcknowledgedBy { get; set; } = string.Empty;
    public DateTime AcknowledgedAt { get; set; }
    public string? Notes { get; set; }
    public bool StopEscalation { get; set; } = true;
}

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

/// <summary>
/// Alert suppression rule
/// </summary>
public class AlertSuppressionRule
{
    public int SuppressionRuleId { get; set; }
    public int? KpiId { get; set; } // null for global rules
    public string? Owner { get; set; } // null for all owners
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Webhook configuration for external integrations
/// </summary>
public class WebhookConfiguration
{
    public int WebhookId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = "POST";
    public Dictionary<string, string> Headers { get; set; } = new();
    public string PayloadTemplate { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int TimeoutSeconds { get; set; } = 30;
    public int RetryCount { get; set; } = 3;
    public List<AlertSeverity> TriggerSeverities { get; set; } = new();
}

/// <summary>
/// KPI trend analysis result
/// </summary>
public class KpiTrendAnalysis
{
    public int KpiId { get; set; }
    public int AnalysisPeriodDays { get; set; }
    public int DataPoints { get; set; }
    public TrendDirection TrendDirection { get; set; }
    public double TrendStrength { get; set; }
    public double Slope { get; set; }
    public double RSquared { get; set; }
    public double Volatility { get; set; }
    public List<TrendChange> TrendChanges { get; set; } = new();
    public decimal LastValue { get; set; }
    public decimal AverageValue { get; set; }
    public decimal MinValue { get; set; }
    public decimal MaxValue { get; set; }
    public double StandardDeviation { get; set; }
    public DateTime AnalysisDate { get; set; }
    public string? Message { get; set; }
}

/// <summary>
/// Trend direction enumeration
/// </summary>
public enum TrendDirection
{
    Unknown = 0,
    Increasing = 1,
    Decreasing = 2,
    Stable = 3,
    Volatile = 4
}

/// <summary>
/// Trend change point
/// </summary>
public class TrendChange
{
    public DateTime Timestamp { get; set; }
    public TrendDirection FromDirection { get; set; }
    public TrendDirection ToDirection { get; set; }
    public double Significance { get; set; }
}

/// <summary>
/// KPI performance metrics
/// </summary>
public class KpiPerformanceMetrics
{
    public int KpiId { get; set; }
    public string Indicator { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public int ExecutionCount { get; set; }
    public int SuccessfulExecutions { get; set; }
    public int FailedExecutions { get; set; }
    public int AlertCount { get; set; }
    public double AverageExecutionTime { get; set; }
    public decimal AverageValue { get; set; }
    public decimal MinValue { get; set; }
    public decimal MaxValue { get; set; }
    public double Reliability { get; set; }
    public double AlertRate { get; set; }
    public DateTime LastExecution { get; set; }
    public string AnalysisPeriod { get; set; } = string.Empty;
}

/// <summary>
/// KPI prediction result
/// </summary>
public class KpiPrediction
{
    public int KpiId { get; set; }
    public DateTime PredictionDate { get; set; }
    public decimal? PredictedValue { get; set; }
    public double ConfidenceLevel { get; set; }
    public string PredictionMethod { get; set; } = string.Empty;
    public int DataPointsUsed { get; set; }
    public string? Message { get; set; }
}

/// <summary>
/// KPI correlation analysis
/// </summary>
public class KpiCorrelationAnalysis
{
    public int Kpi1Id { get; set; }
    public string Kpi1Indicator { get; set; } = string.Empty;
    public int Kpi2Id { get; set; }
    public string Kpi2Indicator { get; set; } = string.Empty;
    public double CorrelationCoefficient { get; set; }
    public string CorrelationStrength { get; set; } = string.Empty;
    public int DataPointsUsed { get; set; }
    public DateTime AnalysisDate { get; set; }
}

/// <summary>
/// KPI anomaly detection result
/// </summary>
public class KpiAnomalyDetection
{
    public int KpiId { get; set; }
    public int AnalysisPeriodDays { get; set; }
    public int DataPointsAnalyzed { get; set; }
    public int AnomaliesDetected { get; set; }
    public List<AnomalyPoint> Anomalies { get; set; } = new();
    public double AnomalyRate { get; set; }
    public DateTime AnalysisDate { get; set; }
    public string? Message { get; set; }
}

/// <summary>
/// Anomaly point
/// </summary>
public class AnomalyPoint
{
    public DateTime Timestamp { get; set; }
    public decimal Value { get; set; }
    public double ZScore { get; set; }
    public string Severity { get; set; } = string.Empty;
}

/// <summary>
/// KPI seasonality analysis
/// </summary>
public class KpiSeasonalityAnalysis
{
    public int KpiId { get; set; }
    public string Pattern { get; set; } = string.Empty; // Daily, Weekly, Monthly, Yearly
    public double Strength { get; set; }
    public List<SeasonalComponent> Components { get; set; } = new();
    public string? Message { get; set; }
}

/// <summary>
/// Seasonal component
/// </summary>
public class SeasonalComponent
{
    public string Period { get; set; } = string.Empty;
    public double Amplitude { get; set; }
    public double Phase { get; set; }
    public double Significance { get; set; }
}
