using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Core.Models;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Services;
using MonitoringGrid.Infrastructure.Data;
using System.Text;
using System.Text.Json;

namespace MonitoringGrid.Infrastructure.Services;

/// <summary>
/// Reporting service for generating various types of reports
/// </summary>
public class ReportingService : IReportingService
{
    private readonly MonitoringContext _context;
    private readonly ILogger<ReportingService> _logger;
    private readonly IKpiAnalyticsService _analyticsService;

    public ReportingService(
        MonitoringContext context,
        ILogger<ReportingService> logger,
        IKpiAnalyticsService analyticsService)
    {
        _context = context;
        _logger = logger;
        _analyticsService = analyticsService;
    }

    public async Task<byte[]> GenerateKpiReportAsync(KpiReportRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Generating KPI report for period {StartDate} to {EndDate}", request.StartDate, request.EndDate);

            // Get KPI data
            var kpisQuery = _context.KPIs.AsQueryable();
            if (request.KpiIds?.Any() == true)
            {
                kpisQuery = kpisQuery.Where(k => request.KpiIds.Contains(k.KpiId));
            }

            var kpis = await kpisQuery.ToListAsync(cancellationToken);

            // Get historical data
            var historicalData = await _context.HistoricalData
                .Where(h => h.Timestamp >= request.StartDate && h.Timestamp <= request.EndDate)
                .Where(h => request.KpiIds == null || request.KpiIds.Contains(h.KpiId))
                .ToListAsync(cancellationToken);

            // Get alerts
            var alerts = await _context.AlertLogs
                .Where(a => a.TriggerTime >= request.StartDate && a.TriggerTime <= request.EndDate)
                .Where(a => request.KpiIds == null || request.KpiIds.Contains(a.KpiId))
                .ToListAsync(cancellationToken);

            var reportData = new KpiReportData
            {
                ReportPeriod = $"{request.StartDate:yyyy-MM-dd} to {request.EndDate:yyyy-MM-dd}",
                GeneratedAt = DateTime.UtcNow,
                KpiCount = kpis.Count,
                TotalExecutions = historicalData.Count,
                TotalAlerts = alerts.Count,
                KpiSummaries = await GenerateKpiSummariesAsync(kpis, historicalData, alerts, request, cancellationToken),
                TrendAnalysis = request.IncludeTrends ? await GenerateTrendAnalysisAsync(kpis, cancellationToken) : new List<KpiTrendSummary>()
            };

            return request.Format.ToUpper() switch
            {
                "PDF" => await GeneratePdfReportAsync(reportData, "KPI Report"),
                "EXCEL" => await GenerateExcelReportAsync(reportData, "KPI Report"),
                "CSV" => GenerateCsvReport(reportData),
                _ => throw new ArgumentException($"Unsupported format: {request.Format}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate KPI report");
            throw;
        }
    }

    public async Task<byte[]> GenerateAlertReportAsync(AlertReportRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Generating Alert report for period {StartDate} to {EndDate}", request.StartDate, request.EndDate);

            var alertsQuery = _context.AlertLogs
                .Where(a => a.TriggerTime >= request.StartDate && a.TriggerTime <= request.EndDate);

            if (request.KpiIds?.Any() == true)
            {
                alertsQuery = alertsQuery.Where(a => request.KpiIds.Contains(a.KpiId));
            }

            if (request.IsResolved.HasValue)
            {
                alertsQuery = alertsQuery.Where(a => a.IsResolved == request.IsResolved.Value);
            }

            var alerts = await alertsQuery
                .OrderByDescending(a => a.TriggerTime)
                .ToListAsync(cancellationToken);

            var reportData = new AlertReportData
            {
                ReportPeriod = $"{request.StartDate:yyyy-MM-dd} to {request.EndDate:yyyy-MM-dd}",
                GeneratedAt = DateTime.UtcNow,
                TotalAlerts = alerts.Count,
                ResolvedAlerts = alerts.Count(a => a.IsResolved),
                UnresolvedAlerts = alerts.Count(a => !a.IsResolved),
                AlertsByKpi = alerts.GroupBy(a => a.KpiId.ToString())
                    .ToDictionary(g => g.Key, g => g.Count()),
                AlertsBySeverity = alerts.GroupBy(a => DetermineAlertSeverity(a))
                    .ToDictionary(g => g.Key, g => g.Count()),
                AlertDetails = alerts.Take(1000).ToList() // Limit for performance
            };

            if (request.IncludeStatistics)
            {
                reportData.Statistics = await GenerateAlertStatisticsAsync(alerts, cancellationToken);
            }

            return request.Format.ToUpper() switch
            {
                "PDF" => await GeneratePdfReportAsync(reportData, "Alert Report"),
                "EXCEL" => await GenerateExcelReportAsync(reportData, "Alert Report"),
                "CSV" => GenerateCsvReport(reportData),
                _ => throw new ArgumentException($"Unsupported format: {request.Format}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate Alert report");
            throw;
        }
    }

