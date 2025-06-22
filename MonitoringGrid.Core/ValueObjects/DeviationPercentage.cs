namespace MonitoringGrid.Core.ValueObjects;

/// <summary>
/// Value object representing a deviation percentage with business logic
/// </summary>
public record DeviationPercentage
{
    // Severity thresholds
    public const decimal CriticalThreshold = 50m;
    public const decimal HighThreshold = 25m;
    public const decimal MediumThreshold = 10m;
    public const decimal LowThreshold = 5m;

    // Color codes for UI
    public const string CriticalColor = "#FF0000"; // Red
    public const string HighColor = "#FF8C00";     // Orange
    public const string MediumColor = "#FFD700";   // Gold
    public const string LowColor = "#32CD32";      // Green
    public const string MinimalColor = "#90EE90";  // Light Green

    public decimal Value { get; }

    public DeviationPercentage(decimal percentage)
    {
        if (percentage < 0)
            throw new ArgumentException("Deviation percentage cannot be negative", nameof(percentage));

        Value = Math.Round(percentage, 2);
    }

    public static implicit operator decimal(DeviationPercentage deviation) => deviation.Value;

    public static explicit operator DeviationPercentage(decimal percentage) => new(percentage);

    public string GetSeverityLevel()
    {
        return Value switch
        {
            >= CriticalThreshold => "Critical",
            >= HighThreshold => "High",
            >= MediumThreshold => "Medium",
            >= LowThreshold => "Low",
            _ => "Minimal"
        };
    }

    public bool RequiresImmediateAttention()
    {
        return Value >= HighThreshold;
    }

    public bool RequiresSmsAlert()
    {
        return Value >= CriticalThreshold;
    }

    public string GetColorCode()
    {
        return Value switch
        {
            >= CriticalThreshold => CriticalColor,
            >= HighThreshold => HighColor,
            >= MediumThreshold => MediumColor,
            >= LowThreshold => LowColor,
            _ => MinimalColor
        };
    }

    public static DeviationPercentage Calculate(decimal current, decimal historical)
    {
        if (historical == 0)
            return new DeviationPercentage(0);

        var deviation = Math.Abs((current - historical) / historical) * 100;
        return new DeviationPercentage(deviation);
    }

    public override string ToString() => $"{Value:F2}%";
}
