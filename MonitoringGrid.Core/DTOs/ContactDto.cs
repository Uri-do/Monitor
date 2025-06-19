namespace MonitoringGrid.Core.DTOs;

/// <summary>
/// Contact data transfer object
/// </summary>
public class ContactDto
{
    public int ContactID { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; }
    public DateTime? CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public int IndicatorCount { get; set; }
    public List<IndicatorSummaryDto> AssignedIndicators { get; set; } = new();
}

/// <summary>
/// Indicator summary for contact assignments
/// </summary>
public class IndicatorSummaryDto
{
    public long IndicatorId { get; set; }
    public string IndicatorName { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
