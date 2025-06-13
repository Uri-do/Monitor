using FluentValidation;
using MonitoringGrid.Api.Common;

namespace MonitoringGrid.Api.Validation;

/// <summary>
/// Enhanced validation extensions for FluentValidation
/// </summary>
public static class EnhancedValidationExtensions
{
    /// <summary>
    /// Validates that frequency is appropriate for priority level
    /// </summary>
    public static IRuleBuilderOptions<T, int> MustHaveAppropriateFrequencyForPriority<T>(
        this IRuleBuilder<T, int> ruleBuilder, 
        Func<T, byte> prioritySelector)
    {
        return ruleBuilder.Must((instance, frequency) =>
        {
            var priority = prioritySelector(instance);
            return ValidateFrequencyForPriority(frequency, priority);
        }).WithMessage((instance, frequency) =>
        {
            var priority = prioritySelector(instance);
            return GetFrequencyValidationMessage(frequency, priority);
        });
    }

    /// <summary>
    /// Validates that cooldown period is reasonable for frequency
    /// </summary>
    public static IRuleBuilderOptions<T, int> MustHaveReasonableCooldownForFrequency<T>(
        this IRuleBuilder<T, int> ruleBuilder,
        Func<T, int> frequencySelector)
    {
        return ruleBuilder.Must((instance, cooldown) =>
        {
            var frequency = frequencySelector(instance);
            return ValidateCooldownPeriod(cooldown, frequency);
        }).WithMessage((instance, cooldown) =>
        {
            var frequency = frequencySelector(instance);
            return GetCooldownValidationMessage(cooldown, frequency);
        });
    }

    /// <summary>
    /// Validates that data window is appropriate for frequency
    /// </summary>
    public static IRuleBuilderOptions<T, int> MustHaveAppropriateDataWindowForFrequency<T>(
        this IRuleBuilder<T, int> ruleBuilder,
        Func<T, int> frequencySelector)
    {
        return ruleBuilder.Must((instance, dataWindow) =>
        {
            var frequency = frequencySelector(instance);
            return ValidateDataWindow(dataWindow, frequency);
        }).WithMessage((instance, dataWindow) =>
        {
            var frequency = frequencySelector(instance);
            return GetDataWindowValidationMessage(dataWindow, frequency);
        });
    }

    /// <summary>
    /// Validates stored procedure naming conventions and security
    /// </summary>
    public static IRuleBuilderOptions<T, string> MustBeSecureStoredProcedureName<T>(
        this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder.Must(spName => ValidateStoredProcedureName(spName))
            .WithMessage("Stored procedure name must be in 'monitoring' or 'stats' schema and contain no dangerous patterns");
    }

    /// <summary>
    /// Validates template contains required placeholders and is safe
    /// </summary>
    public static IRuleBuilderOptions<T, string> MustBeValidTemplate<T>(
        this IRuleBuilder<T, string> ruleBuilder, string templateType = "Template")
    {
        return ruleBuilder.Must(template => ValidateTemplate(template))
            .WithMessage($"{templateType} must contain {{current}}, {{historical}}, and {{deviation}} placeholders and be safe");
    }

    /// <summary>
    /// Validates deviation threshold based on Indicator type
    /// </summary>
    public static IRuleBuilderOptions<T, decimal> MustHaveAppropriateDeviationForIndicatorType<T>(
        this IRuleBuilder<T, decimal> ruleBuilder,
        Func<T, string?> indicatorTypeSelector)
    {
        return ruleBuilder.Must((instance, deviation) =>
        {
            var indicatorType = indicatorTypeSelector(instance) ?? "general";
            return ValidateDeviationThreshold(deviation, indicatorType);
        }).WithMessage((instance, deviation) =>
        {
            var indicatorType = indicatorTypeSelector(instance) ?? "general";
            return GetDeviationValidationMessage(deviation, indicatorType);
        });
    }

    // Private validation methods
    private static bool ValidateFrequencyForPriority(int frequency, byte priority)
    {
        return priority switch
        {
            1 when frequency < 5 => false, // SMS alerts shouldn't run more than every 5 minutes
            1 when frequency > 1440 => false, // High priority should run at least daily
            2 when frequency < 1 => false, // Email-only shouldn't run more than every minute
            2 when frequency > 10080 => false, // Email-only should run at least weekly
            _ => true
        };
    }

    private static string GetFrequencyValidationMessage(int frequency, byte priority)
    {
        return priority switch
        {
            1 when frequency < 5 => "High priority Indicators (SMS alerts) should not run more frequently than every 5 minutes to avoid spam",
            1 when frequency > 1440 => "High priority Indicators should run at least once per day",
            2 when frequency < 1 => "Email-only Indicators should not run more frequently than every minute",
            2 when frequency > 10080 => "Email-only Indicators should run at least once per week",
            _ => "Frequency is valid for this priority level"
        };
    }

