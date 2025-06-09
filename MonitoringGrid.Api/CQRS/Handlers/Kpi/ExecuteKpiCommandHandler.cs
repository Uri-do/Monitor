using AutoMapper;
using MonitoringGrid.Api.Common;
using MonitoringGrid.Api.CQRS.Commands;
using MonitoringGrid.Api.CQRS.Commands.Kpi;
using MonitoringGrid.Api.DTOs;
using MonitoringGrid.Api.Observability;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Events;
using MonitoringGrid.Core.Interfaces;

namespace MonitoringGrid.Api.CQRS.Handlers.Kpi;

/// <summary>
/// Handler for executing a KPI manually
/// </summary>
public class ExecuteKpiCommandHandler : ICommandHandler<ExecuteKpiCommand, KpiExecutionResultDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IKpiExecutionService _kpiExecutionService;
    private readonly MetricsService _metricsService;
    private readonly ILogger<ExecuteKpiCommandHandler> _logger;

    public ExecuteKpiCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IKpiExecutionService kpiExecutionService,
        MetricsService metricsService,
        ILogger<ExecuteKpiCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _kpiExecutionService = kpiExecutionService;
        _metricsService = metricsService;
        _logger = logger;
    }

    public async Task<Result<KpiExecutionResultDto>> Handle(ExecuteKpiCommand request, CancellationToken cancellationToken)
    {
        using var activity = KpiActivitySource.StartKpiExecution(request.KpiId, "Manual Execution");
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("Executing KPI with ID: {KpiId}", request.KpiId);

        var kpiRepository = _unitOfWork.Repository<KPI>();
        var kpi = await kpiRepository.GetByIdAsync(request.KpiId, cancellationToken);

        if (kpi == null)
        {
            var message = $"KPI with ID {request.KpiId} not found";
            _logger.LogWarning(message);
            return Error.NotFound("KPI", request.KpiId);
        }

        if (!kpi.IsActive)
        {
            var message = $"KPI {kpi.Indicator} is not active";
            _logger.LogWarning(message);
            return Error.BusinessRule("KpiInactive", message);
        }

        try
        {
            // Apply custom frequency if provided
            if (request.CustomFrequency.HasValue)
            {
                kpi.Frequency = request.CustomFrequency.Value;
            }

            // Execute the KPI
            var result = await _kpiExecutionService.ExecuteKpiAsync(kpi, cancellationToken);
            var duration = DateTime.UtcNow - startTime;

            // The KpiExecutionService should have updated LastRun, but let's ensure it's saved
            // by refreshing the entity from the database and updating it again
            var refreshedKpi = await kpiRepository.GetByIdAsync(request.KpiId, cancellationToken);
            if (refreshedKpi != null)
            {
                refreshedKpi.UpdateLastRun();
                await kpiRepository.UpdateAsync(refreshedKpi, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            // Record metrics
            _metricsService.RecordKpiExecution(kpi.Indicator, kpi.Owner, duration.TotalSeconds, result.IsSuccessful);

            var dto = _mapper.Map<KpiExecutionResultDto>(result);
            dto.KpiId = request.KpiId;
            dto.Indicator = kpi.Indicator;

            // Log structured completion
            _logger.LogKpiExecutionCompleted(request.KpiId, kpi.Indicator, duration, result.IsSuccessful, result.GetSummary());

            // Record success in activity
            KpiActivitySource.RecordSuccess(activity, result.GetSummary());
            KpiActivitySource.RecordPerformanceMetrics(activity, (long)duration.TotalMilliseconds);

            return Result.Success(dto);
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _logger.LogError(ex, "Error executing KPI {KpiId} after {Duration}ms", request.KpiId, duration.TotalMilliseconds);

            // Record error in activity
            KpiActivitySource.RecordError(activity, ex);

            return Error.Failure("KPI.ExecutionFailed", "An error occurred while executing the KPI");
        }
    }
}
