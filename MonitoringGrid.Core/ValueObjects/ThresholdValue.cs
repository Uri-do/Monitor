namespace MonitoringGrid.Core.ValueObjects;

/// <summary>
/// Value object representing a threshold value with comparison logic
/// </summary>
public record ThresholdValue
{
    public decimal Value { get; }
    public string ComparisonOperator { get; }
    public string ThresholdType { get; }

    public ThresholdValue(decimal value, string comparisonOperator, string thresholdType = "threshold_value")
    {
        if (!IsValidComparisonOperator(comparisonOperator))
            throw new ArgumentException($"Invalid comparison operator: {comparisonOperator}", nameof(comparisonOperator));

        if (!IsValidThresholdType(thresholdType))
            throw new ArgumentException($"Invalid threshold type: {thresholdType}", nameof(thresholdType));

        Value = value;
        ComparisonOperator = comparisonOperator.ToLowerInvariant();
        ThresholdType = thresholdType.ToLowerInvariant();
    }

    private static bool IsValidComparisonOperator(string op)
    {
        return op?.ToLowerInvariant() switch
        {
            "gt" or "gte" or "lt" or "lte" or "eq" or "ne" => true,
            _ => false
        };
    }

    private static bool IsValidThresholdType(string type)
    {
        return type?.ToLowerInvariant() switch
        {
            "threshold_value" or "threshold_percentage" => true,
            _ => false
        };
    }

    /// <summary>
    /// Evaluates if the current value breaches this threshold
    /// </summary>
    public bool IsBreached(decimal currentValue)
    {
        return ComparisonOperator switch
        {
            "gt" => currentValue > Value,
            "gte" => currentValue >= Value,
            "lt" => currentValue < Value,
            "lte" => currentValue <= Value,
            "eq" => Math.Abs(currentValue - Value) < 0.01m,
            "ne" => Math.Abs(currentValue - Value) >= 0.01m,
            _ => false
        };
    }

    /// <summary>
    /// Evaluates if the current value breaches this threshold compared to historical value
    /// </summary>
    public bool IsBreached(decimal currentValue, decimal historicalValue)
    {
        if (ThresholdType == "threshold_percentage")
        {
            if (historicalValue == 0)
                return false; // Cannot calculate percentage with zero historical value

            var percentageChange = Math.Abs((currentValue - historicalValue) / historicalValue) * 100;
            return IsBreached(percentageChange);
        }

        return IsBreached(currentValue);
    }

    /// <summary>
    /// Gets the severity level based on how much the threshold is breached
    /// </summary>
    public string GetBreachSeverity(decimal currentValue, decimal? historicalValue = null)
    {
        if (!IsBreached(currentValue, historicalValue ?? currentValue))
            return "None";

        var deviation = CalculateDeviation(currentValue, historicalValue ?? currentValue);

        return deviation switch
        {
            >= 100 => "Critical",
            >= 50 => "High",
            >= 25 => "Medium",
            >= 10 => "Low",
            _ => "Minimal"
        };
    }

    /// <summary>
    /// Calculates the deviation percentage from the threshold
    /// </summary>
    public decimal CalculateDeviation(decimal currentValue, decimal historicalValue)
    {
        if (ThresholdType == "threshold_percentage")
        {
            if (historicalValue == 0)
                return 0;

            return Math.Abs((currentValue - historicalValue) / historicalValue) * 100;
        }

        if (Value == 0)
            return 0;

        return Math.Abs((currentValue - Value) / Value) * 100;
    }

    /// <summary>
    /// Gets a human-readable description of the threshold
    /// </summary>
    public string GetDescription()
    {
        var operatorText = ComparisonOperator switch
        {
            "gt" => "greater than",
            "gte" => "greater than or equal to",
            "lt" => "less than",
            "lte" => "less than or equal to",
            "eq" => "equal to",
            "ne" => "not equal to",
            _ => "compared to"
        };

        var typeText = ThresholdType == "threshold_percentage" ? "% change" : "";

        return $"Alert when value is {operatorText} {Value:F2}{typeText}";
    }

    /// <summary>
    /// Gets the color code for UI display based on breach severity
    /// </summary>
    public string GetSeverityColorCode(decimal currentValue, decimal? historicalValue = null)
    {
        var severity = GetBreachSeverity(currentValue, historicalValue);

        return severity switch
        {
            "Critical" => "#FF0000", // Red
            "High" => "#FF8C00",     // Orange
            "Medium" => "#FFD700",   // Gold
            "Low" => "#32CD32",      // Green
            "Minimal" => "#90EE90",  // Light Green
            _ => "#808080"           // Gray
        };
    }

    /// <summary>
    /// Determines if immediate action is required based on breach severity
    /// </summary>
    public bool RequiresImmediateAction(decimal currentValue, decimal? historicalValue = null)
    {
        var severity = GetBreachSeverity(currentValue, historicalValue);
        return severity is "Critical" or "High";
    }

    /// <summary>
    /// Creates a threshold for percentage-based comparison
    /// </summary>
    public static ThresholdValue CreatePercentageThreshold(decimal percentage, string comparisonOperator)
    {
        return new ThresholdValue(percentage, comparisonOperator, "threshold_percentage");
    }

    /// <summary>
    /// Creates a threshold for absolute value comparison
    /// </summary>
    public static ThresholdValue CreateAbsoluteThreshold(decimal value, string comparisonOperator)
    {
        return new ThresholdValue(value, comparisonOperator, "threshold_value");
    }

    public static implicit operator decimal(ThresholdValue threshold) => threshold.Value;

    public override string ToString() => GetDescription();
}
