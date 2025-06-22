using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using MonitoringGrid.Api.CQRS.Commands.Indicator;
using MonitoringGrid.Api.DTOs.Indicators;
using MonitoringGrid.Core.Common;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Events;
using MonitoringGrid.Core.Interfaces;

namespace MonitoringGrid.Api.CQRS.Handlers.Indicator;

/// <summary>
/// Handler for creating new indicators
/// </summary>
public class CreateIndicatorCommandHandler : IRequestHandler<CreateIndicatorCommand, Result<IndicatorResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIndicatorService _indicatorService;
    private readonly IProgressPlayDbService _progressPlayDbService;
    private readonly IMonitorStatisticsService _statisticsService;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateIndicatorCommandHandler> _logger;

    public CreateIndicatorCommandHandler(
        IUnitOfWork unitOfWork,
        IIndicatorService indicatorService,
        IProgressPlayDbService progressPlayDbService,
        IMonitorStatisticsService statisticsService,
        IMapper mapper,
        ILogger<CreateIndicatorCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _indicatorService = indicatorService;
        _progressPlayDbService = progressPlayDbService;
        _statisticsService = statisticsService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<IndicatorResponse>> Handle(CreateIndicatorCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Creating indicator: {IndicatorName}", request.IndicatorName);

            // Validate collector exists and is active
            var collector = await _statisticsService.GetCollectorByCollectorIdAsync(request.CollectorID, cancellationToken);
            if (collector == null)
            {
                return Result.Failure<IndicatorResponse>("COLLECTOR_NOT_FOUND", $"Collector with ID {request.CollectorID} not found");
            }

            if (!(collector.IsActive ?? false))
            {
                return Result.Failure<IndicatorResponse>("COLLECTOR_INACTIVE", $"Collector {collector.CollectorCode} is not active");
            }

            // Validate collector item name exists
            if (!string.IsNullOrEmpty(request.CollectorItemName))
            {
                var availableItems = await _statisticsService.GetCollectorItemNamesAsync(request.CollectorID, cancellationToken);
                if (!availableItems.Contains(request.CollectorItemName))
                {
                    return Result.Failure<IndicatorResponse>("ITEM_NOT_FOUND", $"Item '{request.CollectorItemName}' not found for collector {collector.CollectorCode}");
                }
            }

            // Validate owner contact exists
            var contactRepository = _unitOfWork.Repository<Contact>();
            var ownerContact = await contactRepository.GetByIdAsync(request.OwnerContactId, cancellationToken);
            if (ownerContact == null)
            {
                return Result.Failure<IndicatorResponse>("OWNER_CONTACT_NOT_FOUND", $"Owner contact with ID {request.OwnerContactId} not found");
            }

            // Validate additional contacts if provided
            if (request.ContactIds.Any())
            {
                var contacts = await contactRepository.GetByIdsAsync(request.ContactIds, cancellationToken);
                var missingContactIds = request.ContactIds.Except(contacts.Select(c => c.ContactId)).ToList();
                if (missingContactIds.Any())
                {
                    return Result.Failure<IndicatorResponse>("CONTACTS_NOT_FOUND", $"Contacts not found: {string.Join(", ", missingContactIds)}");
                }
            }

            // Validate scheduler if provided
            if (request.SchedulerID.HasValue)
            {
                var schedulerRepository = _unitOfWork.Repository<Core.Entities.Scheduler>();
                var scheduler = await schedulerRepository.GetByIdAsync(request.SchedulerID.Value, cancellationToken);
                if (scheduler == null)
                {
                    return Result.Failure<IndicatorResponse>("SCHEDULER_NOT_FOUND", $"Scheduler with ID {request.SchedulerID} not found");
                }
                if (!scheduler.IsEnabled)
                {
                    return Result.Failure<IndicatorResponse>("SCHEDULER_DISABLED", $"Scheduler '{scheduler.SchedulerName}' is disabled");
                }
            }

            // Check for duplicate indicator code
            var indicatorRepository = _unitOfWork.Repository<Core.Entities.Indicator>();
            var existingIndicator = await indicatorRepository.GetAsync(i => i.IndicatorCode == request.IndicatorCode, cancellationToken);
            if (existingIndicator.Any())
            {
                return Result.Failure<IndicatorResponse>("DUPLICATE_CODE", $"Indicator with code '{request.IndicatorCode}' already exists");
            }

            // Create the indicator entity
            var indicator = new Core.Entities.Indicator
            {
                IndicatorName = request.IndicatorName,
                IndicatorCode = request.IndicatorCode,
                IndicatorDesc = request.IndicatorDesc,
                CollectorID = request.CollectorID,
                CollectorItemName = request.CollectorItemName,
                SchedulerID = request.SchedulerID,
                IsActive = request.IsActive,
                LastMinutes = request.LastMinutes,
                ThresholdType = request.ThresholdType,
                ThresholdField = request.ThresholdField,
                ThresholdComparison = request.ThresholdComparison,
                ThresholdValue = request.ThresholdValue,
                Priority = request.Priority,
                OwnerContactId = request.OwnerContactId,
                AverageLastDays = request.AverageLastDays,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };

            // Add domain event
            indicator.AddDomainEvent(new IndicatorCreatedEvent(
                indicator.IndicatorID,
                indicator.IndicatorName,
                ownerContact.Name));

            // Save the indicator
            var createRequest = new Core.Models.CreateIndicatorRequest
            {
                IndicatorName = indicator.IndicatorName,
                IndicatorCode = indicator.IndicatorCode,
                IndicatorDesc = indicator.IndicatorDesc,
                CollectorId = indicator.CollectorID,
                CollectorItemName = indicator.CollectorItemName,
                SchedulerId = indicator.SchedulerID,
                IsActive = indicator.IsActive,
                LastMinutes = indicator.LastMinutes,
                ThresholdType = indicator.ThresholdType,
                ThresholdField = indicator.ThresholdField,
                ThresholdComparison = indicator.ThresholdComparison,
                ThresholdValue = indicator.ThresholdValue,
                Priority = indicator.Priority,
                OwnerContactId = indicator.OwnerContactId,
                AverageLastDays = indicator.AverageLastDays
            };
            var createdIndicatorResult = await _indicatorService.CreateIndicatorAsync(createRequest);
            if (!createdIndicatorResult.IsSuccess)
            {
                return Result.Failure<IndicatorResponse>("CREATE_FAILED", createdIndicatorResult.Error?.Message ?? "Failed to create indicator");
            }

            var createdIndicator = createdIndicatorResult.Value;

            // Add contacts if provided
            if (request.ContactIds.Any())
            {
                var assignmentOptions = new Core.Models.ContactAssignmentOptions
                {
                    ValidateContacts = true,
                    AssignmentReason = "Indicator creation"
                };
                await _indicatorService.AddContactsToIndicatorAsync(createdIndicator.IndicatorID, request.ContactIds, assignmentOptions, cancellationToken);
            }

            // Reload with contacts for mapping
            var indicatorWithContactsResult = await _indicatorService.GetIndicatorByIdAsync(
                createdIndicator.IndicatorID,
                cancellationToken);

            if (!indicatorWithContactsResult.IsSuccess)
            {
                return Result.Failure<IndicatorResponse>("RELOAD_FAILED", "Failed to reload created indicator");
            }

            var indicatorDto = _mapper.Map<IndicatorResponse>(indicatorWithContactsResult.Value);

            _logger.LogInformation("Successfully created indicator {IndicatorId}: {IndicatorName}",
                createdIndicator.IndicatorID, createdIndicator.IndicatorName);

            return Result<IndicatorResponse>.Success(indicatorDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create indicator: {IndicatorName}", request.IndicatorName);
            return Result.Failure<IndicatorResponse>("CREATE_FAILED", $"Failed to create indicator: {ex.Message}");
        }
    }


}
