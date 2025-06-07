using MediatR;
using Microsoft.EntityFrameworkCore;
using MonitoringGrid.Api.CQRS.Queries.Kpi;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Interfaces;

namespace MonitoringGrid.Api.CQRS.Handlers.Kpi;

/// <summary>
/// Optimized handler for getting KPIs with projections and performance optimizations
/// </summary>
public class GetKpisOptimizedQueryHandler : IRequestHandler<GetKpisOptimizedQuery, PagedResult<KpiSummaryDto>>
{
    private readonly IProjectionRepository<KPI> _projectionRepository;
    private readonly ILogger<GetKpisOptimizedQueryHandler> _logger;

    public GetKpisOptimizedQueryHandler(
        IProjectionRepository<KPI> projectionRepository,
        ILogger<GetKpisOptimizedQueryHandler> logger)
    {
        _projectionRepository = projectionRepository;
        _logger = logger;
    }

    public async Task<PagedResult<KpiSummaryDto>> Handle(GetKpisOptimizedQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Executing optimized KPI query with filters: IsActive={IsActive}, Owner={Owner}, Priority={Priority}",
            request.IsActive, request.Owner, request.Priority);

        // Build predicate for filtering
        var predicate = BuildPredicate(request);

        // Define projection to minimize data transfer
        var projection = BuildProjection();

        // Define ordering
        var orderBy = BuildOrderBy(request.SortBy, request.SortDescending);

        // Execute optimized query with projection
        var result = await _projectionRepository.GetPagedProjectedAsync(
            predicate,
            projection,
            orderBy.Expression,
            orderBy.Descending,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        _logger.LogDebug("Optimized KPI query returned {Count} of {Total} records in page {Page}",
            result.Items.Count(), result.TotalCount, result.PageNumber);

        return result;
    }

    private static System.Linq.Expressions.Expression<Func<KPI, bool>>? BuildPredicate(GetKpisOptimizedQuery request)
    {
        System.Linq.Expressions.Expression<Func<KPI, bool>>? predicate = null;

        if (request.IsActive.HasValue)
        {
            predicate = k => k.IsActive == request.IsActive.Value;
        }

        if (!string.IsNullOrEmpty(request.Owner))
        {
            System.Linq.Expressions.Expression<Func<KPI, bool>> ownerPredicate = k => k.Owner.Contains(request.Owner);
            predicate = predicate == null ? ownerPredicate : CombinePredicates(predicate, ownerPredicate);
        }

        if (request.Priority.HasValue)
        {
            System.Linq.Expressions.Expression<Func<KPI, bool>> priorityPredicate = k => k.Priority == request.Priority.Value;
            predicate = predicate == null ? priorityPredicate : CombinePredicates(predicate, priorityPredicate);
        }

        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            System.Linq.Expressions.Expression<Func<KPI, bool>> searchPredicate = k =>
                k.Indicator.Contains(request.SearchTerm) ||
                k.Owner.Contains(request.SearchTerm) ||
                k.DescriptionTemplate.Contains(request.SearchTerm);
            predicate = predicate == null ? searchPredicate : CombinePredicates(predicate, searchPredicate);
        }

