using AutoMapper;
using MonitoringGrid.Api.Common;
using MonitoringGrid.Api.CQRS.Commands;
using MonitoringGrid.Api.CQRS.Commands.Kpi;
using MonitoringGrid.Api.DTOs;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Events;
using MonitoringGrid.Core.Factories;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Core.Services;
using MonitoringGrid.Core.ValueObjects;

namespace MonitoringGrid.Api.CQRS.Handlers.Kpi;

/// <summary>
/// Handler for creating a new KPI
/// </summary>
public class CreateKpiCommandHandler : ICommandHandler<CreateKpiCommand, KpiDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly KpiDomainService _kpiDomainService;
    private readonly KpiFactory _kpiFactory;
    private readonly ILogger<CreateKpiCommandHandler> _logger;

    public CreateKpiCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        KpiDomainService kpiDomainService,
        KpiFactory kpiFactory,
        ILogger<CreateKpiCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _kpiDomainService = kpiDomainService;
        _kpiFactory = kpiFactory;
        _logger = logger;
    }

    public async Task<Result<KpiDto>> Handle(CreateKpiCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating KPI with indicator: {Indicator}", request.Indicator);

        // Validate deviation percentage using value object
        try
        {
            var deviationPercentage = new DeviationPercentage(request.Deviation);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid deviation percentage: {Message}", ex.Message);
            return Error.Validation("Deviation", ex.Message);
        }

        try
        {
            // Use factory to create KPI
            var kpi = _kpiFactory.CreateKpi(
                request.Indicator,
                request.Owner,
                request.Priority,
                request.Frequency,
                request.Deviation,
                request.SpName,
                request.SubjectTemplate,
                request.DescriptionTemplate);

            // Set additional properties
            kpi.LastMinutes = request.LastMinutes;
            kpi.IsActive = request.IsActive;
            kpi.CooldownMinutes = request.CooldownMinutes;
            kpi.MinimumThreshold = request.MinimumThreshold;

            // Check if indicator name is unique using domain service
            if (!await _kpiDomainService.IsIndicatorUniqueAsync(request.Indicator))
            {
                var message = $"KPI with indicator '{request.Indicator}' already exists";
                _logger.LogWarning(message);
                return Error.Conflict(message);
            }

            // Add to repository and save
            var kpiRepository = _unitOfWork.Repository<KPI>();
            await kpiRepository.AddAsync(kpi, cancellationToken);

            // Raise domain event for KPI creation
            var createdEvent = new KpiCreatedEvent(kpi);
            _unitOfWork.AddDomainEvent(createdEvent);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created KPI {Indicator} with ID {KpiId}", kpi.Indicator, kpi.KpiId);

            // Map to DTO and return
            var kpiDto = _mapper.Map<KpiDto>(kpi);
            return Result.Success(kpiDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating KPI with indicator: {Indicator}", request.Indicator);
            return Error.Failure("KPI.CreateFailed", "An error occurred while creating the KPI");
        }
    }
}
