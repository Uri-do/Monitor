using AutoMapper;
using Microsoft.EntityFrameworkCore;
using MonitoringGrid.Api.Common;
using MonitoringGrid.Api.CQRS.Queries;
using MonitoringGrid.Api.CQRS.Queries.Kpi;
using MonitoringGrid.Api.DTOs;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Core.Specifications;

namespace MonitoringGrid.Api.CQRS.Handlers.Kpi;

/// <summary>
/// Handler for getting KPIs with optional filtering
/// </summary>
public class GetKpisQueryHandler : IQueryHandler<GetKpisQuery, List<KpiDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<GetKpisQueryHandler> _logger;

    public GetKpisQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<GetKpisQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<List<KpiDto>>> Handle(GetKpisQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Getting KPIs with filters - IsActive: {IsActive}, Owner: {Owner}, Priority: {Priority}",
                request.IsActive, request.Owner, request.Priority);

            var kpiRepository = _unitOfWork.Repository<KPI>();
            IEnumerable<KPI> kpis;

            if (!string.IsNullOrEmpty(request.Owner))
            {
                // Use specification for owner filtering
                var specification = new KpisByOwnerSpecification(request.Owner);
                kpis = await kpiRepository.GetAsync(specification, cancellationToken);
            }
            else
            {
                kpis = await kpiRepository.GetWithThenIncludesAsync(
                    k => true,
                    k => k.Indicator,
                    true,
                    query => query.Include(k => k.KpiContacts).ThenInclude(kc => kc.Contact));
            }

            // Apply additional filters
            if (request.IsActive.HasValue)
            {
                kpis = kpis.Where(k => k.IsActive == request.IsActive.Value);
            }

            if (request.Priority.HasValue)
            {
                kpis = kpis.Where(k => k.Priority == request.Priority.Value);
            }

            var kpiDtos = _mapper.Map<List<KpiDto>>(kpis.ToList());

            _logger.LogDebug("Retrieved {Count} KPIs", kpiDtos.Count);
            return Result.Success(kpiDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving KPIs");
            return Error.Failure("KPI.RetrieveAllFailed", "An error occurred while retrieving KPIs");
        }
    }
}
