namespace MonitoringGrid.Core.DTOs;

/// <summary>
/// Data transfer object for collector information
/// </summary>
public class CollectorDto
{
    public long CollectorID { get; set; }
    public string CollectorCode { get; set; } = string.Empty;
    public string CollectorDesc { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public List<string> AvailableItems { get; set; } = new();

    // Convenience properties for compatibility
    public long Id => CollectorID;
    public string Name => CollectorCode;
    public string Description => CollectorDesc;
}
