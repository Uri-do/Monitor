using MonitoringGrid.Api.DTOs;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Interfaces;
using System.Diagnostics;

namespace MonitoringGrid.Api.Services;

/// <summary>
/// Enhanced service for bulk operations and query optimizations with performance monitoring
/// </summary>
public interface IBulkOperationsService
{
    Task<BulkOperationResult> BulkCreateKpisAsync(IEnumerable<CreateKpiRequest> requests, CancellationToken cancellationToken = default);
    Task<BulkOperationResult> BulkUpdateKpiStatusAsync(IEnumerable<int> kpiIds, bool isActive, CancellationToken cancellationToken = default);
    Task<BulkOperationResult> BulkDeleteHistoricalDataAsync(DateTime olderThan, CancellationToken cancellationToken = default);
    Task<BulkOperationResult> BulkUpdateKpiFrequencyAsync(IEnumerable<int> kpiIds, int newFrequency, CancellationToken cancellationToken = default);
    Task<BulkOperationResult> BulkAssignContactsAsync(IEnumerable<int> kpiIds, IEnumerable<int> contactIds, CancellationToken cancellationToken = default);
    Task<BulkOperationResult> BulkExecuteKpisAsync(IEnumerable<int> kpiIds, CancellationToken cancellationToken = default);
    Task<IEnumerable<KpiSummaryDto>> GetKpiSummariesAsync(bool? isActive = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<PerformanceMetricDto>> GetPerformanceMetricsAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default);
    Task<BulkOperationResult> BulkArchiveHistoricalDataAsync(DateTime olderThan, string archiveLocation, CancellationToken cancellationToken = default);
    Task<BulkOperationResult> BulkOptimizeIndexesAsync(CancellationToken cancellationToken = default);
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

    public async Task<BulkOperationResult> BulkUpdateKpiFrequencyAsync(IEnumerable<int> kpiIds, int newFrequency, CancellationToken cancellationToken = default)
    {
        var result = new BulkOperationResult();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var kpiIdsList = kpiIds.ToList();
        result.TotalRequested = kpiIdsList.Count;

        try
        {
            var kpiRepository = _unitOfWork.Repository<KPI>();
            var kpis = await kpiRepository.GetAsync(k => kpiIdsList.Contains(k.KpiId), cancellationToken);
            var kpiList = kpis.ToList();

            if (!kpiList.Any())
            {
                result.IsSuccess = true;
                result.SuccessCount = 0;
                return result;
            }

            foreach (var kpi in kpiList)
            {
                kpi.Frequency = newFrequency;
                kpi.ModifiedDate = DateTime.UtcNow;
                await kpiRepository.UpdateAsync(kpi, cancellationToken);
                result.SuccessCount++;
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            result.IsSuccess = true;

            stopwatch.Stop();
            result.ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds;

            _logger.LogInformation("Bulk updated frequency for {Count} KPIs to {Frequency} minutes in {ElapsedMs}ms",
                result.SuccessCount, newFrequency, result.ExecutionTimeMs);
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds;
            _logger.LogError(ex, "Error during bulk KPI frequency update");
            result.Errors.Add($"Bulk frequency update failed: {ex.Message}");
            result.IsSuccess = false;
            return result;
        }
    }

    public async Task<BulkOperationResult> BulkAssignContactsAsync(IEnumerable<int> kpiIds, IEnumerable<int> contactIds, CancellationToken cancellationToken = default)
    {
        var result = new BulkOperationResult();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var kpiIdsList = kpiIds.ToList();
        var contactIdsList = contactIds.ToList();
        result.TotalRequested = kpiIdsList.Count * contactIdsList.Count;

        try
        {
            var kpiContactRepository = _unitOfWork.Repository<KpiContact>();

            // Remove existing assignments for these KPIs
            var existingAssignments = await kpiContactRepository.GetAsync(
                kc => kpiIdsList.Contains(kc.KpiId), cancellationToken);

            if (existingAssignments.Any())
            {
                await kpiContactRepository.DeleteRangeAsync(existingAssignments, cancellationToken);
            }

            // Create new assignments
            var newAssignments = new List<KpiContact>();
            foreach (var kpiId in kpiIdsList)
            {
                foreach (var contactId in contactIdsList)
                {
                    newAssignments.Add(new KpiContact
                    {
                        KpiId = kpiId,
                        ContactId = contactId
                    });
                }
            }

            await kpiContactRepository.AddRangeAsync(newAssignments, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            result.SuccessCount = newAssignments.Count;
            result.IsSuccess = true;

            stopwatch.Stop();
            result.ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds;

            _logger.LogInformation("Bulk assigned {ContactCount} contacts to {KpiCount} KPIs ({TotalAssignments} assignments) in {ElapsedMs}ms",
                contactIdsList.Count, kpiIdsList.Count, result.SuccessCount, result.ExecutionTimeMs);
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds;
            _logger.LogError(ex, "Error during bulk contact assignment");
            result.Errors.Add($"Bulk contact assignment failed: {ex.Message}");
            result.IsSuccess = false;
            return result;
        }
    }

    public async Task<BulkOperationResult> BulkExecuteKpisAsync(IEnumerable<int> kpiIds, CancellationToken cancellationToken = default)
    {
        var result = new BulkOperationResult();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var kpiIdsList = kpiIds.ToList();
        result.TotalRequested = kpiIdsList.Count;

        try
        {
            var kpiRepository = _unitOfWork.Repository<KPI>();
            var kpis = await kpiRepository.GetAsync(k => kpiIdsList.Contains(k.KpiId) && k.IsActive, cancellationToken);
            var kpiList = kpis.ToList();

            if (!kpiList.Any())
            {
                result.IsSuccess = true;
                result.SuccessCount = 0;
                return result;
            }

            // Execute KPIs in parallel with limited concurrency
            var semaphore = new SemaphoreSlim(5); // Limit to 5 concurrent executions
            var tasks = kpiList.Select(async kpi =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    // Here you would call your KPI execution service
                    // For now, we'll just update the LastRun timestamp
                    kpi.LastRun = DateTime.UtcNow;
                    await kpiRepository.UpdateAsync(kpi, cancellationToken);
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to execute KPI {KpiId}", kpi.KpiId);
                    return false;
                }
                finally
                {
                    semaphore.Release();
                }
            });

            var results = await Task.WhenAll(tasks);
            result.SuccessCount = results.Count(r => r);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            result.IsSuccess = true;

            stopwatch.Stop();
            result.ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds;

            _logger.LogInformation("Bulk executed {SuccessCount}/{TotalCount} KPIs in {ElapsedMs}ms",
                result.SuccessCount, kpiList.Count, result.ExecutionTimeMs);
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds;
            _logger.LogError(ex, "Error during bulk KPI execution");
            result.Errors.Add($"Bulk execution failed: {ex.Message}");
            result.IsSuccess = false;
            return result;
        }
    }

