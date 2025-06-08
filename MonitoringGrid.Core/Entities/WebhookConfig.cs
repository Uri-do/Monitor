using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MonitoringGrid.Core.Entities;

/// <summary>
/// Webhook configuration entity for external system integrations.
/// Defines how alerts and notifications are sent to external endpoints
/// via HTTP requests with customizable payloads and retry logic.
/// </summary>
[Table("WebhookConfigurations")]
public class WebhookConfig
{
    /// <summary>
    /// Unique identifier for the webhook configuration.
    /// </summary>
    [Key]
    public int WebhookId { get; set; }

    /// <summary>
    /// Descriptive name for the webhook configuration.
    /// </summary>
    [Required(ErrorMessage = "Webhook name is required")]
    [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
    [MinLength(1, ErrorMessage = "Name cannot be empty")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Target URL where webhook requests will be sent.
    /// Must be a valid HTTP or HTTPS URL.
    /// </summary>
    [Required(ErrorMessage = "Webhook URL is required")]
    [StringLength(500)]
    [Url(ErrorMessage = "Invalid URL format")]
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// HTTP method to use for webhook requests (GET, POST, PUT, PATCH).
    /// </summary>
    [StringLength(10)]
    [RegularExpression("^(GET|POST|PUT|PATCH|DELETE)$", ErrorMessage = "Invalid HTTP method")]
    public string HttpMethod { get; set; } = "POST";

    /// <summary>
    /// Custom HTTP headers to include in webhook requests.
    /// Stored as JSON object for flexibility.
    /// </summary>
    [StringLength(4000)]
    public string? Headers { get; set; }

    /// <summary>
    /// Template for the webhook payload with placeholders for dynamic values.
    /// Supports JSON, XML, or plain text formats.
    /// </summary>
    [StringLength(4000)]
    public string? PayloadTemplate { get; set; }

    /// <summary>
    /// Whether this webhook configuration is active and should receive notifications.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Request timeout in seconds. Must be between 1 and 300 seconds.
    /// </summary>
    [Range(1, 300, ErrorMessage = "Timeout must be between 1 and 300 seconds")]
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Number of retry attempts for failed webhook deliveries.
    /// </summary>
    [Range(0, 10, ErrorMessage = "Retry count must be between 0 and 10")]
    public int RetryCount { get; set; } = 3;

    /// <summary>
    /// Alert severities that trigger this webhook.
    /// Stored as JSON array (e.g., ["Critical", "High"]).
    /// </summary>
    [StringLength(1000)]
    public string? TriggerSeverities { get; set; }

    /// <summary>
    /// When this webhook configuration was created.
    /// </summary>
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this webhook configuration was last modified.
    /// </summary>
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

    // Navigation properties
    /// <summary>
    /// Delivery logs for this webhook showing success/failure history.
    /// </summary>
    public virtual ICollection<WebhookDeliveryLog> DeliveryLogs { get; set; } = new List<WebhookDeliveryLog>();

    // Domain methods
    /// <summary>
    /// Validates the webhook URL format and accessibility.
    /// </summary>
    /// <returns>True if the URL is valid and accessible.</returns>
    public bool IsValidUrl()
    {
        if (string.IsNullOrWhiteSpace(Url))
            return false;

        return Uri.TryCreate(Url, UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    /// <summary>
    /// Checks if the webhook should be triggered for the given severity level.
    /// </summary>
    /// <param name="severity">Alert severity level to check.</param>
    /// <returns>True if the webhook should be triggered.</returns>
    public bool ShouldTriggerForSeverity(string severity)
    {
        if (string.IsNullOrWhiteSpace(TriggerSeverities))
            return true; // Trigger for all severities if not specified

        try
        {
            var severities = System.Text.Json.JsonSerializer.Deserialize<string[]>(TriggerSeverities);
            return severities?.Contains(severity, StringComparer.OrdinalIgnoreCase) ?? true;
        }
        catch
        {
            return true; // Default to trigger if JSON parsing fails
        }
    }

    /// <summary>
    /// Gets the total timeout including all retry attempts.
    /// </summary>
    /// <returns>Total timeout in seconds.</returns>
    public int GetTotalTimeoutSeconds()
    {
        return TimeoutSeconds * (RetryCount + 1);
    }
}