    private static bool ValidateCooldownPeriod(int cooldownMinutes, int frequency)
    {
        if (cooldownMinutes < 0) return false;
        if (cooldownMinutes > frequency * 10) return false; // Cooldown shouldn't exceed 10x frequency
        if (frequency <= 5 && cooldownMinutes < frequency) return false; // High-frequency needs adequate cooldown
        return true;
    }

    private static string GetCooldownValidationMessage(int cooldownMinutes, int frequency)
    {
        if (cooldownMinutes < 0) return "Cooldown period cannot be negative";
        if (cooldownMinutes > frequency * 10) return "Cooldown period should not exceed 10 times the execution frequency";
        if (frequency <= 5 && cooldownMinutes < frequency) return "High-frequency Indicators should have cooldown periods at least equal to their frequency";
        return "Cooldown period is valid";
    }

    private static bool ValidateDataWindow(int lastMinutes, int frequency)
    {
        if (lastMinutes <= 0) return false;
        if (lastMinutes < frequency) return false; // Data window should be at least equal to frequency
        if (lastMinutes > frequency * 100) return false; // Data window shouldn't be excessive
        if (frequency <= 5 && lastMinutes > 60) return false; // High-frequency should use smaller windows
        if (frequency >= 1440 && lastMinutes < 1440) return false; // Daily Indicators should use at least 24h windows
        return true;
    }

    private static string GetDataWindowValidationMessage(int lastMinutes, int frequency)
    {
        if (lastMinutes <= 0) return "Data window must be greater than 0";
        if (lastMinutes < frequency) return "Data window should be at least equal to the execution frequency";
        if (lastMinutes > frequency * 100) return "Data window should not exceed 100 times the execution frequency";
        if (frequency <= 5 && lastMinutes > 60) return "High-frequency Indicators (≤5 min) should use data windows ≤60 minutes";
        if (frequency >= 1440 && lastMinutes < 1440) return "Daily Indicators should use data windows of at least 24 hours";
        return "Data window is valid";
    }

    private static bool ValidateStoredProcedureName(string spName)
    {
        if (string.IsNullOrWhiteSpace(spName)) return false;
        
        // Check for dangerous patterns
        var dangerousPatterns = new[] { ";", "--", "/*", "*/", "xp_", "DROP", "DELETE", "TRUNCATE", "ALTER" };
        if (dangerousPatterns.Any(pattern => spName.ToUpper().Contains(pattern.ToUpper()))) return false;
        
        // Validate naming convention
        if (!spName.StartsWith("monitoring.") && !spName.StartsWith("stats.")) return false;
        
        // Check length
        if (spName.Length > 255) return false;
        
        return true;
    }

    private static bool ValidateTemplate(string template)
    {
        if (string.IsNullOrWhiteSpace(template)) return false;
        
        // Check for required placeholders
        var requiredPlaceholders = new[] { "{current}", "{historical}", "{deviation}" };
        if (requiredPlaceholders.Any(p => !template.Contains(p))) return false;
        
        // Check for dangerous content
        var dangerousPatterns = new[] { "<script", "javascript:", "onclick=", "onerror=" };
        if (dangerousPatterns.Any(pattern => template.ToLower().Contains(pattern))) return false;
        
        return true;
    }

    private static bool ValidateDeviationThreshold(decimal deviation, string indicatorType)
    {
        if (deviation < 0 || deviation > 100) return false;
        
        return indicatorType.ToLower() switch
        {
            "success_rate" when deviation < 1 => false, // Success rates need at least 1% to avoid false positives
            "transaction_count" when deviation < 5 => false, // Transaction counts need at least 5% due to variance
            "performance_metric" when deviation < 2 => false, // Performance metrics need at least 2%
            _ => true
        };
    }

    private static string GetDeviationValidationMessage(decimal deviation, string indicatorType)
    {
        if (deviation < 0 || deviation > 100) return "Deviation must be between 0 and 100 percent";
        
        return indicatorType.ToLower() switch
        {
            "success_rate" when deviation < 1 => "Success rate Indicators should have deviation thresholds of at least 1% to avoid false positives",
            "transaction_count" when deviation < 5 => "Transaction count Indicators should have deviation thresholds of at least 5% due to natural variance",
            "performance_metric" when deviation < 2 => "Performance metric Indicators should have deviation thresholds of at least 2%",
            _ => "Deviation threshold is valid for this Indicator type"
        };
    }
}
