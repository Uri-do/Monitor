using MediatR;
using Microsoft.Extensions.Logging;
using MonitoringGrid.Api.DTOs;
using MonitoringGrid.Api.DTOs.Hubs;
using MonitoringGrid.Api.Hubs;
using MonitoringGrid.Core.Events;

namespace MonitoringGrid.Api.EventHandlers;

/// <summary>
/// Domain event handler for Indicator execution started events
/// </summary>
public class IndicatorExecutionStartedEventHandler : INotificationHandler<IndicatorExecutionStartedEvent>
{
    private readonly IRealtimeNotificationService _realtimeService;
    private readonly ILogger<IndicatorExecutionStartedEventHandler> _logger;

    public IndicatorExecutionStartedEventHandler(
        IRealtimeNotificationService realtimeService,
        ILogger<IndicatorExecutionStartedEventHandler> logger)
    {
        _realtimeService = realtimeService;
        _logger = logger;
    }

    public async Task Handle(IndicatorExecutionStartedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Handling IndicatorExecutionStartedEvent for Indicator {IndicatorId}", notification.IndicatorId);

            var dto = new IndicatorExecutionStartedDto
            {
                IndicatorID = notification.IndicatorId,
                IndicatorName = notification.IndicatorName,
                Owner = notification.Owner,
                StartTime = notification.OccurredOn.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                ExecutionContext = notification.ExecutionContext
            };

            await _realtimeService.SendIndicatorExecutionStartedAsync(dto);

            _logger.LogDebug("Successfully handled IndicatorExecutionStartedEvent for Indicator {IndicatorId}", notification.IndicatorId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle IndicatorExecutionStartedEvent for Indicator {IndicatorId}", notification.IndicatorId);
        }
    }
}

/// <summary>
/// Domain event handler for Indicator execution completed events
/// </summary>
public class IndicatorExecutionCompletedEventHandler : INotificationHandler<IndicatorExecutionCompletedEvent>
{
    private readonly IRealtimeNotificationService _realtimeService;
    private readonly ILogger<IndicatorExecutionCompletedEventHandler> _logger;

    public IndicatorExecutionCompletedEventHandler(
        IRealtimeNotificationService realtimeService,
        ILogger<IndicatorExecutionCompletedEventHandler> logger)
    {
        _realtimeService = realtimeService;
        _logger = logger;
    }

    public async Task Handle(IndicatorExecutionCompletedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Handling IndicatorExecutionCompletedEvent for Indicator {IndicatorId}", notification.IndicatorId);

            var dto = new IndicatorExecutionCompletedDto
            {
                IndicatorId = notification.IndicatorId,
                IndicatorName = notification.IndicatorName,
                Success = true, // Completion event assumes success
                CompletedAt = notification.OccurredOn.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                Duration = 0 // Will be calculated by the UI or provided separately
            };

            await _realtimeService.SendIndicatorExecutionCompletedAsync(dto);

            _logger.LogDebug("Successfully handled IndicatorExecutionCompletedEvent for Indicator {IndicatorId}", notification.IndicatorId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle IndicatorExecutionCompletedEvent for Indicator {IndicatorId}", notification.IndicatorId);
        }
    }
}

/// <summary>
/// Domain event handler for Indicator executed events (with results)
/// </summary>
public class IndicatorExecutedEventHandler : INotificationHandler<IndicatorExecutedEvent>
{
    private readonly IRealtimeNotificationService _realtimeService;
    private readonly ILogger<IndicatorExecutedEventHandler> _logger;

    public IndicatorExecutedEventHandler(
        IRealtimeNotificationService realtimeService,
        ILogger<IndicatorExecutedEventHandler> logger)
    {
        _realtimeService = realtimeService;
        _logger = logger;
    }

    public async Task Handle(IndicatorExecutedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Handling IndicatorExecutedEvent for Indicator {IndicatorId}", notification.IndicatorId);

            var dto = new IndicatorExecutionCompletedDto
            {
                IndicatorId = notification.IndicatorId,
                IndicatorName = notification.IndicatorName,
                Success = notification.WasSuccessful,
                Value = notification.CurrentValue,
                CompletedAt = notification.OccurredOn.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                ErrorMessage = notification.ErrorMessage,
                Duration = 0 // Will be calculated separately
            };

            await _realtimeService.SendIndicatorExecutionCompletedAsync(dto);

            // Send status update
            var statusDto = new IndicatorStatusUpdateDto
            {
                IndicatorID = notification.IndicatorId,
                IndicatorName = notification.IndicatorName,
                IsCurrentlyRunning = false,
                LastValue = notification.CurrentValue,
                Status = notification.WasSuccessful ? "healthy" : "error",
                IsSignificantChange = true,
                ChangeReason = notification.WasSuccessful ? "Execution completed successfully" : "Execution failed"
            };

            await _realtimeService.SendIndicatorUpdateAsync(statusDto);

            _logger.LogDebug("Successfully handled IndicatorExecutedEvent for Indicator {IndicatorId}", notification.IndicatorId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle IndicatorExecutedEvent for Indicator {IndicatorId}", notification.IndicatorId);
        }
    }
}

/// <summary>
/// Domain event handler for Indicator threshold breached events
/// </summary>
public class IndicatorThresholdBreachedEventHandler : INotificationHandler<IndicatorThresholdBreachedEvent>
{
    private readonly IRealtimeNotificationService _realtimeService;
    private readonly ILogger<IndicatorThresholdBreachedEventHandler> _logger;

    public IndicatorThresholdBreachedEventHandler(
        IRealtimeNotificationService realtimeService,
        ILogger<IndicatorThresholdBreachedEventHandler> logger)
    {
        _realtimeService = realtimeService;
        _logger = logger;
    }

