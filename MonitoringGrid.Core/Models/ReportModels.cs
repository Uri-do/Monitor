namespace MonitoringGrid.Core.Models;

/// <summary>
/// KPI report request model
/// </summary>
public class KpiReportRequest
{
    public List<int>? KpiIds { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Format { get; set; } = "PDF"; // PDF, Excel, CSV
    public bool IncludeCharts { get; set; } = true;
    public bool IncludeTrends { get; set; } = true;
    public string? TemplateId { get; set; }
}

/// <summary>
/// Alert report request model
/// </summary>
public class AlertReportRequest
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<int>? KpiIds { get; set; }
    public List<string>? Severities { get; set; }
    public bool? IsResolved { get; set; }
    public string Format { get; set; } = "PDF";
    public bool IncludeStatistics { get; set; } = true;
}

/// <summary>
/// Performance report request model
/// </summary>
public class PerformanceReportRequest
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string ReportType { get; set; } = "Summary"; // Summary, Detailed, Trends
    public string Format { get; set; } = "PDF";
    public bool IncludeBenchmarks { get; set; } = true;
}

/// <summary>
/// Custom report request model
/// </summary>
public class CustomReportRequest
{
    public string ReportName { get; set; } = string.Empty;
    public string Query { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public string Format { get; set; } = "PDF";
    public string? TemplateId { get; set; }
}

/// <summary>
/// Report parameter model
/// </summary>
public class ReportParameter
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public object? DefaultValue { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// Report schedule request model
/// </summary>
public class ReportScheduleRequest
{
    public string Name { get; set; } = string.Empty;
    public string ReportType { get; set; } = string.Empty;
    public string CronExpression { get; set; } = string.Empty;
    public List<string> Recipients { get; set; } = new();
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// Report template model
/// </summary>
public class ReportTemplate
{
    public int TemplateId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ReportType { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

/// <summary>
/// Report schedule model
/// </summary>
public class ReportSchedule
{
    public int ScheduleId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ReportType { get; set; } = string.Empty;
    public string CronExpression { get; set; } = string.Empty;
    public List<string> Recipients { get; set; } = new();
    public Dictionary<string, object> Parameters { get; set; } = new();
    public bool IsActive { get; set; }
    public DateTime? NextRun { get; set; }
}
