using System.ComponentModel.DataAnnotations;

namespace MonitoringGrid.Core.Entities;

/// <summary>
/// System configuration settings
/// </summary>
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

    // Domain methods
    public T GetValue<T>()
    {
        return (T)Convert.ChangeType(ConfigValue, typeof(T));
    }

    public bool GetBoolValue()
    {
        return bool.TryParse(ConfigValue, out var result) && result;
    }

    public int GetIntValue()
    {
        return int.TryParse(ConfigValue, out var result) ? result : 0;
    }

    public decimal GetDecimalValue()
    {
        return decimal.TryParse(ConfigValue, out var result) ? result : 0;
    }
}
