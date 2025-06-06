namespace MonitoringGrid.Core.Models;

/// <summary>
/// Webhook test result model
/// </summary>
public class WebhookTestResult
{
    public bool IsSuccess { get; set; }
    public int StatusCode { get; set; }
    public string? Response { get; set; }
    public string? ErrorMessage { get; set; }
    public double ResponseTimeMs { get; set; }
    public DateTime TestTime { get; set; }
}

/// <summary>
/// Webhook configuration model for requests
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
    public List<string> TriggerSeverities { get; set; } = new();
}
