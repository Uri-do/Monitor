using MonitoringGrid.Api.Common;
using MonitoringGrid.Api.CQRS.Commands;
using MonitoringGrid.Api.CQRS.Commands.Kpi;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Interfaces;

namespace MonitoringGrid.Api.CQRS.Handlers.Kpi;

/// <summary>
/// Handler for bulk KPI operations
/// </summary>
public class BulkKpiOperationCommandHandler : ICommandHandler<BulkKpiOperationCommand, string>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<BulkKpiOperationCommandHandler> _logger;

    public BulkKpiOperationCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<BulkKpiOperationCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<string>> Handle(BulkKpiOperationCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Performing bulk operation {Operation} on {Count} KPIs",
            request.Operation, request.KpiIds.Count);

        if (!request.KpiIds.Any())
        {
            return Error.Validation("KpiIds", "No KPI IDs provided");
        }

        try
        {
            var kpiRepository = _unitOfWork.Repository<KPI>();
            var allKpis = await kpiRepository.GetAllAsync(cancellationToken);
            var kpis = allKpis.Where(k => request.KpiIds.Contains(k.KpiId)).ToList();

            if (!kpis.Any())
            {
                return Error.NotFound("KPIs", string.Join(", ", request.KpiIds));
            }

            switch (request.Operation.ToLower())
            {
                case "activate":
                    kpis.ForEach(k => k.IsActive = true);
                    break;
                case "deactivate":
                    kpis.ForEach(k => k.IsActive = false);
                    break;
                case "delete":
                    await kpiRepository.DeleteRangeAsync(kpis, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    _logger.LogInformation("Bulk operation {Operation} performed on {Count} KPIs",
                        request.Operation, kpis.Count);
                    return Result.Success($"Operation '{request.Operation}' completed on {kpis.Count} KPIs");
                default:
                    return Error.Validation("Operation", $"Unknown operation: {request.Operation}");
            }

            // For activate/deactivate operations
            foreach (var kpi in kpis)
            {
                await kpiRepository.UpdateAsync(kpi, cancellationToken);
            }
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Bulk operation {Operation} performed on {Count} KPIs",
                request.Operation, kpis.Count);

            return Result.Success($"Operation '{request.Operation}' completed on {kpis.Count} KPIs");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing bulk operation {Operation} on KPIs", request.Operation);
            return Error.Failure("KPI.BulkOperationFailed", "An error occurred while performing the bulk operation");
        }
    }
}
