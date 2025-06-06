using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MonitoringGrid.Core.Entities;

/// <summary>
/// Webhook configuration entity
/// </summary>
[Table("WebhookConfigurations")]
public class WebhookConfig
{
    [Key]
    public int WebhookId { get; set; }
    
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [StringLength(500)]
    public string Url { get; set; } = string.Empty;
    
    [StringLength(10)]
    public string HttpMethod { get; set; } = "POST";
    
    [StringLength(4000)]
    public string? Headers { get; set; } // JSON serialized headers
    
    [StringLength(4000)]
    public string? PayloadTemplate { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public int TimeoutSeconds { get; set; } = 30;
    
    public int RetryCount { get; set; } = 3;
    
    [StringLength(1000)]
    public string? TriggerSeverities { get; set; } // JSON serialized list
    
    public DateTime CreatedDate { get; set; }
    
    public DateTime ModifiedDate { get; set; }
    
    // Navigation properties
    public virtual ICollection<WebhookDeliveryLog> DeliveryLogs { get; set; } = new List<WebhookDeliveryLog>();
}
