using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using MonitoringGrid.Api.CQRS.Commands.Indicator;
using MonitoringGrid.Api.DTOs;
using MonitoringGrid.Api.Common;
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

    public async Task<Result<IndicatorDto>> Handle(CreateIndicatorCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Creating indicator: {IndicatorName}", request.IndicatorName);

            // Validate collector exists and is active
            var collector = await _statisticsService.GetCollectorByCollectorIdAsync(request.CollectorID, cancellationToken);
            if (collector == null)
            {
                return Result.Failure<IndicatorDto>("COLLECTOR_NOT_FOUND", $"Collector with ID {request.CollectorID} not found");
            }

            if (!(collector.IsActive ?? false))
            {
                return Result.Failure<IndicatorDto>("COLLECTOR_INACTIVE", $"Collector {collector.CollectorCode} is not active");
            }

            // Validate collector item name exists
            var availableItems = await _statisticsService.GetCollectorItemNamesAsync(request.CollectorID, cancellationToken);
            if (!availableItems.Contains(request.CollectorItemName))
            {
                return Result.Failure<IndicatorDto>("ITEM_NOT_FOUND", $"Item '{request.CollectorItemName}' not found for collector {collector.CollectorCode}");
            }

            // Validate owner contact exists
            var contactRepository = _unitOfWork.Repository<Contact>();
            var ownerContact = await contactRepository.GetByIdAsync(request.OwnerContactId, cancellationToken);
            if (ownerContact == null)
            {
                return Result.Failure<IndicatorDto>("OWNER_CONTACT_NOT_FOUND", $"Owner contact with ID {request.OwnerContactId} not found");
            }

            // Validate additional contacts if provided
            if (request.ContactIds.Any())
            {
                var contacts = await contactRepository.GetByIdsAsync(request.ContactIds, cancellationToken);
                var missingContactIds = request.ContactIds.Except(contacts.Select(c => c.ContactId)).ToList();
                if (missingContactIds.Any())
                {
                    return Result.Failure<IndicatorDto>("CONTACTS_NOT_FOUND", $"Contacts not found: {string.Join(", ", missingContactIds)}");
                }
            }

            // Validate schedule configuration
            if (!IsValidScheduleConfiguration(request.ScheduleConfiguration))
            {
                return Result.Failure<IndicatorDto>("INVALID_SCHEDULE", "Invalid schedule configuration format");
            }

            // Check for duplicate indicator code
            var indicatorRepository = _unitOfWork.Repository<Core.Entities.Indicator>();
            var existingIndicator = await indicatorRepository.GetAsync(i => i.IndicatorCode == request.IndicatorCode, cancellationToken);
            if (existingIndicator.Any())
            {
                return Result.Failure<IndicatorDto>("DUPLICATE_CODE", $"Indicator with code '{request.IndicatorCode}' already exists");
            }

            // Create the indicator entity
            var indicator = new Core.Entities.Indicator
            {
                IndicatorName = request.IndicatorName,
                IndicatorCode = request.IndicatorCode,
                IndicatorDesc = request.IndicatorDesc,
                CollectorID = request.CollectorID,
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
                indicator.IndicatorID,
                indicator.IndicatorName,
                ownerContact.Name));

            // Save the indicator
            var createdIndicator = await _indicatorService.CreateIndicatorAsync(indicator, cancellationToken);

            // Add contacts if provided
            if (request.ContactIds.Any())
            {
                await _indicatorService.AddContactsToIndicatorAsync(
                    createdIndicator.IndicatorID,
                    request.ContactIds,
                    cancellationToken);
            }

            // Reload with contacts for mapping
            var indicatorWithContacts = await _indicatorService.GetIndicatorByIdAsync(
                createdIndicator.IndicatorID,
                cancellationToken);

            var indicatorDto = _mapper.Map<IndicatorDto>(indicatorWithContacts);

            _logger.LogInformation("Successfully created indicator {IndicatorId}: {IndicatorName}",
                createdIndicator.IndicatorID, createdIndicator.IndicatorName);

            return Result<IndicatorDto>.Success(indicatorDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create indicator: {IndicatorName}", request.IndicatorName);
            return Result.Failure<IndicatorDto>("CREATE_FAILED", $"Failed to create indicator: {ex.Message}");
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
