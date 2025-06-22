using MonitoringGrid.Core.Models;
using MonitoringGrid.Core.Entities;

namespace MonitoringGrid.Core.Interfaces;

/// <summary>
/// Interface for reporting service
/// </summary>
public interface IReportingService
{
    Task<byte[]> GenerateIndicatorReportAsync(IndicatorReportRequest request, CancellationToken cancellationToken = default);
    Task<byte[]> GenerateAlertReportAsync(AlertReportRequest request, CancellationToken cancellationToken = default);
    Task<byte[]> GeneratePerformanceReportAsync(PerformanceReportRequest request, CancellationToken cancellationToken = default);
    Task<byte[]> GenerateCustomReportAsync(CustomReportRequest request, CancellationToken cancellationToken = default);
    Task<List<Entities.ReportTemplate>> GetReportTemplatesAsync(CancellationToken cancellationToken = default);
    Task<Entities.ReportSchedule> ScheduleReportAsync(ReportScheduleRequest request, CancellationToken cancellationToken = default);
}
