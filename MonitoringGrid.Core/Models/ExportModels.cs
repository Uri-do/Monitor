namespace MonitoringGrid.Core.Models;

/// <summary>
/// Data export request model
/// </summary>
public class DataExportRequest
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Format { get; set; } = "CSV"; // CSV, Excel, JSON, XML
    public List<string>? Columns { get; set; }
    public Dictionary<string, object> Filters { get; set; } = new();
    public bool IncludeHeaders { get; set; } = true;
}

/// <summary>
/// Scheduled export request model
/// </summary>
public class ScheduledExportRequest
{
    public string Name { get; set; } = string.Empty;
    public string ExportType { get; set; } = string.Empty;
    public string CronExpression { get; set; } = string.Empty;
    public DataExportRequest ExportRequest { get; set; } = new();
    public List<string> Recipients { get; set; } = new();
}
