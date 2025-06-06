using MonitoringGrid.Core.Models;
using MonitoringGrid.Core.Entities;

namespace MonitoringGrid.Core.Interfaces;

/// <summary>
/// Interface for data export service
/// </summary>
public interface IDataExportService
{
    Task<byte[]> ExportKpiDataAsync(DataExportRequest request, CancellationToken cancellationToken = default);
    Task<byte[]> ExportAlertDataAsync(DataExportRequest request, CancellationToken cancellationToken = default);
    Task<byte[]> ExportAuditDataAsync(DataExportRequest request, CancellationToken cancellationToken = default);
    Task<Entities.ExportJob> ScheduleExportAsync(ScheduledExportRequest request, CancellationToken cancellationToken = default);
    Task<List<Entities.ExportJob>> GetExportJobsAsync(string? userId = null, CancellationToken cancellationToken = default);
}
