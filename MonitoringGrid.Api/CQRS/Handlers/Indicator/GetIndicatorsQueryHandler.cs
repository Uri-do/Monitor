using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using MonitoringGrid.Api.CQRS.Queries.Indicator;
using MonitoringGrid.Api.DTOs.Indicators;
using MonitoringGrid.Core.Common;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Core.Models;

namespace MonitoringGrid.Api.CQRS.Handlers.Indicator;

/// <summary>
/// Handler for getting indicators with filtering
/// </summary>
public class GetIndicatorsQueryHandler : IRequestHandler<GetIndicatorsQuery, Result<List<IndicatorResponse>>>
{
    private readonly IIndicatorService _indicatorService;
    private readonly IMapper _mapper;
    private readonly ILogger<GetIndicatorsQueryHandler> _logger;

    public GetIndicatorsQueryHandler(
        IIndicatorService indicatorService,
        IMapper mapper,
        ILogger<GetIndicatorsQueryHandler> logger)
    {
        _indicatorService = indicatorService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<List<IndicatorResponse>>> Handle(GetIndicatorsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Getting indicators with filters - IsActive: {IsActive}, Priority: {Priority}, Owner: {OwnerContactId}",
                request.IsActive, request.Priority, request.OwnerContactId);

            List<Core.Entities.Indicator> indicators;

            // Apply filters - get all indicators first, then filter in memory for now
            var filterOptions = new IndicatorFilterOptions();
            var allIndicatorsResult = await _indicatorService.GetAllIndicatorsAsync(filterOptions, cancellationToken);
            if (!allIndicatorsResult.IsSuccess)
            {
                return Result.Failure<List<IndicatorResponse>>("GET_FAILED", allIndicatorsResult.Error?.Message ?? "Failed to get indicators");
            }

            indicators = allIndicatorsResult.Value.Items.ToList();

            // Apply additional filters
            if (request.IsActive.HasValue)
            {
                indicators = indicators.Where(i => i.IsActive == request.IsActive.Value).ToList();
            }

            if (request.CollectorId.HasValue)
            {
                indicators = indicators.Where(i => i.CollectorID == request.CollectorId.Value).ToList();
            }

            if (!string.IsNullOrEmpty(request.ThresholdType))
            {
                indicators = indicators.Where(i => i.ThresholdType == request.ThresholdType).ToList();
            }

            if (request.IsCurrentlyRunning.HasValue)
            {
                indicators = indicators.Where(i => i.IsCurrentlyRunning == request.IsCurrentlyRunning.Value).ToList();
            }

            if (request.LastRunAfter.HasValue)
            {
                indicators = indicators.Where(i => i.LastRun >= request.LastRunAfter.Value).ToList();
            }

            if (request.LastRunBefore.HasValue)
            {
                indicators = indicators.Where(i => i.LastRun <= request.LastRunBefore.Value).ToList();
            }

            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                var searchTerm = request.SearchTerm.ToLower();
                indicators = indicators.Where(i => 
                    i.IndicatorName.ToLower().Contains(searchTerm) ||
                    i.IndicatorCode.ToLower().Contains(searchTerm) ||
                    (i.IndicatorDesc?.ToLower().Contains(searchTerm) ?? false)).ToList();
            }

            // Apply sorting
            indicators = ApplySorting(indicators, request.SortBy, request.SortDirection);

            // Apply pagination
            var totalCount = indicators.Count;
            var skip = (request.Page - 1) * request.PageSize;
            indicators = indicators.Skip(skip).Take(request.PageSize).ToList();

            var indicatorDtos = _mapper.Map<List<IndicatorResponse>>(indicators);

            _logger.LogDebug("Retrieved {Count} indicators (total: {TotalCount})", 
                indicatorDtos.Count, totalCount);

            return Result<List<IndicatorResponse>>.Success(indicatorDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get indicators");
            return Result.Failure<List<IndicatorResponse>>("GET_FAILED", $"Failed to get indicators: {ex.Message}");
        }
    }

    private static List<Core.Entities.Indicator> ApplySorting(List<Core.Entities.Indicator> indicators, 
        string sortBy, string sortDirection)
    {
        var isDescending = sortDirection.ToLower() == "desc";

        return sortBy.ToLower() switch
        {
            "indicatorname" => isDescending 
                ? indicators.OrderByDescending(i => i.IndicatorName).ToList()
                : indicators.OrderBy(i => i.IndicatorName).ToList(),
            "indicatorcode" => isDescending 
                ? indicators.OrderByDescending(i => i.IndicatorCode).ToList()
                : indicators.OrderBy(i => i.IndicatorCode).ToList(),
            "priority" => isDescending 
                ? indicators.OrderByDescending(i => i.Priority).ToList()
                : indicators.OrderBy(i => i.Priority).ToList(),
            "lastrun" => isDescending 
                ? indicators.OrderByDescending(i => i.LastRun ?? DateTime.MinValue).ToList()
                : indicators.OrderBy(i => i.LastRun ?? DateTime.MinValue).ToList(),
            "createddate" => isDescending 
                ? indicators.OrderByDescending(i => i.CreatedDate).ToList()
                : indicators.OrderBy(i => i.CreatedDate).ToList(),
            "updateddate" => isDescending 
                ? indicators.OrderByDescending(i => i.UpdatedDate).ToList()
                : indicators.OrderBy(i => i.UpdatedDate).ToList(),
            "isactive" => isDescending 
                ? indicators.OrderByDescending(i => i.IsActive).ToList()
                : indicators.OrderBy(i => i.IsActive).ToList(),
            _ => indicators.OrderBy(i => i.IndicatorName).ToList()
        };
    }
}