    public async Task<byte[]> GeneratePerformanceReportAsync(PerformanceReportRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Generating Performance report for period {StartDate} to {EndDate}", request.StartDate, request.EndDate);

            var performanceMetrics = await _analyticsService.GetKpiPerformanceMetricsAsync(request.StartDate, request.EndDate, cancellationToken);

            var reportData = new PerformanceReportData
            {
                ReportPeriod = $"{request.StartDate:yyyy-MM-dd} to {request.EndDate:yyyy-MM-dd}",
                GeneratedAt = DateTime.UtcNow,
                ReportType = request.ReportType,
                PerformanceMetrics = performanceMetrics,
                SystemMetrics = await GenerateSystemMetricsAsync(request.StartDate, request.EndDate, cancellationToken)
            };

            return request.Format.ToUpper() switch
            {
                "PDF" => await GeneratePdfReportAsync(reportData, "Performance Report"),
                "EXCEL" => await GenerateExcelReportAsync(reportData, "Performance Report"),
                "CSV" => GenerateCsvReport(reportData),
                _ => throw new ArgumentException($"Unsupported format: {request.Format}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate Performance report");
            throw;
        }
    }

    public async Task<byte[]> GenerateCustomReportAsync(CustomReportRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Generating custom report: {ReportName}", request.ReportName);

            // Execute custom query (in a real implementation, you'd want to validate and sanitize this)
            // For security, consider using a whitelist of allowed queries or a query builder
            var results = await ExecuteCustomQueryAsync(request.Query, request.Parameters, cancellationToken);

            var reportData = new CustomReportData
            {
                ReportName = request.ReportName,
                GeneratedAt = DateTime.UtcNow,
                Query = request.Query,
                Parameters = request.Parameters,
                Results = results
            };

            return request.Format.ToUpper() switch
            {
                "PDF" => await GeneratePdfReportAsync(reportData, request.ReportName),
                "EXCEL" => await GenerateExcelReportAsync(reportData, request.ReportName),
                "CSV" => GenerateCsvReport(reportData),
                "JSON" => Encoding.UTF8.GetBytes(JsonSerializer.Serialize(reportData, new JsonSerializerOptions { WriteIndented = true })),
                _ => throw new ArgumentException($"Unsupported format: {request.Format}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate custom report: {ReportName}", request.ReportName);
            throw;
        }
    }

