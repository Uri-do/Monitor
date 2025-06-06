using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.ValueObjects;

namespace MonitoringGrid.Core.Factories;

/// <summary>
/// Factory interface for creating KPI entities
/// </summary>
public interface IKpiFactory
{
    /// <summary>
    /// Creates a new KPI with validation
    /// </summary>
    KPI CreateKpi(string indicator, string owner, byte priority, int frequency, 
        decimal deviation, string spName, string subjectTemplate, string descriptionTemplate);

    /// <summary>
    /// Creates a KPI from a template
    /// </summary>
    KPI CreateFromTemplate(KpiTemplate template, string owner);

    /// <summary>
    /// Creates a copy of an existing KPI with modifications
    /// </summary>
    KPI CreateCopy(KPI sourceKpi, string newIndicator, string? newOwner = null);
}

/// <summary>
/// KPI template for factory creation
/// </summary>
public class KpiTemplate
{
    public string Indicator { get; set; } = string.Empty;
    public byte Priority { get; set; }
    public int Frequency { get; set; }
    public decimal Deviation { get; set; }
    public string SpName { get; set; } = string.Empty;
    public string SubjectTemplate { get; set; } = string.Empty;
    public string DescriptionTemplate { get; set; } = string.Empty;
    public int CooldownMinutes { get; set; } = 30;
    public decimal? MinimumThreshold { get; set; }
}

/// <summary>
/// Factory implementation for creating KPI entities
/// </summary>
public class KpiFactory : IKpiFactory
{
    public KPI CreateKpi(string indicator, string owner, byte priority, int frequency, 
        decimal deviation, string spName, string subjectTemplate, string descriptionTemplate)
    {
        // Validate inputs
        if (string.IsNullOrWhiteSpace(indicator))
            throw new ArgumentException("Indicator cannot be empty", nameof(indicator));

        if (string.IsNullOrWhiteSpace(owner))
            throw new ArgumentException("Owner cannot be empty", nameof(owner));

        if (priority < 1 || priority > 2)
            throw new ArgumentException("Priority must be 1 (SMS + Email) or 2 (Email Only)", nameof(priority));

        if (frequency <= 0)
            throw new ArgumentException("Frequency must be greater than 0", nameof(frequency));

        if (deviation < 0 || deviation > 100)
            throw new ArgumentException("Deviation must be between 0 and 100", nameof(deviation));

        if (string.IsNullOrWhiteSpace(spName))
            throw new ArgumentException("Stored procedure name cannot be empty", nameof(spName));

        // Create KPI with validated data
        var kpi = new KPI
        {
            Indicator = indicator.Trim(),
            Owner = owner.Trim(),
            Priority = priority,
            Frequency = frequency,
            Deviation = deviation,
            SpName = spName.Trim(),
            SubjectTemplate = subjectTemplate.Trim(),
            DescriptionTemplate = descriptionTemplate.Trim(),
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            CooldownMinutes = 30 // Default cooldown
        };

        return kpi;
    }

    public KPI CreateFromTemplate(KpiTemplate template, string owner)
    {
        return CreateKpi(
            template.Indicator,
            owner,
            template.Priority,
            template.Frequency,
            template.Deviation,
            template.SpName,
            template.SubjectTemplate,
            template.DescriptionTemplate
        );
    }

    public KPI CreateCopy(KPI sourceKpi, string newIndicator, string? newOwner = null)
    {
        return CreateKpi(
            newIndicator,
            newOwner ?? sourceKpi.Owner,
            sourceKpi.Priority,
            sourceKpi.Frequency,
            sourceKpi.Deviation,
            sourceKpi.SpName,
            sourceKpi.SubjectTemplate,
            sourceKpi.DescriptionTemplate
        );
    }
}
