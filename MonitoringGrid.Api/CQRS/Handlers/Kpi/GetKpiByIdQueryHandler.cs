using AutoMapper;
using Microsoft.EntityFrameworkCore;
using MonitoringGrid.Api.Common;
using MonitoringGrid.Api.CQRS.Queries;
using MonitoringGrid.Api.CQRS.Queries.Kpi;
using MonitoringGrid.Api.DTOs;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Interfaces;

namespace MonitoringGrid.Api.CQRS.Handlers.Kpi;

/// <summary>
/// Handler for getting a KPI by ID
/// </summary>
public class GetKpiByIdQueryHandler : IQueryHandler<GetKpiByIdQuery, KpiDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<GetKpiByIdQueryHandler> _logger;

    public GetKpiByIdQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<GetKpiByIdQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<KpiDto?>> Handle(GetKpiByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Getting KPI with ID: {KpiId}", request.KpiId);

            var kpiRepository = _unitOfWork.Repository<KPI>();
            var kpi = await kpiRepository.GetByIdWithThenIncludesAsync(request.KpiId,
                query => query.Include(k => k.KpiContacts).ThenInclude(kc => kc.Contact));

            if (kpi == null)
            {
                _logger.LogDebug("KPI with ID {KpiId} not found", request.KpiId);
                return Error.NotFound("KPI", request.KpiId);
            }

            var kpiDto = _mapper.Map<KpiDto>(kpi);
            return Result.Success<KpiDto?>(kpiDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving KPI with ID: {KpiId}", request.KpiId);
            return Error.Failure("KPI.RetrieveFailed", "An error occurred while retrieving the KPI");
        }
    }
}
