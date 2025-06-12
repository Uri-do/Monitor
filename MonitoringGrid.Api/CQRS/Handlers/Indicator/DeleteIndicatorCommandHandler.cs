using MediatR;
using Microsoft.Extensions.Logging;
using MonitoringGrid.Api.CQRS.Commands.Indicator;
using MonitoringGrid.Api.Common;
using MonitoringGrid.Core.Events;
using MonitoringGrid.Core.Interfaces;

namespace MonitoringGrid.Api.CQRS.Handlers.Indicator;

/// <summary>
/// Handler for deleting indicators
/// </summary>
public class DeleteIndicatorCommandHandler : IRequestHandler<DeleteIndicatorCommand, Result<bool>>
{
    private readonly IIndicatorService _indicatorService;
    private readonly ILogger<DeleteIndicatorCommandHandler> _logger;

    public DeleteIndicatorCommandHandler(
        IIndicatorService indicatorService,
        ILogger<DeleteIndicatorCommandHandler> logger)
    {
        _indicatorService = indicatorService;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(DeleteIndicatorCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Deleting indicator {IndicatorId}", request.IndicatorId);

            // Get the indicator to validate it exists and get details for the event
            var indicator = await _indicatorService.GetIndicatorByIdAsync(request.IndicatorId, cancellationToken);
            if (indicator == null)
            {
                return Result.Failure<bool>("INDICATOR_NOT_FOUND", $"Indicator with ID {request.IndicatorId} not found");
            }

            // Check if indicator is currently running
            if (indicator.IsCurrentlyRunning)
            {
                return Result.Failure<bool>("INDICATOR_RUNNING", $"Cannot delete indicator '{indicator.IndicatorName}' while it is currently running");
            }

            // Add domain event before deletion
            indicator.AddDomainEvent(new IndicatorDeletedEvent(
                indicator.IndicatorId, 
                indicator.IndicatorName, 
                indicator.OwnerContact?.Name ?? "Unknown"));

            // Delete the indicator
            var success = await _indicatorService.DeleteIndicatorAsync(request.IndicatorId, cancellationToken);

            if (success)
            {
                _logger.LogInformation("Successfully deleted indicator {IndicatorId}: {IndicatorName}", 
                    indicator.IndicatorId, indicator.IndicatorName);
                return Result<bool>.Success(true);
            }
            else
            {
                _logger.LogWarning("Failed to delete indicator {IndicatorId}: {IndicatorName}", 
                    indicator.IndicatorId, indicator.IndicatorName);
                return Result.Failure<bool>("DELETE_FAILED", "Failed to delete indicator");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting indicator {IndicatorId}", request.IndicatorId);
            return Result.Failure<bool>("DELETE_ERROR", $"Failed to delete indicator: {ex.Message}");
        }
    }
}
