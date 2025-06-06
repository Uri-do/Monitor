using MonitoringGrid.Api.DTOs;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Interfaces;

namespace MonitoringGrid.Api.Services;

/// <summary>
/// Service for demonstrating bulk operations and query optimizations
/// </summary>
public interface IBulkOperationsService
{
    Task<BulkOperationResult> BulkCreateKpisAsync(IEnumerable<CreateKpiRequest> requests, CancellationToken cancellationToken = default);
    Task<BulkOperationResult> BulkUpdateKpiStatusAsync(IEnumerable<int> kpiIds, bool isActive, CancellationToken cancellationToken = default);
    Task<BulkOperationResult> BulkDeleteHistoricalDataAsync(DateTime olderThan, CancellationToken cancellationToken = default);
    Task<IEnumerable<KpiSummaryDto>> GetKpiSummariesAsync(bool? isActive = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<PerformanceMetricDto>> GetPerformanceMetricsAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default);
}

public class BulkOperationsService : IBulkOperationsService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<BulkOperationsService> _logger;

    public BulkOperationsService(IUnitOfWork unitOfWork, ILogger<BulkOperationsService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<BulkOperationResult> BulkCreateKpisAsync(IEnumerable<CreateKpiRequest> requests, CancellationToken cancellationToken = default)
    {
        var requestList = requests.ToList();
        var result = new BulkOperationResult { TotalRequested = requestList.Count };

        try
        {
            var kpiRepository = _unitOfWork.Repository<KPI>();
            var kpisToCreate = new List<KPI>();

            foreach (var request in requestList)
            {
                // Check for duplicates
                var exists = await kpiRepository.AnyAsync(k => k.Indicator == request.Indicator, cancellationToken);
                if (exists)
                {
                    result.Errors.Add($"KPI with indicator '{request.Indicator}' already exists");
                    continue;
                }

                var kpi = new KPI
                {
                    Indicator = request.Indicator,
                    SpName = request.SpName,
                    SubjectTemplate = request.SubjectTemplate,
                    DescriptionTemplate = request.DescriptionTemplate,
                    Deviation = request.Deviation,
                    Frequency = request.Frequency,
                    LastMinutes = request.LastMinutes,
                    Owner = request.Owner,
                    Priority = request.Priority,
                    CooldownMinutes = request.CooldownMinutes,
                    MinimumThreshold = request.MinimumThreshold,
                    IsActive = request.IsActive,
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow
                };

                kpisToCreate.Add(kpi);
            }

            if (kpisToCreate.Any())
            {
                var created = await kpiRepository.BulkInsertAsync(kpisToCreate, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                
                result.SuccessCount = created;
                _logger.LogInformation("Bulk created {Count} KPIs successfully", created);
            }

            result.IsSuccess = result.Errors.Count == 0;
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bulk KPI creation");
            result.Errors.Add($"Bulk operation failed: {ex.Message}");
            result.IsSuccess = false;
            return result;
        }
    }

    public async Task<BulkOperationResult> BulkUpdateKpiStatusAsync(IEnumerable<int> kpiIds, bool isActive, CancellationToken cancellationToken = default)
    {
        var idList = kpiIds.ToList();
        var result = new BulkOperationResult { TotalRequested = idList.Count };

        try
        {
            var kpiRepository = _unitOfWork.Repository<KPI>();
            
            // Get KPIs to update
            var kpisToUpdate = await kpiRepository.GetAsync(k => idList.Contains(k.KpiId), cancellationToken);
            var kpiList = kpisToUpdate.ToList();

            if (!kpiList.Any())
            {
                result.Errors.Add("No KPIs found with the specified IDs");
                result.IsSuccess = false;
                return result;
            }

            // Update status
            foreach (var kpi in kpiList)
            {
                kpi.IsActive = isActive;
                kpi.ModifiedDate = DateTime.UtcNow;
            }

            var updated = await kpiRepository.BulkUpdateAsync(kpiList, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            result.SuccessCount = updated;
            result.IsSuccess = true;
            
            _logger.LogInformation("Bulk updated {Count} KPIs to {Status}", updated, isActive ? "active" : "inactive");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bulk KPI status update");
            result.Errors.Add($"Bulk operation failed: {ex.Message}");
            result.IsSuccess = false;
            return result;
        }
    }

    public async Task<BulkOperationResult> BulkDeleteHistoricalDataAsync(DateTime olderThan, CancellationToken cancellationToken = default)
    {
        var result = new BulkOperationResult();

        try
        {
            var historicalDataRepository = _unitOfWork.Repository<HistoricalData>();
            
            // Count records to be deleted
            var countToDelete = await historicalDataRepository.CountAsync(h => h.Timestamp < olderThan, cancellationToken);
            result.TotalRequested = countToDelete;

            if (countToDelete == 0)
            {
                result.IsSuccess = true;
                result.SuccessCount = 0;
                return result;
            }

            // Delete in batches to avoid memory issues
            var deleted = await historicalDataRepository.BulkDeleteAsync(h => h.Timestamp < olderThan, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            result.SuccessCount = deleted;
            result.IsSuccess = true;
            
            _logger.LogInformation("Bulk deleted {Count} historical data records older than {Date}", deleted, olderThan);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bulk historical data deletion");
            result.Errors.Add($"Bulk operation failed: {ex.Message}");
            result.IsSuccess = false;
            return result;
        }
    }

    public async Task<IEnumerable<KpiSummaryDto>> GetKpiSummariesAsync(bool? isActive = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var kpiRepository = _unitOfWork.Repository<KPI>();
            
            // Use query projection to reduce data transfer
            var summaries = await kpiRepository.GetProjectedAsync(
                predicate: k => isActive == null || k.IsActive == isActive,
                selector: k => new KpiSummaryDto
                {
                    KpiId = k.KpiId,
                    Indicator = k.Indicator,
                    Owner = k.Owner,
                    Priority = k.Priority,
                    IsActive = k.IsActive,
                    LastRun = k.LastRun,
                    Frequency = k.Frequency,
                    CreatedDate = k.CreatedDate
                },
                cancellationToken);

            _logger.LogInformation("Retrieved {Count} KPI summaries with projection", summaries.Count());
            return summaries;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving KPI summaries");
            throw;
        }
    }

    public async Task<IEnumerable<PerformanceMetricDto>> GetPerformanceMetricsAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        try
        {
            var historicalDataRepository = _unitOfWork.Repository<HistoricalData>();
            
            // Use query projection for performance metrics
            var metrics = await historicalDataRepository.GetProjectedAsync(
                predicate: h => h.Timestamp >= from && h.Timestamp <= to,
                selector: h => new PerformanceMetricDto
                {
                    KpiId = h.KpiId,
                    Timestamp = h.Timestamp,
                    ExecutionTimeMs = h.ExecutionTimeMs ?? 0,
                    IsSuccessful = h.IsSuccessful,
                    DeviationPercent = h.DeviationPercent,
                    Value = h.Value
                },
                cancellationToken);

            var metricsList = metrics.ToList();
            _logger.LogInformation("Retrieved {Count} performance metrics from {From} to {To}",
                metricsList.Count, from, to);
            return metricsList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving performance metrics");
            throw;
        }
    }
}

/// <summary>
/// Result of a bulk operation
/// </summary>
public class BulkOperationResult
{
    public bool IsSuccess { get; set; }
    public int TotalRequested { get; set; }
    public int SuccessCount { get; set; }
    public List<string> Errors { get; set; } = new();
    
    public int FailureCount => TotalRequested - SuccessCount;
    public double SuccessRate => TotalRequested > 0 ? (double)SuccessCount / TotalRequested * 100 : 0;
}

/// <summary>
/// KPI summary DTO for optimized queries
/// </summary>
public class KpiSummaryDto
{
    public int KpiId { get; set; }
    public string Indicator { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public byte Priority { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastRun { get; set; }
    public int Frequency { get; set; }
    public DateTime CreatedDate { get; set; }
}

/// <summary>
/// Performance metric DTO for optimized queries
/// </summary>
public class PerformanceMetricDto
{
    public int KpiId { get; set; }
    public DateTime Timestamp { get; set; }
    public int ExecutionTimeMs { get; set; }
    public bool IsSuccessful { get; set; }
    public decimal? DeviationPercent { get; set; }
    public decimal? Value { get; set; }
}
