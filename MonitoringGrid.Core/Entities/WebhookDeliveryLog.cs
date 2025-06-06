using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MonitoringGrid.Core.Entities;

/// <summary>
/// Webhook delivery log entity
/// </summary>
[Table("WebhookDeliveryLogs")]
public class WebhookDeliveryLog
{
    [Key]
    public int LogId { get; set; }
    
    public int WebhookId { get; set; }
    
    public DateTime DeliveryTime { get; set; }
    
    public int StatusCode { get; set; }
    
    [StringLength(4000)]
    public string? Response { get; set; }
    
    public bool IsSuccess { get; set; }
    
    [StringLength(1000)]
    public string? ErrorMessage { get; set; }
    
    public int RetryCount { get; set; }
    
    public double ResponseTimeMs { get; set; }
    
    // Navigation properties
    [ForeignKey("WebhookId")]
    public virtual WebhookConfig Webhook { get; set; } = null!;
}
