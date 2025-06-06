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

    [MaxLength(50)]
    public string? Category { get; set; }

    public bool IsEncrypted { get; set; } = false;

    public bool IsReadOnly { get; set; } = false;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    // Domain methods
    public T GetValue<T>()
    {
        try
        {
            if (typeof(T) == typeof(string))
                return (T)(object)ConfigValue;

            if (typeof(T).IsEnum)
                return (T)Enum.Parse(typeof(T), ConfigValue, true);

            return (T)Convert.ChangeType(ConfigValue, typeof(T));
        }
        catch
        {
            return default(T)!;
        }
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

    public DateTime? GetDateTimeValue()
    {
        return DateTime.TryParse(ConfigValue, out var result) ? result : null;
    }

    public void SetValue<T>(T value)
    {
        if (IsReadOnly)
            throw new InvalidOperationException($"Configuration key '{ConfigKey}' is read-only");

        ConfigValue = value?.ToString() ?? string.Empty;
        ModifiedDate = DateTime.UtcNow;
    }

    public bool IsValidValue(string testValue)
    {
        try
        {
            // Basic validation - can be enhanced based on key patterns
            return !string.IsNullOrEmpty(testValue);
        }
        catch
        {
            return false;
        }
    }
}