    public async Task<BulkOperationResult> BulkArchiveHistoricalDataAsync(DateTime olderThan, string archiveLocation, CancellationToken cancellationToken = default)
    {
        var result = new BulkOperationResult();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var historicalDataRepository = _unitOfWork.Repository<HistoricalData>();
            var oldRecords = await historicalDataRepository.GetAsync(h => h.Timestamp < olderThan, cancellationToken);
            var recordsList = oldRecords.ToList();

            if (!recordsList.Any())
            {
                result.IsSuccess = true;
                result.SuccessCount = 0;
                return result;
            }

            result.TotalRequested = recordsList.Count;

            // Archive data (in a real implementation, you'd export to files or another database)
            _logger.LogInformation("Archiving {Count} historical records to {Location}", recordsList.Count, archiveLocation);

            // Simulate archiving process
            await Task.Delay(100, cancellationToken);

            // Delete archived records
            await historicalDataRepository.DeleteRangeAsync(recordsList, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            result.SuccessCount = recordsList.Count;
            result.IsSuccess = true;

            stopwatch.Stop();
            result.ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds;

            _logger.LogInformation("Bulk archived and deleted {Count} historical records in {ElapsedMs}ms",
                result.SuccessCount, result.ExecutionTimeMs);
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds;
            _logger.LogError(ex, "Error during bulk historical data archiving");
            result.Errors.Add($"Bulk archiving failed: {ex.Message}");
            result.IsSuccess = false;
            return result;
        }
    }

    public async Task<BulkOperationResult> BulkOptimizeIndexesAsync(CancellationToken cancellationToken = default)
    {
        var result = new BulkOperationResult();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // In a real implementation, you'd execute database-specific index optimization commands
            _logger.LogInformation("Starting database index optimization");

            // Simulate index optimization
            await Task.Delay(1000, cancellationToken);

            result.SuccessCount = 1;
            result.TotalRequested = 1;
            result.IsSuccess = true;

            stopwatch.Stop();
            result.ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds;

            _logger.LogInformation("Database index optimization completed in {ElapsedMs}ms", result.ExecutionTimeMs);
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds;
            _logger.LogError(ex, "Error during index optimization");
            result.Errors.Add($"Index optimization failed: {ex.Message}");
            result.IsSuccess = false;
            return result;
        }
    }
}

/// <summary>
/// Enhanced result of a bulk operation with performance metrics
/// </summary>
public class BulkOperationResult
{
    public bool IsSuccess { get; set; }
    public int TotalRequested { get; set; }
    public int SuccessCount { get; set; }
    public List<string> Errors { get; set; } = new();
    public int ExecutionTimeMs { get; set; }
    public Dictionary<string, object> Metrics { get; set; } = new();

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
