namespace MonitoringGrid.Core.ValueObjects;

/// <summary>
/// Value object representing a deviation percentage with business logic
/// </summary>
public record DeviationPercentage
{
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
            >= 50 => "Critical",
            >= 25 => "High", 
            >= 10 => "Medium",
            >= 5 => "Low",
            _ => "Minimal"
        };
    }

    public bool RequiresImmediateAttention()
    {
        return Value >= 25;
    }

    public bool RequiresSmsAlert()
    {
        return Value >= 50;
    }

    public string GetColorCode()
    {
        return Value switch
        {
            >= 50 => "#FF0000", // Red
            >= 25 => "#FF8C00", // Orange
            >= 10 => "#FFD700", // Gold
            >= 5 => "#32CD32",  // Green
            _ => "#90EE90"      // Light Green
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