    public async Task<List<Core.Entities.ReportTemplate>> GetReportTemplatesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Set<Core.Entities.ReportTemplate>()
                .Where(t => t.IsActive)
                .OrderBy(t => t.Name)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get report templates");
            return new List<Core.Entities.ReportTemplate>();
        }
    }

    public async Task<Core.Entities.ReportSchedule> ScheduleReportAsync(ReportScheduleRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var entitySchedule = new Core.Entities.ReportSchedule
            {
                Name = request.Name,
                ReportType = request.ReportType,
                CronExpression = request.CronExpression,
                Recipients = JsonSerializer.Serialize(request.Recipients),
                Parameters = JsonSerializer.Serialize(request.Parameters),
                IsActive = true,
                NextRun = CalculateNextRun(request.CronExpression)
            };

            _context.Set<Core.Entities.ReportSchedule>().Add(entitySchedule);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Report scheduled: {Name} with cron expression {CronExpression}", request.Name, request.CronExpression);

            return entitySchedule;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to schedule report: {Name}", request.Name);
            throw;
        }
    }

    // Private helper methods
    private async Task<List<KpiSummary>> GenerateKpiSummariesAsync(
        List<KPI> kpis, 
        List<HistoricalData> historicalData, 
        List<AlertLog> alerts, 
        KpiReportRequest request,
        CancellationToken cancellationToken)
    {
        var summaries = new List<KpiSummary>();

        foreach (var kpi in kpis)
        {
            var kpiData = historicalData.Where(h => h.KpiId == kpi.KpiId).ToList();
            var kpiAlerts = alerts.Where(a => a.KpiId == kpi.KpiId).ToList();

            var summary = new KpiSummary
            {
                KpiId = kpi.KpiId,
                Indicator = kpi.Indicator,
                Owner = kpi.Owner,
                ExecutionCount = kpiData.Count,
                SuccessRate = kpiData.Any() ? 100.0 : 0.0, // Simplified - no IsSuccessful property
                AlertCount = kpiAlerts.Count,
                AverageValue = kpiData.Any() ? kpiData.Average(d => d.Value) : 0,
                LastExecution = kpiData.Any() ? kpiData.Max(d => d.Timestamp) : (DateTime?)null
            };

            summaries.Add(summary);
        }

        return summaries;
    }

    private async Task<List<KpiTrendSummary>> GenerateTrendAnalysisAsync(List<KPI> kpis, CancellationToken cancellationToken)
    {
        var trendSummaries = new List<KpiTrendSummary>();

        foreach (var kpi in kpis)
        {
            try
            {
                var trendAnalysis = await _analyticsService.GetKpiTrendAsync(kpi.KpiId, 30, cancellationToken);
                
                trendSummaries.Add(new KpiTrendSummary
                {
                    KpiId = kpi.KpiId,
                    Indicator = kpi.Indicator,
                    TrendDirection = trendAnalysis.TrendDirection.ToString(),
                    TrendStrength = trendAnalysis.TrendStrength,
                    Volatility = (double)trendAnalysis.Volatility
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate trend analysis for KPI {KpiId}", kpi.KpiId);
            }
        }

        return trendSummaries;
    }

    private async Task<AlertStatistics> GenerateAlertStatisticsAsync(List<AlertLog> alerts, CancellationToken cancellationToken)
    {
        return new AlertStatistics
        {
            TotalAlerts = alerts.Count,
            AverageAlertsPerDay = alerts.Any() ? alerts.Count / Math.Max(1, (alerts.Max(a => a.TriggerTime) - alerts.Min(a => a.TriggerTime)).Days) : 0,
            MostAlertingKpi = alerts.GroupBy(a => a.KpiId.ToString())
                .OrderByDescending(g => g.Count())
                .FirstOrDefault()?.Key ?? "None",
            PeakAlertHour = alerts.GroupBy(a => a.TriggerTime.Hour)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault()?.Key ?? 0
        };
    }

    private async Task<SystemMetrics> GenerateSystemMetricsAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
    {
        var systemStatus = await _context.SystemStatus.FirstOrDefaultAsync(cancellationToken);
        
        return new SystemMetrics
        {
            ServiceUptime = systemStatus?.LastHeartbeat != null ? 
                DateTime.UtcNow - systemStatus.LastHeartbeat : TimeSpan.Zero,
            TotalKpisProcessed = systemStatus?.ProcessedKpis ?? 0,
            TotalAlertsSent = systemStatus?.AlertsSent ?? 0,
            SystemStatus = systemStatus?.Status ?? "Unknown"
        };
    }

    private async Task<List<Dictionary<string, object>>> ExecuteCustomQueryAsync(string query, Dictionary<string, object> parameters, CancellationToken cancellationToken)
    {
        // This is a simplified implementation. In production, you'd want proper query validation and security
        // Consider using a query builder or predefined query templates for security
        throw new NotImplementedException("Custom query execution requires additional security implementation");
    }

    private string DetermineAlertSeverity(AlertLog alert)
    {
        var deviation = Math.Abs(alert.DeviationPercent ?? 0);
        return deviation switch
        {
            >= 50 => "Emergency",
            >= 30 => "Critical",
            >= 20 => "High",
            >= 10 => "Medium",
            _ => "Low"
        };
    }

    private DateTime? CalculateNextRun(string cronExpression)
    {
        // Simplified cron calculation - in production, use a proper cron library like NCrontab
        return DateTime.UtcNow.AddHours(1);
    }

    private async Task<byte[]> GeneratePdfReportAsync(object reportData, string title)
    {
        // PDF generation implementation would go here
        // Consider using libraries like iTextSharp, PdfSharp, or DinkToPdf
        throw new NotImplementedException("PDF generation requires additional PDF library implementation");
    }

    private async Task<byte[]> GenerateExcelReportAsync(object reportData, string title)
    {
        // Excel generation implementation would go here
        // Consider using libraries like EPPlus, ClosedXML, or NPOI
        throw new NotImplementedException("Excel generation requires additional Excel library implementation");
    }

    private byte[] GenerateCsvReport(object reportData)
    {
        // CSV generation implementation
        var csv = new StringBuilder();
        csv.AppendLine($"Report Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
        csv.AppendLine($"Report Data: {JsonSerializer.Serialize(reportData)}");
        
        return Encoding.UTF8.GetBytes(csv.ToString());
    }
}

// Supporting classes for reporting
public class KpiReportData
{
    public string ReportPeriod { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public int KpiCount { get; set; }
    public int TotalExecutions { get; set; }
    public int TotalAlerts { get; set; }
    public List<KpiSummary> KpiSummaries { get; set; } = new();
    public List<KpiTrendSummary> TrendAnalysis { get; set; } = new();
}

public class AlertReportData
{
    public string ReportPeriod { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public int TotalAlerts { get; set; }
    public int ResolvedAlerts { get; set; }
    public int UnresolvedAlerts { get; set; }
    public Dictionary<string, int> AlertsByKpi { get; set; } = new();
    public Dictionary<string, int> AlertsBySeverity { get; set; } = new();
    public List<AlertLog> AlertDetails { get; set; } = new();
    public AlertStatistics? Statistics { get; set; }
}

public class PerformanceReportData
{
    public string ReportPeriod { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public string ReportType { get; set; } = string.Empty;
    public List<KpiPerformanceMetrics> PerformanceMetrics { get; set; } = new();
    public SystemMetrics? SystemMetrics { get; set; }
}

public class CustomReportData
{
    public string ReportName { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public string Query { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public List<Dictionary<string, object>> Results { get; set; } = new();
}

public class KpiSummary
{
    public int KpiId { get; set; }
    public string Indicator { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public int ExecutionCount { get; set; }
    public double SuccessRate { get; set; }
    public int AlertCount { get; set; }
    public decimal AverageValue { get; set; }
    public DateTime? LastExecution { get; set; }
}

public class KpiTrendSummary
{
    public int KpiId { get; set; }
    public string Indicator { get; set; } = string.Empty;
    public string TrendDirection { get; set; } = string.Empty;
    public double TrendStrength { get; set; }
    public double Volatility { get; set; }
}

public class AlertStatistics
{
    public int TotalAlerts { get; set; }
    public double AverageAlertsPerDay { get; set; }
    public string MostAlertingKpi { get; set; } = string.Empty;
    public int PeakAlertHour { get; set; }
}

public class SystemMetrics
{
    public TimeSpan ServiceUptime { get; set; }
    public int TotalKpisProcessed { get; set; }
    public int TotalAlertsSent { get; set; }
    public string SystemStatus { get; set; } = string.Empty;
}