        return predicate;
    }

    private static System.Linq.Expressions.Expression<Func<KPI, KpiSummaryDto>> BuildProjection()
    {
        return k => new KpiSummaryDto
        {
            KpiId = k.KpiId,
            Indicator = k.Indicator,
            Owner = k.Owner,
            Priority = k.Priority,
            Frequency = k.Frequency,
            IsActive = k.IsActive,
            LastRun = k.LastRun,
            KpiType = k.KpiType,
            Deviation = k.Deviation,
            CooldownMinutes = k.CooldownMinutes
        };
    }

    private static (System.Linq.Expressions.Expression<Func<KPI, object>> Expression, bool Descending) BuildOrderBy(string? sortBy, bool sortDescending)
    {
        return sortBy?.ToLower() switch
        {
            "indicator" => (k => k.Indicator, sortDescending),
            "owner" => (k => k.Owner, sortDescending),
            "priority" => (k => k.Priority, sortDescending),
            "frequency" => (k => k.Frequency, sortDescending),
            "lastrun" => (k => k.LastRun ?? DateTime.MinValue, sortDescending),
            "isactive" => (k => k.IsActive, sortDescending),
            _ => (k => k.Indicator, sortDescending)
        };
    }

    private static System.Linq.Expressions.Expression<Func<KPI, bool>> CombinePredicates(
        System.Linq.Expressions.Expression<Func<KPI, bool>> first,
        System.Linq.Expressions.Expression<Func<KPI, bool>> second)
    {
        var parameter = System.Linq.Expressions.Expression.Parameter(typeof(KPI), "k");
        var firstBody = ReplaceParameter(first.Body, first.Parameters[0], parameter);
        var secondBody = ReplaceParameter(second.Body, second.Parameters[0], parameter);
        var combined = System.Linq.Expressions.Expression.AndAlso(firstBody, secondBody);
        return System.Linq.Expressions.Expression.Lambda<Func<KPI, bool>>(combined, parameter);
    }

    private static System.Linq.Expressions.Expression ReplaceParameter(
        System.Linq.Expressions.Expression expression,
        System.Linq.Expressions.ParameterExpression oldParameter,
        System.Linq.Expressions.ParameterExpression newParameter)
    {
        return new ParameterReplacer(oldParameter, newParameter).Visit(expression);
    }

    private class ParameterReplacer : System.Linq.Expressions.ExpressionVisitor
    {
        private readonly System.Linq.Expressions.ParameterExpression _oldParameter;
        private readonly System.Linq.Expressions.ParameterExpression _newParameter;

        public ParameterReplacer(System.Linq.Expressions.ParameterExpression oldParameter, System.Linq.Expressions.ParameterExpression newParameter)
        {
            _oldParameter = oldParameter;
            _newParameter = newParameter;
        }

        protected override System.Linq.Expressions.Expression VisitParameter(System.Linq.Expressions.ParameterExpression node)
        {
            return node == _oldParameter ? _newParameter : base.VisitParameter(node);
        }
    }
}

/// <summary>
/// Optimized handler for KPI dashboard data with aggregations
/// </summary>
public class GetKpiDashboardOptimizedQueryHandler : IRequestHandler<GetKpiDashboardOptimizedQuery, KpiDashboardOptimizedDto>
{
    private readonly IProjectionRepository<KPI> _kpiProjectionRepository;
    private readonly IProjectionRepository<HistoricalData> _historyProjectionRepository;
    private readonly ILogger<GetKpiDashboardOptimizedQueryHandler> _logger;

    public GetKpiDashboardOptimizedQueryHandler(
        IProjectionRepository<KPI> kpiProjectionRepository,
        IProjectionRepository<HistoricalData> historyProjectionRepository,
        ILogger<GetKpiDashboardOptimizedQueryHandler> logger)
    {
        _kpiProjectionRepository = kpiProjectionRepository;
        _historyProjectionRepository = historyProjectionRepository;
        _logger = logger;
    }

    public async Task<KpiDashboardOptimizedDto> Handle(GetKpiDashboardOptimizedQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Executing optimized dashboard query for {Days} days", request.Days);

        var startDate = DateTime.UtcNow.AddDays(-request.Days);

        // Execute multiple optimized queries in parallel
        var kpiCountsTask = GetKpiCountsAsync(cancellationToken);
        var recentExecutionsTask = GetRecentExecutionsAsync(startDate, cancellationToken);
        var performanceMetricsTask = GetPerformanceMetricsAsync(startDate, cancellationToken);
        var executionTrendTask = GetExecutionTrendAsync(startDate, cancellationToken);

        await Task.WhenAll(kpiCountsTask, recentExecutionsTask, performanceMetricsTask, executionTrendTask);

        var kpiCounts = await kpiCountsTask;
        var recentExecutions = await recentExecutionsTask;
        var performanceMetrics = await performanceMetricsTask;
        var executionTrend = await executionTrendTask;

        var dashboard = new KpiDashboardOptimizedDto
        {
            TotalKpis = kpiCounts.TotalKpis,
            ActiveKpis = kpiCounts.ActiveKpis,
            InactiveKpis = kpiCounts.InactiveKpis,
            RecentExecutions = recentExecutions,
            PerformanceMetrics = performanceMetrics,
            ExecutionTrend = executionTrend,
            SystemHealth = DetermineSystemHealth(kpiCounts, performanceMetrics)
        };

        _logger.LogDebug("Dashboard query completed: {TotalKpis} KPIs, {ActiveKpis} active, {RecentExecutions} recent executions",
            dashboard.TotalKpis, dashboard.ActiveKpis, dashboard.RecentExecutions.Count);

        return dashboard;
    }

