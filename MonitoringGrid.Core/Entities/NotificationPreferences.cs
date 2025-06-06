using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MonitoringGrid.Core.Entities;

/// <summary>
/// Notification preferences entity
/// </summary>
[Table("NotificationPreferences")]
public class NotificationPreferences
{
    [Key]
    [StringLength(100)]
    public string UserId { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string EnabledChannels { get; set; } = string.Empty; // JSON serialized list
    
    [StringLength(2000)]
    public string? ChannelSettings { get; set; } // JSON serialized settings
    
    public bool EnableQuietHours { get; set; }
    
    public TimeSpan QuietHoursStart { get; set; }
    
    public TimeSpan QuietHoursEnd { get; set; }
    
    public DateTime CreatedDate { get; set; }
    
    public DateTime ModifiedDate { get; set; }
}
