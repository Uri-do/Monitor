namespace MonitoringGrid.Core.Models;

/// <summary>
/// Result of alert sending operation
/// </summary>
public class AlertResult
{
    public int EmailsSent { get; set; }
    public int SmsSent { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Domain methods
    public bool Success => Errors.Count == 0;

    public int TotalRecipients => EmailsSent + SmsSent;

    public bool HasNotifications => EmailsSent > 0 || SmsSent > 0;

    public string GetSummary()
    {
        if (!Success)
            return $"Alert failed: {string.Join(", ", Errors)}";

        var parts = new List<string>();
        if (EmailsSent > 0) parts.Add($"{EmailsSent} email(s)");
        if (SmsSent > 0) parts.Add($"{SmsSent} SMS");

        return parts.Any() ? $"Alert sent to {string.Join(" and ", parts)}" : "No alerts sent";
    }
}
