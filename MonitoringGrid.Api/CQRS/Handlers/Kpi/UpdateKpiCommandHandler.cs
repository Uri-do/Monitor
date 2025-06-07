using AutoMapper;
using Microsoft.EntityFrameworkCore;
using MonitoringGrid.Api.Common;
using MonitoringGrid.Api.CQRS.Commands;
using MonitoringGrid.Api.CQRS.Commands.Kpi;
using MonitoringGrid.Api.DTOs;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Core.Services;
using MonitoringGrid.Core.ValueObjects;

namespace MonitoringGrid.Api.CQRS.Handlers.Kpi;

/// <summary>
/// Handler for updating an existing KPI
/// </summary>
public class UpdateKpiCommandHandler : ICommandHandler<UpdateKpiCommand, KpiDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly KpiDomainService _kpiDomainService;
    private readonly ILogger<UpdateKpiCommandHandler> _logger;

    public UpdateKpiCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        KpiDomainService kpiDomainService,
        ILogger<UpdateKpiCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _kpiDomainService = kpiDomainService;
        _logger = logger;
    }

    public async Task<Result<KpiDto>> Handle(UpdateKpiCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating KPI with ID: {KpiId}", request.KpiId);

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

        var kpiRepository = _unitOfWork.Repository<KPI>();
        var existingKpi = await kpiRepository.GetByIdAsync(request.KpiId, cancellationToken);

        if (existingKpi == null)
        {
            var message = $"KPI with ID {request.KpiId} not found";
            _logger.LogWarning(message);
            return Error.NotFound("KPI", request.KpiId);
        }

        // Check if indicator name is unique (excluding current KPI)
        if (!await _kpiDomainService.IsIndicatorUniqueAsync(request.Indicator, request.KpiId))
        {
            var message = $"KPI with indicator '{request.Indicator}' already exists";
            _logger.LogWarning(message);
            return Error.Conflict(message);
        }

        try
        {
            // Map request to existing entity
            _mapper.Map(request, existingKpi);
            existingKpi.ModifiedDate = DateTime.UtcNow;

            // Update contact assignments
            await UpdateContactAssignmentsAsync(existingKpi, request.ContactIds, cancellationToken);

            // Raise domain event for KPI update
            existingKpi.UpdateConfiguration("System"); // TODO: Get from current user context

            // Update and save
            await kpiRepository.UpdateAsync(existingKpi, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Updated KPI {Indicator} with ID {KpiId}", existingKpi.Indicator, request.KpiId);

            // Reload KPI with contacts for response
            var updatedKpi = await kpiRepository.GetByIdWithThenIncludesAsync(request.KpiId,
                query => query.Include(k => k.KpiContacts).ThenInclude(kc => kc.Contact));

            // Map to DTO and return
            var kpiDto = _mapper.Map<KpiDto>(updatedKpi);
            return Result.Success(kpiDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating KPI with ID: {KpiId}", request.KpiId);
            return Error.Failure("KPI.UpdateFailed", "An error occurred while updating the KPI");
        }
    }

    /// <summary>
    /// Updates the contact assignments for a KPI
    /// </summary>
    private async Task UpdateContactAssignmentsAsync(KPI kpi, List<int> contactIds, CancellationToken cancellationToken)
    {
        var kpiContactRepository = _unitOfWork.Repository<KpiContact>();

        // Remove existing assignments
        var existingAssignments = await kpiContactRepository.GetAsync(
            kc => kc.KpiId == kpi.KpiId, cancellationToken);

        if (existingAssignments.Any())
        {
            await kpiContactRepository.DeleteRangeAsync(existingAssignments, cancellationToken);
        }

        // Add new assignments
        if (contactIds.Any())
        {
            var newAssignments = contactIds.Select(contactId => new KpiContact
            {
                KpiId = kpi.KpiId,
                ContactId = contactId
            });

            await kpiContactRepository.AddRangeAsync(newAssignments, cancellationToken);
        }

        _logger.LogInformation("Updated contact assignments for KPI {KpiId}. Assigned to {Count} contacts",
            kpi.KpiId, contactIds.Count);
    }
}