    private async Task<(int TotalKpis, int ActiveKpis, int InactiveKpis)> GetKpiCountsAsync(CancellationToken cancellationToken)
    {
        var totalKpis = await _kpiProjectionRepository.CountAsync(null, cancellationToken);
        var activeKpis = await _kpiProjectionRepository.CountAsync(k => k.IsActive, cancellationToken);
        var inactiveKpis = totalKpis - activeKpis;

        return (totalKpis, activeKpis, inactiveKpis);
    }

    private async Task<List<KpiExecutionSummaryDto>> GetRecentExecutionsAsync(DateTime startDate, CancellationToken cancellationToken)
    {
        return (await _historyProjectionRepository.GetTopProjectedAsync(
            h => h.Timestamp >= startDate,
            h => new KpiExecutionSummaryDto
            {
                KpiId = h.KpiId,
                Indicator = h.MetricKey,
                ExecutionTime = h.Timestamp,
                Success = h.IsSuccessful,
                CurrentValue = h.Value,
                DeviationPercent = h.DeviationPercent,
                ExecutionTimeMs = h.ExecutionTimeMs ?? 0
            },
            h => h.Timestamp,
            10,
            true,
            cancellationToken)).ToList();
    }

    private async Task<KpiPerformanceMetricsDto> GetPerformanceMetricsAsync(DateTime startDate, CancellationToken cancellationToken)
    {
        var executions = await _historyProjectionRepository.GetProjectedAsync(
            h => h.Timestamp >= startDate,
            h => new { h.IsSuccessful, h.ExecutionTimeMs },
            cancellationToken);

        var executionsList = executions.ToList();
        var totalExecutions = executionsList.Count;
        var successfulExecutions = executionsList.Count(e => e.IsSuccessful);

        return new KpiPerformanceMetricsDto
        {
            TotalExecutionsToday = totalExecutions,
            SuccessfulExecutions = successfulExecutions,
            FailedExecutions = totalExecutions - successfulExecutions,
            SuccessRate = totalExecutions > 0 ? (double)successfulExecutions / totalExecutions * 100 : 0,
            AverageExecutionTimeMs = executionsList.Where(e => e.ExecutionTimeMs.HasValue)
                .Average(e => e.ExecutionTimeMs ?? 0)
        };
    }

    private async Task<List<KpiTrendDto>> GetExecutionTrendAsync(DateTime startDate, CancellationToken cancellationToken)
    {
        return (await _historyProjectionRepository.GetGroupedProjectedAsync(
            h => h.Timestamp >= startDate,
            h => h.Timestamp.Date,
            g => new KpiTrendDto
            {
                Date = g.Key,
                ExecutionCount = g.Count(),
                SuccessCount = g.Count(h => h.IsSuccessful),
                FailureCount = g.Count(h => !h.IsSuccessful),
                AverageExecutionTime = g.Where(h => h.ExecutionTimeMs.HasValue).Average(h => h.ExecutionTimeMs ?? 0)
            },
            cancellationToken)).OrderBy(t => t.Date).ToList();
    }

    private static string DetermineSystemHealth((int TotalKpis, int ActiveKpis, int InactiveKpis) kpiCounts, KpiPerformanceMetricsDto performance)
    {
        if (performance.SuccessRate >= 95) return "Excellent";
        if (performance.SuccessRate >= 90) return "Good";
        if (performance.SuccessRate >= 80) return "Fair";
        if (performance.SuccessRate >= 70) return "Poor";
        return "Critical";
    }
}
