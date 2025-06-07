using Microsoft.AspNetCore.Mvc;
using MonitoringGrid.Api.Events;

namespace MonitoringGrid.Api.Controllers;

/// <summary>
/// Controller for monitoring and demonstrating domain events
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class DomainEventsController : ControllerBase
{
    private readonly DomainEventIntegrationService _domainEventService;
    private readonly ILogger<DomainEventsController> _logger;

    public DomainEventsController(
        DomainEventIntegrationService domainEventService,
        ILogger<DomainEventsController> logger)
    {
        _domainEventService = domainEventService;
        _logger = logger;
    }

    /// <summary>
    /// Get domain event statistics
    /// </summary>
    [HttpGet("statistics")]
    public ActionResult<DomainEventStatistics> GetStatistics()
    {
        try
        {
            var statistics = _domainEventService.GetEventStatistics();
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving domain event statistics");
            return StatusCode(500, "An error occurred while retrieving event statistics");
        }
    }

    /// <summary>
    /// Demonstrate the event-driven workflow
    /// </summary>
    [HttpPost("demonstrate")]
    public async Task<IActionResult> DemonstrateWorkflow(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting domain event workflow demonstration");
            
            await _domainEventService.DemonstrateEventDrivenWorkflowAsync(cancellationToken);
            
            var statistics = _domainEventService.GetEventStatistics();
            
            return Ok(new
            {
                Message = "Domain event workflow demonstration completed successfully",
                Statistics = statistics
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during domain event workflow demonstration");
            return StatusCode(500, "An error occurred during the workflow demonstration");
        }
    }

    /// <summary>
    /// Reset event statistics
    /// </summary>
    [HttpPost("reset-statistics")]
    public IActionResult ResetStatistics()
    {
        try
        {
            _domainEventService.ResetStatistics();
            
            return Ok(new
            {
                Message = "Domain event statistics have been reset"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting domain event statistics");
            return StatusCode(500, "An error occurred while resetting statistics");
        }
    }

    /// <summary>
    /// Get information about available domain event handlers
    /// </summary>
    [HttpGet("handlers")]
    public ActionResult<object> GetHandlerInfo()
    {
        try
        {
            var handlerInfo = new
            {
                KpiEventHandlers = new[]
                {
                    new { EventType = "KpiCreatedEvent", Handler = "KpiCreatedEventHandler", Description = "Handles KPI creation events - updates metrics, initializes tracking, logs audit trail" },
                    new { EventType = "KpiUpdatedEvent", Handler = "KpiUpdatedEventHandler", Description = "Handles KPI update events - logs changes, updates schedules, clears cache" },
                    new { EventType = "KpiExecutedEvent", Handler = "KpiExecutedEventHandler", Description = "Handles KPI execution events - updates metrics, logs execution details" },
                    new { EventType = "KpiThresholdBreachedEvent", Handler = "KpiThresholdBreachedEventHandler", Description = "Handles threshold breaches - creates alerts, triggers notifications" },
                    new { EventType = "KpiDeactivatedEvent", Handler = "KpiDeactivatedEventHandler", Description = "Handles KPI deactivation - stops monitoring, cleans up resources" }
                },
                AlertEventHandlers = new[]
                {
                    new { EventType = "AlertTriggeredEvent", Handler = "AlertTriggeredEventHandler", Description = "Handles alert triggers - sends real-time notifications, updates metrics" },
                    new { EventType = "AlertResolvedEvent", Handler = "AlertResolvedEventHandler", Description = "Handles alert resolution - sends notifications, updates metrics, cleans up escalations" }
                },
                Architecture = new
                {
                    Publisher = "MediatRDomainEventPublisher",
                    Integration = "UnitOfWork automatically publishes domain events after SaveChanges",
                    Pattern = "Event-driven architecture with CQRS and MediatR",
                    Benefits = new[]
                    {
                        "Decoupled business logic",
                        "Auditable event trail",
                        "Real-time notifications",
                        "Scalable event processing",
                        "Testable event handlers"
                    }
                }
            };

            return Ok(handlerInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving handler information");
            return StatusCode(500, "An error occurred while retrieving handler information");
        }
    }
}
