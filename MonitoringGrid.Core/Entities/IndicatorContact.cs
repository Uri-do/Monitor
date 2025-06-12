using MonitoringGrid.Core.Common;

namespace MonitoringGrid.Core.Entities;

/// <summary>
/// Junction entity for many-to-many relationship between Indicators and Contacts
/// Replaces KpiContact entity
/// </summary>
public class IndicatorContact
{
    public int IndicatorId { get; set; }
    public int ContactId { get; set; }

    // Navigation properties
    public virtual Indicator Indicator { get; set; } = null!;
    public virtual Contact Contact { get; set; } = null!;
}
