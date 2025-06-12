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
/// Handler for creating new indicators
/// </summary>
public class CreateIndicatorCommandHandler : IRequestHandler<CreateIndicatorCommand, Result<IndicatorDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIndicatorService _indicatorService;
    private readonly IProgressPlayDbService _progressPlayDbService;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateIndicatorCommandHandler> _logger;

    public CreateIndicatorCommandHandler(
        IUnitOfWork unitOfWork,
        IIndicatorService indicatorService,
        IProgressPlayDbService progressPlayDbService,
        IMapper mapper,
        ILogger<CreateIndicatorCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _indicatorService = indicatorService;
        _progressPlayDbService = progressPlayDbService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<IndicatorDto>> Handle(CreateIndicatorCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Creating indicator: {IndicatorName}", request.IndicatorName);

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

            // Check for duplicate indicator code
            var indicatorRepository = _unitOfWork.Repository<Core.Entities.Indicator>();
            var existingIndicator = await indicatorRepository.GetAsync(i => i.IndicatorCode == request.IndicatorCode, cancellationToken);
            if (existingIndicator.Any())
            {
                return Result<IndicatorDto>.Failure($"Indicator with code '{request.IndicatorCode}' already exists");
            }

            // Create the indicator entity
            var indicator = new Core.Entities.Indicator
            {
                IndicatorName = request.IndicatorName,
                IndicatorCode = request.IndicatorCode,
                IndicatorDesc = request.IndicatorDesc,
                CollectorId = request.CollectorId,
                CollectorItemName = request.CollectorItemName,
                ScheduleConfiguration = request.ScheduleConfiguration,
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
                indicator.IndicatorId, 
                indicator.IndicatorName, 
                ownerContact.Name));

            // Save the indicator
            var createdIndicator = await _indicatorService.CreateIndicatorAsync(indicator, cancellationToken);

            // Add contacts if provided
            if (request.ContactIds.Any())
            {
                await _indicatorService.AddContactsToIndicatorAsync(
                    createdIndicator.IndicatorId, 
                    request.ContactIds, 
                    cancellationToken);
            }

            // Reload with contacts for mapping
            var indicatorWithContacts = await _indicatorService.GetIndicatorByIdAsync(
                createdIndicator.IndicatorId, 
                cancellationToken);

            var indicatorDto = _mapper.Map<IndicatorDto>(indicatorWithContacts);

            _logger.LogInformation("Successfully created indicator {IndicatorId}: {IndicatorName}", 
                createdIndicator.IndicatorId, createdIndicator.IndicatorName);

            return Result<IndicatorDto>.Success(indicatorDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create indicator: {IndicatorName}", request.IndicatorName);
            return Result<IndicatorDto>.Failure($"Failed to create indicator: {ex.Message}");
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
