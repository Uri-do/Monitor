using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using MonitoringGrid.Api.CQRS.Queries.Indicator;
using MonitoringGrid.Api.DTOs;
using MonitoringGrid.Api.Common;
using MonitoringGrid.Core.Interfaces;

namespace MonitoringGrid.Api.CQRS.Handlers.Indicator;

/// <summary>
/// Handler for getting indicators with filtering
/// </summary>
public class GetIndicatorsQueryHandler : IRequestHandler<GetIndicatorsQuery, Result<List<IndicatorDto>>>
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

    public async Task<Result<List<IndicatorDto>>> Handle(GetIndicatorsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Getting indicators with filters - IsActive: {IsActive}, Priority: {Priority}, Owner: {OwnerContactId}",
                request.IsActive, request.Priority, request.OwnerContactId);

            List<Core.Entities.Indicator> indicators;

            // Apply filters
            if (request.OwnerContactId.HasValue)
            {
                indicators = await _indicatorService.GetIndicatorsByOwnerAsync(request.OwnerContactId.Value, cancellationToken);
            }
            else if (!string.IsNullOrEmpty(request.Priority))
            {
                indicators = await _indicatorService.GetIndicatorsByPriorityAsync(request.Priority, cancellationToken);
            }
            else if (request.IsActive.HasValue && request.IsActive.Value)
            {
                indicators = await _indicatorService.GetActiveIndicatorsAsync(cancellationToken);
            }
            else
            {
                indicators = await _indicatorService.GetAllIndicatorsAsync(cancellationToken);
            }

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

            var indicatorDtos = _mapper.Map<List<IndicatorDto>>(indicators);

            _logger.LogDebug("Retrieved {Count} indicators (total: {TotalCount})", 
                indicatorDtos.Count, totalCount);

            return Result<List<IndicatorDto>>.Success(indicatorDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get indicators");
            return Result.Failure<List<IndicatorDto>>("GET_FAILED", $"Failed to get indicators: {ex.Message}");
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
