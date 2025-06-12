using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using MonitoringGrid.Api.CQRS.Commands.Indicator;
using MonitoringGrid.Api.DTOs;
using MonitoringGrid.Core.Common;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Events;
using MonitoringGrid.Core.Interfaces;

namespace MonitoringGrid.Api.CQRS.Handlers.Indicator;

/// <summary>
/// Handler for updating existing indicators
/// </summary>
public class UpdateIndicatorCommandHandler : IRequestHandler<UpdateIndicatorCommand, Result<IndicatorDto>>
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

    public async Task<Result<IndicatorDto>> Handle(UpdateIndicatorCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Updating indicator {IndicatorId}: {IndicatorName}", request.IndicatorId, request.IndicatorName);

            // Get existing indicator
            var existingIndicator = await _indicatorService.GetIndicatorByIdAsync(request.IndicatorId, cancellationToken);
            if (existingIndicator == null)
            {
                return Result<IndicatorDto>.Failure($"Indicator with ID {request.IndicatorId} not found");
            }

            // Validate collector exists and is active
            var collector = await _progressPlayDbService.GetCollectorByIdAsync(request.CollectorId, cancellationToken);
            if (collector == null)
            {
                return Result<IndicatorDto>.Failure($"Collector with ID {request.CollectorId} not found");
            }

            if (!collector.IsActive)
            {
                return Result<IndicatorDto>.Failure($"Collector {collector.CollectorCode} is not active");
            }

            // Validate collector item name exists
            var availableItems = await _progressPlayDbService.GetCollectorItemNamesAsync(request.CollectorId, cancellationToken);
            if (!availableItems.Contains(request.CollectorItemName))
            {
                return Result<IndicatorDto>.Failure($"Item '{request.CollectorItemName}' not found for collector {collector.CollectorCode}");
            }

            // Validate owner contact exists
            var contactRepository = _unitOfWork.Repository<Contact>();
            var ownerContact = await contactRepository.GetByIdAsync(request.OwnerContactId, cancellationToken);
            if (ownerContact == null)
            {
                return Result<IndicatorDto>.Failure($"Owner contact with ID {request.OwnerContactId} not found");
            }

            // Validate additional contacts if provided
            if (request.ContactIds.Any())
            {
                var contacts = await contactRepository.GetByIdsAsync(request.ContactIds, cancellationToken);
                var missingContactIds = request.ContactIds.Except(contacts.Select(c => c.ContactId)).ToList();
                if (missingContactIds.Any())
                {
                    return Result<IndicatorDto>.Failure($"Contacts not found: {string.Join(", ", missingContactIds)}");
                }
            }

            // Validate schedule configuration
            if (!IsValidScheduleConfiguration(request.ScheduleConfiguration))
            {
                return Result<IndicatorDto>.Failure("Invalid schedule configuration format");
            }

            // Check for duplicate indicator code (excluding current indicator)
            var indicatorRepository = _unitOfWork.Repository<Core.Entities.Indicator>();
            var duplicateIndicator = await indicatorRepository.GetAsync(
                i => i.IndicatorCode == request.IndicatorCode && i.IndicatorId != request.IndicatorId, 
                cancellationToken);
            if (duplicateIndicator.Any())
            {
                return Result<IndicatorDto>.Failure($"Indicator with code '{request.IndicatorCode}' already exists");
            }

            // Update the indicator properties
            existingIndicator.IndicatorName = request.IndicatorName;
            existingIndicator.IndicatorCode = request.IndicatorCode;
            existingIndicator.IndicatorDesc = request.IndicatorDesc;
            existingIndicator.CollectorId = request.CollectorId;
            existingIndicator.CollectorItemName = request.CollectorItemName;
            existingIndicator.ScheduleConfiguration = request.ScheduleConfiguration;
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
                existingIndicator.IndicatorId, 
                existingIndicator.IndicatorName, 
                ownerContact.Name));

            // Update the indicator
            var updatedIndicator = await _indicatorService.UpdateIndicatorAsync(existingIndicator, cancellationToken);

            // Update contacts
            var currentContactIds = existingIndicator.IndicatorContacts.Select(ic => ic.ContactId).ToList();
            var contactsToRemove = currentContactIds.Except(request.ContactIds).ToList();
            var contactsToAdd = request.ContactIds.Except(currentContactIds).ToList();

            if (contactsToRemove.Any())
            {
                await _indicatorService.RemoveContactsFromIndicatorAsync(
                    updatedIndicator.IndicatorId, 
                    contactsToRemove, 
                    cancellationToken);
            }

            if (contactsToAdd.Any())
            {
                await _indicatorService.AddContactsToIndicatorAsync(
                    updatedIndicator.IndicatorId, 
                    contactsToAdd, 
                    cancellationToken);
            }

            // Reload with updated contacts for mapping
            var indicatorWithContacts = await _indicatorService.GetIndicatorByIdAsync(
                updatedIndicator.IndicatorId, 
                cancellationToken);

            var indicatorDto = _mapper.Map<IndicatorDto>(indicatorWithContacts);

            _logger.LogInformation("Successfully updated indicator {IndicatorId}: {IndicatorName}", 
                updatedIndicator.IndicatorId, updatedIndicator.IndicatorName);

            return Result<IndicatorDto>.Success(indicatorDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update indicator {IndicatorId}: {IndicatorName}", 
                request.IndicatorId, request.IndicatorName);
            return Result<IndicatorDto>.Failure($"Failed to update indicator: {ex.Message}");
        }
    }

    private static bool IsValidScheduleConfiguration(string scheduleConfiguration)
    {
        try
        {
            var config = System.Text.Json.JsonSerializer.Deserialize<ScheduleConfig>(scheduleConfiguration);
            return config != null && !string.IsNullOrEmpty(config.ScheduleType);
        }
        catch
        {
            return false;
        }
    }
}
