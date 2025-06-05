namespace MonitoringGrid.Core.Entities;

/// <summary>
/// Many-to-many relationship between KPIs and Contacts
/// </summary>
public class KpiContact
{
    public int KpiId { get; set; }
    public int ContactId { get; set; }

    // Navigation properties
    public virtual KPI KPI { get; set; } = null!;
    public virtual Contact Contact { get; set; } = null!;
}
