using Microsoft.Extensions.Logging;
using MonitoringGrid.Core.Common;
using MonitoringGrid.Core.DTOs;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Core.Models;
using static MonitoringGrid.Core.Interfaces.IAlertService;

namespace MonitoringGrid.Infrastructure.Services;

/// <summary>
/// Service for managing alerts
/// </summary>
public class AlertService : IAlertService
{
    private readonly IAlertRepository _alertRepository;
    private readonly ILogger<AlertService> _logger;

    public AlertService(
        IAlertRepository alertRepository,
        ILogger<AlertService> logger)
    {
        _alertRepository = alertRepository;
        _logger = logger;
    }

    public async Task<Result<PaginatedAlertsDto>> GetAlertsAsync(AlertFilterDto filter, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting alerts with filter: {@Filter}", filter);

            // Convert DTO to Model
            var alertFilter = new AlertFilter
            {
                StartDate = filter.StartDate,
                EndDate = filter.EndDate,
                IndicatorIds = filter.IndicatorIds,
                Owners = filter.Owners,
                IsResolved = filter.IsResolved,
                SentVia = filter.SentVia?.Select(sv => sv.ToString()).ToList(),
                MinDeviation = filter.MinDeviation,
                MaxDeviation = filter.MaxDeviation,
                SearchText = filter.SearchText,
                Page = filter.Page,
                PageSize = filter.PageSize,
                SortBy = filter.SortBy,
                SortDirection = filter.SortDirection
            };

            var alertsResult = await _alertRepository.GetAlertsWithFilteringAsync(alertFilter);

            var alertDtos = alertsResult.Alerts.Select(alert => new AlertLogDto
            {
                AlertId = alert.AlertId,
                IndicatorId = alert.IndicatorId,
                IndicatorName = alert.Indicator?.IndicatorName ?? "Unknown",
                IndicatorOwner = alert.Indicator?.OwnerContactId.ToString() ?? "Unknown",
                TriggerTime = alert.TriggerTime,
                Message = alert.Message ?? "No message",
                Details = alert.Details,
                SentVia = alert.SentVia,
                SentTo = alert.SentTo,
                CurrentValue = alert.CurrentValue,
                HistoricalValue = alert.HistoricalValue,
                DeviationPercent = alert.DeviationPercent,
                IsResolved = alert.IsResolved,
                ResolvedTime = alert.ResolvedTime,
                ResolvedBy = alert.ResolvedBy
            }).ToList();

            var result = new PaginatedAlertsDto
            {
                Alerts = alertDtos,
                TotalCount = alertsResult.TotalCount,
                Page = filter.Page,
                PageSize = filter.PageSize,
                TotalPages = (int)Math.Ceiling((double)alertsResult.TotalCount / filter.PageSize),
                HasNextPage = filter.Page * filter.PageSize < alertsResult.TotalCount,
                HasPreviousPage = filter.Page > 1
            };

            return Result.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get alerts with filter: {@Filter}", filter);
            return Result.Failure<PaginatedAlertsDto>(Error.Failure("Alert.RetrievalError", "Failed to retrieve alerts"));
        }
    }

    public async Task<Result<AlertLogDto>> GetAlertByIdAsync(long alertId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting alert by ID: {AlertId}", alertId);

            var alert = await _alertRepository.GetByIdAsync(alertId, cancellationToken);
            if (alert == null)
            {
                return Result.Failure<AlertLogDto>(Error.NotFound("Alert", alertId));
            }

            var alertDto = new AlertLogDto
            {
                AlertId = alert.AlertId,
                IndicatorId = alert.IndicatorId,
                IndicatorName = alert.Indicator?.IndicatorName ?? "Unknown",
                IndicatorOwner = alert.Indicator?.OwnerContactId.ToString() ?? "Unknown",
                TriggerTime = alert.TriggerTime,
                Message = alert.Message ?? "No message",
                Details = alert.Details,
                SentVia = alert.SentVia,
                SentTo = alert.SentTo,
                CurrentValue = alert.CurrentValue,
                HistoricalValue = alert.HistoricalValue,
                DeviationPercent = alert.DeviationPercent,
                IsResolved = alert.IsResolved,
                ResolvedTime = alert.ResolvedTime,
                ResolvedBy = alert.ResolvedBy
            };

            return Result.Success(alertDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get alert by ID: {AlertId}", alertId);
            return Result.Failure<AlertLogDto>(Error.Failure("Alert.RetrievalError", "Failed to retrieve alert"));
        }
    }

    public async Task<Result<AlertLogDto>> CreateAlertAsync(CreateAlertRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Creating alert for indicator: {IndicatorId}", request.IndicatorID);

            var alert = new AlertLog
            {
                IndicatorId = request.IndicatorID,
                TriggerTime = DateTime.UtcNow,
                Message = request.AlertMessage,
                CurrentValue = request.TriggerValue,
                HistoricalValue = request.ThresholdValue,
                IsResolved = false,
                SentTo = "System",
                SentVia = 1, // System
                Details = request.AdditionalData
            };

            await _alertRepository.AddAsync(alert, cancellationToken);

            var alertDto = new AlertLogDto
            {
                AlertId = alert.AlertId,
                IndicatorId = alert.IndicatorId,
                TriggerTime = alert.TriggerTime,
                Message = alert.Message ?? "No message",
                Details = alert.Details,
                SentVia = alert.SentVia,
                SentTo = alert.SentTo,
                CurrentValue = alert.CurrentValue,
                HistoricalValue = alert.HistoricalValue,
                IsResolved = alert.IsResolved
            };

            return Result.Success(alertDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create alert for indicator: {IndicatorId}", request.IndicatorID);
            return Result.Failure<AlertLogDto>(Error.Failure("Alert.CreateError", "Failed to create alert"));
        }
    }

    public async Task<Result<bool>> ResolveAlertAsync(long alertId, ResolveAlertRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Resolving alert {AlertId} by {ResolvedBy}", alertId, request.ResolvedBy);

            var alert = await _alertRepository.GetByIdAsync(alertId, cancellationToken);
            if (alert == null)
            {
                return Result.Failure<bool>(Error.NotFound("Alert", alertId));
            }

            if (alert.IsResolved)
            {
                return Result.Failure<bool>(Error.BusinessRule("Alert.AlreadyResolved", "Alert is already resolved"));
            }

            alert.IsResolved = true;
            alert.ResolvedTime = DateTime.UtcNow;
            alert.ResolvedBy = request.ResolvedBy;

            await _alertRepository.UpdateAsync(alert, cancellationToken);

            return Result.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve alert {AlertId}", alertId);
            return Result.Failure<bool>(Error.Failure("Alert.ResolveError", "Failed to resolve alert"));
        }
    }

    public async Task<Result<int>> BulkResolveAlertsAsync(BulkResolveAlertsRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Bulk resolving {Count} alerts by {ResolvedBy}", request.AlertIds.Count, request.ResolvedBy);

            int resolvedCount = 0;
            foreach (var alertId in request.AlertIds)
            {
                var alert = await _alertRepository.GetByIdAsync(alertId, cancellationToken);
                if (alert != null && !alert.IsResolved)
                {
                    alert.IsResolved = true;
                    alert.ResolvedTime = DateTime.UtcNow;
                    alert.ResolvedBy = request.ResolvedBy;
                    await _alertRepository.UpdateAsync(alert, cancellationToken);
                    resolvedCount++;
                }
            }

            return Result.Success(resolvedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to bulk resolve alerts");
            return Result.Failure<int>(Error.Failure("Alert.BulkResolveError", "Failed to bulk resolve alerts"));
        }
    }

    public async Task<Result<AlertStatisticsDto>> GetAlertStatisticsAsync(int days = 30, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting alert statistics for {Days} days", days);

            // This would need to be implemented based on your specific requirements
            // For now, returning a basic implementation
            var statistics = new AlertStatisticsDto
            {
                TotalAlerts = 0,
                ResolvedAlerts = 0,
                UnresolvedAlerts = 0,
                CriticalAlerts = 0,
                HighPriorityAlerts = 0,
                AlertsToday = 0,
                AlertsThisWeek = 0,
                AlertsThisMonth = 0,
                AverageResolutionTimeHours = 0
            };

            return Result.Success(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get alert statistics");
            return Result.Failure<AlertStatisticsDto>(Error.Failure("Alert.StatisticsError", "Failed to get alert statistics"));
        }
    }

    public async Task<Result<AlertDashboardDto>> GetAlertDashboardAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting alert dashboard");

            // This would need to be implemented based on your specific requirements
            // For now, returning a basic implementation
            var dashboard = new AlertDashboardDto
            {
                TotalAlertsToday = 0,
                UnresolvedAlerts = 0,
                CriticalAlerts = 0,
                AlertsLastHour = 0,
                AlertTrendPercentage = 0,
                HourlyTrend = new List<AlertTrendDto>()
            };

            return Result.Success(dashboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get alert dashboard");
            return Result.Failure<AlertDashboardDto>(Error.Failure("Alert.DashboardError", "Failed to get alert dashboard"));
        }
    }

    public async Task<Result<List<AlertLogDto>>> GetAlertsByIndicatorAsync(long indicatorId, int days = 30, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting alerts for indicator {IndicatorId} for {Days} days", indicatorId, days);

            var filter = new AlertFilter
            {
                IndicatorIds = new List<long> { indicatorId },
                Page = 1,
                PageSize = 1000, // Get all alerts for the indicator
                StartDate = DateTime.UtcNow.AddDays(-days),
                EndDate = DateTime.UtcNow
            };

            var alertsResult = await _alertRepository.GetAlertsWithFilteringAsync(filter);

            var alertDtos = alertsResult.Alerts.Select(alert => new AlertLogDto
            {
                AlertId = alert.AlertId,
                IndicatorId = alert.IndicatorId,
                IndicatorName = alert.Indicator?.IndicatorName ?? "Unknown",
                IndicatorOwner = alert.Indicator?.OwnerContactId.ToString() ?? "Unknown",
                TriggerTime = alert.TriggerTime,
                Message = alert.Message ?? "No message",
                Details = alert.Details,
                SentVia = alert.SentVia,
                SentTo = alert.SentTo,
                CurrentValue = alert.CurrentValue,
                HistoricalValue = alert.HistoricalValue,
                DeviationPercent = alert.DeviationPercent,
                IsResolved = alert.IsResolved,
                ResolvedTime = alert.ResolvedTime,
                ResolvedBy = alert.ResolvedBy
            }).ToList();

            return Result.Success(alertDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get alerts for indicator {IndicatorId}", indicatorId);
            return Result.Failure<List<AlertLogDto>>(Error.Failure("Alert.RetrievalError", "Failed to get alerts for indicator"));
        }
    }

    public async Task<Result<int>> GetUnresolvedAlertsCountAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting unresolved alerts count");

            var filter = new AlertFilter
            {
                IsResolved = false,
                Page = 1,
                PageSize = 1
            };

            var alertsResult = await _alertRepository.GetAlertsWithFilteringAsync(filter);
            return Result.Success(alertsResult.TotalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get unresolved alerts count");
            return Result.Failure<int>(Error.Failure("Alert.CountError", "Failed to get unresolved alerts count"));
        }
    }

    public async Task<Result<bool>> ProcessIndicatorExecutionAsync(long indicatorId, decimal value, bool thresholdExceeded, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Processing indicator execution for {IndicatorId}, value: {Value}, threshold exceeded: {ThresholdExceeded}", 
                indicatorId, value, thresholdExceeded);

            if (thresholdExceeded)
            {
                var createRequest = new CreateAlertRequest
                {
                    IndicatorID = indicatorId,
                    AlertMessage = $"Indicator threshold exceeded. Current value: {value}",
                    Severity = "High",
                    TriggerValue = value
                };

                var result = await CreateAlertAsync(createRequest, cancellationToken);
                return result.IsSuccess ? Result.Success(true) : Result.Failure<bool>(result.Error);
            }

            return Result.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process indicator execution for {IndicatorId}", indicatorId);
            return Result.Failure<bool>(Error.Failure("Alert.ProcessError", "Failed to process indicator execution"));
        }
    }
}
