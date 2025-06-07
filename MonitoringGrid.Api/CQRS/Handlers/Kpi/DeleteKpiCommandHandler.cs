using MonitoringGrid.Api.Common;
using MonitoringGrid.Api.CQRS.Commands;
using MonitoringGrid.Api.CQRS.Commands.Kpi;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Interfaces;

namespace MonitoringGrid.Api.CQRS.Handlers.Kpi;

/// <summary>
/// Handler for deleting a KPI
/// </summary>
public class DeleteKpiCommandHandler : ICommandHandler<DeleteKpiCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteKpiCommandHandler> _logger;

    public DeleteKpiCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<DeleteKpiCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(DeleteKpiCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting KPI with ID: {KpiId}", request.KpiId);

        var kpiRepository = _unitOfWork.Repository<KPI>();
        var kpi = await kpiRepository.GetByIdAsync(request.KpiId, cancellationToken);

        if (kpi == null)
        {
            _logger.LogWarning("KPI with ID {KpiId} not found", request.KpiId);
            return Error.NotFound("KPI", request.KpiId);
        }

        try
        {
            // Delete the KPI
            await kpiRepository.DeleteAsync(kpi, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully deleted KPI {Indicator} with ID {KpiId}", kpi.Indicator, request.KpiId);
            return Result.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting KPI with ID: {KpiId}", request.KpiId);
            return Error.Failure("KPI.DeleteFailed", "An error occurred while deleting the KPI");
        }
    }
}
