using MonitoringGrid.Core.Enums;

namespace MonitoringGrid.Core.Models;

/// <summary>
/// Alert notification DTO for integration services
/// </summary>
public class AlertNotificationDto
{
    public int AlertId { get; set; }
    public int KpiId { get; set; }
    public string Indicator { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Priority { get; set; } = "Normal";
    public decimal CurrentValue { get; set; }
    public decimal HistoricalValue { get; set; }
    public decimal? ThresholdValue { get; set; }
    public decimal? Deviation { get; set; }
    public DateTime TriggerTime { get; set; }
    public string? AlertType { get; set; }
    public List<string> NotifiedContacts { get; set; } = new();
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}

/// <summary>
/// Notification request model
/// </summary>
public class NotificationRequest
{
    public string UserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
    public List<NotificationChannel> PreferredChannels { get; set; } = new();
    public Dictionary<string, object> Data { get; set; } = new();
}

/// <summary>
/// Notification result model
/// </summary>
public class NotificationResult
{
    public bool IsSuccess { get; set; }
    public List<string> DeliveredChannels { get; set; } = new();
    public List<string> FailedChannels { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

// Enums moved to MonitoringGrid.Core.Enums.CoreEnums