    public async Task Handle(IndicatorThresholdBreachedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Handling IndicatorThresholdBreachedEvent for Indicator {IndicatorId}", notification.IndicatorId);

            var alertDto = new IndicatorAlertDto
            {
                IndicatorID = notification.IndicatorId,
                IndicatorName = notification.IndicatorName,
                Owner = notification.Owner,
                AlertType = "Threshold",
                Message = $"Indicator '{notification.IndicatorName}' threshold breached. Current value: {notification.CurrentValue}, Threshold: {notification.ThresholdValue}",
                Priority = notification.Priority,
                CurrentValue = notification.CurrentValue,
                ThresholdValue = notification.ThresholdValue,
                ThresholdComparison = notification.ThresholdComparison,
                TriggerTime = notification.OccurredOn,
                RequiresAction = notification.Priority == "high"
            };

            await _realtimeService.SendIndicatorAlertAsync(alertDto);

            // Send status update
            var statusDto = new IndicatorStatusUpdateDto
            {
                IndicatorID = notification.IndicatorId,
                IndicatorName = notification.IndicatorName,
                Status = "warning",
                LastValue = notification.CurrentValue,
                IsSignificantChange = true,
                ChangeReason = "Threshold breached"
            };

            await _realtimeService.SendIndicatorUpdateAsync(statusDto);

            _logger.LogWarning("Indicator threshold breached: {IndicatorName} (ID: {IndicatorId}), Current: {CurrentValue}, Threshold: {ThresholdValue}",
                notification.IndicatorName, notification.IndicatorId, notification.CurrentValue, notification.ThresholdValue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle IndicatorThresholdBreachedEvent for Indicator {IndicatorId}", notification.IndicatorId);
        }
    }
}

/// <summary>
/// Domain event handler for Indicator created events
/// </summary>
public class IndicatorCreatedEventHandler : INotificationHandler<IndicatorCreatedEvent>
{
    private readonly IRealtimeNotificationService _realtimeService;
    private readonly ILogger<IndicatorCreatedEventHandler> _logger;

    public IndicatorCreatedEventHandler(
        IRealtimeNotificationService realtimeService,
        ILogger<IndicatorCreatedEventHandler> logger)
    {
        _realtimeService = realtimeService;
        _logger = logger;
    }

    public async Task Handle(IndicatorCreatedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Handling IndicatorCreatedEvent for Indicator {IndicatorId}", notification.IndicatorId);

            var statusDto = new IndicatorStatusUpdateDto
            {
                IndicatorID = notification.IndicatorId,
                IndicatorName = notification.IndicatorName,
                IsActive = true,
                Status = "idle",
                IsSignificantChange = true,
                ChangeReason = "Indicator created"
            };

            await _realtimeService.SendIndicatorUpdateAsync(statusDto);

            _logger.LogInformation("New indicator created: {IndicatorName} (ID: {IndicatorId}) by {Owner}",
                notification.IndicatorName, notification.IndicatorId, notification.Owner);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle IndicatorCreatedEvent for Indicator {IndicatorId}", notification.IndicatorId);
        }
    }
}

/// <summary>
/// Domain event handler for Indicator updated events
/// </summary>
public class IndicatorUpdatedEventHandler : INotificationHandler<IndicatorUpdatedEvent>
{
    private readonly IRealtimeNotificationService _realtimeService;
    private readonly ILogger<IndicatorUpdatedEventHandler> _logger;

    public IndicatorUpdatedEventHandler(
        IRealtimeNotificationService realtimeService,
        ILogger<IndicatorUpdatedEventHandler> logger)
    {
        _realtimeService = realtimeService;
        _logger = logger;
    }

    public async Task Handle(IndicatorUpdatedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Handling IndicatorUpdatedEvent for Indicator {IndicatorId}", notification.IndicatorId);

            var statusDto = new IndicatorStatusUpdateDto
            {
                IndicatorID = notification.IndicatorId,
                IndicatorName = notification.IndicatorName,
                IsSignificantChange = true,
                ChangeReason = "Indicator updated"
            };

            await _realtimeService.SendIndicatorUpdateAsync(statusDto);

            _logger.LogInformation("Indicator updated: {IndicatorName} (ID: {IndicatorId}) by {Owner}",
                notification.IndicatorName, notification.IndicatorId, notification.Owner);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle IndicatorUpdatedEvent for Indicator {IndicatorId}", notification.IndicatorId);
        }
    }
}

/// <summary>
/// Domain event handler for Indicator deleted events
/// </summary>
public class IndicatorDeletedEventHandler : INotificationHandler<IndicatorDeletedEvent>
{
    private readonly IRealtimeNotificationService _realtimeService;
    private readonly ILogger<IndicatorDeletedEventHandler> _logger;

    public IndicatorDeletedEventHandler(
        IRealtimeNotificationService realtimeService,
        ILogger<IndicatorDeletedEventHandler> logger)
    {
        _realtimeService = realtimeService;
        _logger = logger;
    }

    public async Task Handle(IndicatorDeletedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Handling IndicatorDeletedEvent for Indicator {IndicatorId}", notification.IndicatorId);

            var statusDto = new IndicatorStatusUpdateDto
            {
                IndicatorID = notification.IndicatorId,
                IndicatorName = notification.IndicatorName,
                IsActive = false,
                Status = "deleted",
                IsSignificantChange = true,
                ChangeReason = "Indicator deleted"
            };

            await _realtimeService.SendIndicatorUpdateAsync(statusDto);

            _logger.LogInformation("Indicator deleted: {IndicatorName} (ID: {IndicatorId}) by {Owner}",
                notification.IndicatorName, notification.IndicatorId, notification.Owner);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle IndicatorDeletedEvent for Indicator {IndicatorId}", notification.IndicatorId);
        }
    }
}
