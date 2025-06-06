using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.ValueObjects;

namespace MonitoringGrid.Core.Factories;

/// <summary>
/// Factory interface for creating Alert entities
/// </summary>
public interface IAlertFactory
{
    /// <summary>
    /// Creates a new alert from KPI execution results
    /// </summary>
    AlertLog CreateAlert(KPI kpi, decimal currentValue, decimal historicalValue, 
        decimal? deviation = null, string? customMessage = null);

    /// <summary>
    /// Creates a test alert for validation purposes
    /// </summary>
    AlertLog CreateTestAlert(KPI kpi, string testMessage);

    /// <summary>
    /// Creates an alert from a threshold breach
    /// </summary>
    AlertLog CreateThresholdAlert(KPI kpi, decimal currentValue, decimal threshold, string thresholdType);
}

/// <summary>
/// Factory implementation for creating Alert entities
/// </summary>
public class AlertFactory : IAlertFactory
{
    public AlertLog CreateAlert(KPI kpi, decimal currentValue, decimal historicalValue, 
        decimal? deviation = null, string? customMessage = null)
    {
        if (kpi == null)
            throw new ArgumentNullException(nameof(kpi));

        // Calculate deviation if not provided
        var actualDeviation = deviation ?? CalculateDeviation(currentValue, historicalValue);
        var deviationPercentage = new DeviationPercentage(actualDeviation);

        // Determine severity based on deviation and KPI priority
        var severity = DetermineSeverity(deviationPercentage, kpi.Priority);

        // Build alert message
        var subject = BuildSubject(kpi, currentValue, historicalValue, actualDeviation);
        var message = customMessage ?? BuildMessage(kpi, currentValue, historicalValue, actualDeviation);

        var alert = new AlertLog
        {
            KpiId = kpi.KpiId,
            Subject = subject,
            Message = message,
            Description = kpi.DescriptionTemplate,
            CurrentValue = currentValue,
            HistoricalValue = historicalValue,
            DeviationPercent = actualDeviation,
            TriggerTime = DateTime.UtcNow,
            IsResolved = false,
            KPI = kpi
        };

        return alert;
    }

    public AlertLog CreateTestAlert(KPI kpi, string testMessage)
    {
        if (kpi == null)
            throw new ArgumentNullException(nameof(kpi));

        var alert = new AlertLog
        {
            KpiId = kpi.KpiId,
            Subject = $"TEST ALERT: {kpi.Indicator}",
            Message = testMessage,
            Description = "This is a test alert for validation purposes",
            CurrentValue = 0,
            HistoricalValue = 0,
            DeviationPercent = 0,
            TriggerTime = DateTime.UtcNow,
            IsResolved = false,
            KPI = kpi
        };

        return alert;
    }

    public AlertLog CreateThresholdAlert(KPI kpi, decimal currentValue, decimal threshold, string thresholdType)
    {
        if (kpi == null)
            throw new ArgumentNullException(nameof(kpi));

        var message = $"KPI '{kpi.Indicator}' has breached the {thresholdType} threshold. " +
                     $"Current value: {currentValue:N2}, Threshold: {threshold:N2}";

        var alert = new AlertLog
        {
            KpiId = kpi.KpiId,
            Subject = $"THRESHOLD BREACH: {kpi.Indicator}",
            Message = message,
            Description = kpi.DescriptionTemplate,
            CurrentValue = currentValue,
            HistoricalValue = threshold,
            DeviationPercent = Math.Abs(currentValue - threshold),
            TriggerTime = DateTime.UtcNow,
            IsResolved = false,
            KPI = kpi
        };

        return alert;
    }

    private static decimal CalculateDeviation(decimal current, decimal historical)
    {
        if (historical == 0)
            return 0;

        return Math.Abs((current - historical) / historical) * 100;
    }

    private static string DetermineSeverity(DeviationPercentage deviation, byte priority)
    {
        // High priority KPIs (SMS alerts) have lower thresholds
        if (priority == 1) // SMS + Email
        {
            return deviation.Value switch
            {
                >= 75 => "Emergency",
                >= 50 => "Critical",
                >= 25 => "High",
                >= 10 => "Medium",
                _ => "Low"
            };
        }
        else // Email only
        {
            return deviation.Value switch
            {
                >= 100 => "Emergency",
                >= 75 => "Critical",
                >= 50 => "High",
                >= 25 => "Medium",
                _ => "Low"
            };
        }
    }

    private static string BuildSubject(KPI kpi, decimal current, decimal historical, decimal deviation)
    {
        var template = kpi.SubjectTemplate;
        
        return template
            .Replace("{Indicator}", kpi.Indicator)
            .Replace("{Owner}", kpi.Owner)
            .Replace("{CurrentValue}", current.ToString("N2"))
            .Replace("{HistoricalValue}", historical.ToString("N2"))
            .Replace("{Deviation}", deviation.ToString("F2"));
    }

    private static string BuildMessage(KPI kpi, decimal current, decimal historical, decimal deviation)
    {
        var template = kpi.DescriptionTemplate;
        
        return template
            .Replace("{Indicator}", kpi.Indicator)
            .Replace("{Owner}", kpi.Owner)
            .Replace("{CurrentValue}", current.ToString("N2"))
            .Replace("{HistoricalValue}", historical.ToString("N2"))
            .Replace("{Deviation}", deviation.ToString("F2"))
            .Replace("{DateTime}", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"));
    }
}
