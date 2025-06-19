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
/// Handler for updating existing indicators
/// </summary>
public class UpdateIndicatorCommandHandler : IRequestHandler<UpdateIndicatorCommand, Result<IndicatorResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIndicatorService _indicatorService;
    private readonly IProgressPlayDbService _progressPlayDbService;
    private readonly IMapper _mapper;
    private readonly ILogger<UpdateIndicatorCommandHandler> _logger;

    public UpdateIndicatorCommandHandler(
        IUnitOfWork unitOfWork,
        IIndicatorService indicatorService,
        IProgressPlayDbService progressPlayDbService,
        IMapper mapper,
        ILogger<UpdateIndicatorCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _indicatorService = indicatorService;
        _progressPlayDbService = progressPlayDbService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<IndicatorResponse>> Handle(UpdateIndicatorCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Updating indicator {IndicatorId}: {IndicatorName}", request.IndicatorID, request.IndicatorName);

            // Get existing indicator
            var existingIndicatorResult = await _indicatorService.GetIndicatorByIdAsync(request.IndicatorID, cancellationToken);
            if (!existingIndicatorResult.IsSuccess)
            {
                return Result.Failure<IndicatorResponse>("INDICATOR_NOT_FOUND", $"Indicator with ID {request.IndicatorID} not found");
            }

            var existingIndicator = existingIndicatorResult.Value;

            // Validate collector exists and is active
            _logger.LogInformation("DEBUG: Validating collector ID {CollectorId}", request.CollectorID);
            var collector = await _progressPlayDbService.GetCollectorByIdAsync(request.CollectorID, cancellationToken);
            if (collector == null)
            {
                _logger.LogWarning("DEBUG: Collector {CollectorId} not found", request.CollectorID);
                return Result.Failure<IndicatorResponse>("COLLECTOR_NOT_FOUND", $"Collector with ID {request.CollectorID} not found");
            }
            _logger.LogInformation("DEBUG: Collector {CollectorId} found: {CollectorCode}", request.CollectorID, collector.CollectorCode);

            if (!collector.IsActive)
            {
                return Result.Failure<IndicatorResponse>("COLLECTOR_INACTIVE", $"Collector {collector.CollectorCode} is not active");
            }

            // Validate collector item name exists
            var availableItems = await _progressPlayDbService.GetCollectorItemNamesAsync(request.CollectorID, cancellationToken);
            if (!availableItems.Contains(request.CollectorItemName))
            {
                return Result.Failure<IndicatorResponse>("ITEM_NOT_FOUND", $"Item '{request.CollectorItemName}' not found for collector {collector.CollectorCode}");
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

            // Check for duplicate indicator code (excluding current indicator)
            var indicatorRepository = _unitOfWork.Repository<Core.Entities.Indicator>();
            var duplicateIndicator = await indicatorRepository.GetAsync(
                i => i.IndicatorCode == request.IndicatorCode && i.IndicatorID != request.IndicatorID,
                cancellationToken);
            if (duplicateIndicator.Any())
            {
                return Result.Failure<IndicatorResponse>("DUPLICATE_CODE", $"Indicator with code '{request.IndicatorCode}' already exists");
            }

            // Update the indicator properties
            existingIndicator.IndicatorName = request.IndicatorName;
            existingIndicator.IndicatorCode = request.IndicatorCode;
            existingIndicator.IndicatorDesc = request.IndicatorDesc;
            existingIndicator.CollectorID = request.CollectorID;
            existingIndicator.CollectorItemName = request.CollectorItemName;
            existingIndicator.SchedulerID = request.SchedulerID;
            existingIndicator.IsActive = request.IsActive;
            existingIndicator.LastMinutes = request.LastMinutes;
            existingIndicator.ThresholdType = request.ThresholdType;
            existingIndicator.ThresholdField = request.ThresholdField;
            existingIndicator.ThresholdComparison = request.ThresholdComparison;
            existingIndicator.ThresholdValue = request.ThresholdValue;
            existingIndicator.Priority = request.Priority;
            existingIndicator.OwnerContactId = request.OwnerContactId;
            existingIndicator.AverageLastDays = request.AverageLastDays;
            existingIndicator.UpdatedDate = DateTime.UtcNow;

            // Add domain event
            existingIndicator.AddDomainEvent(new IndicatorUpdatedEvent(
                existingIndicator.IndicatorID,
                existingIndicator.IndicatorName,
                ownerContact.Name));

            // Update the indicator
            var updateRequest = new Core.Models.UpdateIndicatorRequest
            {
                IndicatorID = existingIndicator.IndicatorID,
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
            };
            var updatedIndicatorResult = await _indicatorService.UpdateIndicatorAsync(updateRequest, cancellationToken);
            if (!updatedIndicatorResult.IsSuccess)
            {
                return Result.Failure<IndicatorResponse>("UPDATE_FAILED", updatedIndicatorResult.Error?.Message ?? "Failed to update indicator");
            }

            var updatedIndicator = updatedIndicatorResult.Value;

            // Update contacts
            var currentContactIds = existingIndicator.IndicatorContacts.Select(ic => ic.ContactId).ToList();
            var contactsToRemove = currentContactIds.Except(request.ContactIds).ToList();
            var contactsToAdd = request.ContactIds.Except(currentContactIds).ToList();

            if (contactsToRemove.Any())
            {
                var removalOptions = new Core.Models.ContactRemovalOptions
                {
                    CheckDependencies = true,
                    RemovalReason = "Indicator update"
                };
                await _indicatorService.RemoveContactsFromIndicatorAsync(updatedIndicator.IndicatorID, contactsToRemove, removalOptions, cancellationToken);
            }

            if (contactsToAdd.Any())
            {
                var assignmentOptions = new Core.Models.ContactAssignmentOptions
                {
                    ValidateContacts = true,
                    AssignmentReason = "Indicator update"
                };
                await _indicatorService.AddContactsToIndicatorAsync(updatedIndicator.IndicatorID, contactsToAdd, assignmentOptions, cancellationToken);
            }

            // Reload with updated contacts for mapping
            var indicatorWithContactsResult = await _indicatorService.GetIndicatorByIdAsync(
                updatedIndicator.IndicatorID,
                cancellationToken);

            if (!indicatorWithContactsResult.IsSuccess)
            {
                return Result.Failure<IndicatorResponse>("RELOAD_FAILED", "Failed to reload updated indicator");
            }

            var indicatorDto = _mapper.Map<IndicatorResponse>(indicatorWithContactsResult.Value);

            _logger.LogInformation("Successfully updated indicator {IndicatorId}: {IndicatorName}",
                updatedIndicator.IndicatorID, updatedIndicator.IndicatorName);

            return Result<IndicatorResponse>.Success(indicatorDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update indicator {IndicatorId}: {IndicatorName}",
                request.IndicatorID, request.IndicatorName);
            return Result.Failure<IndicatorResponse>("UPDATE_FAILED", $"Failed to update indicator: {ex.Message}");
        }
    }


}
