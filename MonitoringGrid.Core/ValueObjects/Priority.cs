namespace MonitoringGrid.Core.ValueObjects;

/// <summary>
/// Value object representing indicator priority with business logic
/// </summary>
public record Priority
{
    public string Value { get; }
    public int NumericValue { get; }

    private Priority(string value, int numericValue)
    {
        Value = value;
        NumericValue = numericValue;
    }

    public static readonly Priority High = new("high", 1);
    public static readonly Priority Medium = new("medium", 2);
    public static readonly Priority Low = new("low", 3);

    public static Priority FromString(string priority)
    {
        return priority?.ToLowerInvariant() switch
        {
            "high" => High,
            "medium" => Medium,
            "low" => Low,
            _ => throw new ArgumentException($"Invalid priority: {priority}", nameof(priority))
        };
    }

    public static Priority FromNumeric(int priority)
    {
        return priority switch
        {
            1 => High,
            2 => Medium,
            3 => Low,
            _ => throw new ArgumentException($"Invalid priority: {priority}", nameof(priority))
        };
    }

    /// <summary>
    /// Determines if SMS alerts should be sent for this priority
    /// </summary>
    public bool RequiresSmsAlert()
    {
        return this == High;
    }

    /// <summary>
    /// Determines if email alerts should be sent for this priority
    /// </summary>
    public bool RequiresEmailAlert()
    {
        return true; // All priorities require email alerts
    }

    /// <summary>
    /// Gets the escalation timeout in minutes for this priority
    /// </summary>
    public int GetEscalationTimeoutMinutes()
    {
        return this == High ? 15 : this == Medium ? 60 : 240;
    }

    /// <summary>
    /// Gets the cooldown period in minutes for this priority
    /// </summary>
    public int GetCooldownMinutes()
    {
        return this == High ? 5 : this == Medium ? 15 : 30;
    }

    /// <summary>
    /// Gets the color code for UI display
    /// </summary>
    public string GetColorCode()
    {
        return this == High ? "#FF0000" : this == Medium ? "#FFA500" : "#008000";
    }

    /// <summary>
    /// Gets the display name for the priority
    /// </summary>
    public string GetDisplayName()
    {
        return this == High ? "High Priority" : this == Medium ? "Medium Priority" : "Low Priority";
    }

    /// <summary>
    /// Gets the icon for UI display
    /// </summary>
    public string GetIcon()
    {
        return this == High ? "🔴" : this == Medium ? "🟡" : "🟢";
    }

    /// <summary>
    /// Determines if this priority is higher than another
    /// </summary>
    public bool IsHigherThan(Priority other)
    {
        return NumericValue < other.NumericValue;
    }

    /// <summary>
    /// Determines if this priority is lower than another
    /// </summary>
    public bool IsLowerThan(Priority other)
    {
        return NumericValue > other.NumericValue;
    }

    public static implicit operator string(Priority priority) => priority.Value;
    public static implicit operator int(Priority priority) => priority.NumericValue;
    public static explicit operator Priority(string priority) => FromString(priority);
    public static explicit operator Priority(int priority) => FromNumeric(priority);

    public override string ToString() => GetDisplayName();
}
