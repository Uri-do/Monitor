using Microsoft.EntityFrameworkCore;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Core.Models;
using MonitoringGrid.Infrastructure.Data;

namespace MonitoringGrid.Infrastructure.Repositories;

/// <summary>
/// Specialized repository implementation for AlertLog with complex query support
/// </summary>
public class AlertRepository : Repository<AlertLog>, IAlertRepository
{
    public AlertRepository(MonitoringContext context) : base(context)
    {
    }

    public async Task<PaginatedAlerts<AlertLog>> GetAlertsWithFilteringAsync(AlertFilter filter)
    {
        // Validate pagination parameters
        filter.Page = Math.Max(1, filter.Page);
        filter.PageSize = Math.Max(1, Math.Min(100, filter.PageSize)); // Limit to max 100 items per page

        var query = _dbSet
            .Include(a => a.KPI)
            .AsQueryable();

        // Apply filters
        if (filter.StartDate.HasValue)
            query = query.Where(a => a.TriggerTime >= filter.StartDate.Value);

        if (filter.EndDate.HasValue)
            query = query.Where(a => a.TriggerTime <= filter.EndDate.Value);

        if (filter.KpiIds?.Any() == true)
            query = query.Where(a => filter.KpiIds.Contains(a.KpiId));

        if (filter.Owners?.Any() == true)
            query = query.Where(a => filter.Owners.Contains(a.KPI.Owner));

        if (filter.IsResolved.HasValue)
            query = query.Where(a => a.IsResolved == filter.IsResolved.Value);

        if (filter.SentVia?.Any() == true)
        {
            // Convert string values to byte values for comparison
            var sentViaBytes = filter.SentVia.Select(sv => sv.ToLower() switch
            {
                "sms" => (byte)1,
                "email" => (byte)2,
                "both" => (byte)3,
                "sms + email" => (byte)3,
                _ => (byte)0
            }).Where(b => b > 0).ToList();

            if (sentViaBytes.Any())
                query = query.Where(a => sentViaBytes.Contains(a.SentVia));
        }

        if (filter.MinDeviation.HasValue)
            query = query.Where(a => a.DeviationPercent >= filter.MinDeviation.Value);

        if (filter.MaxDeviation.HasValue)
            query = query.Where(a => a.DeviationPercent <= filter.MaxDeviation.Value);

        if (!string.IsNullOrEmpty(filter.SearchText))
            query = query.Where(a => a.Message.Contains(filter.SearchText) ||
                                   a.KPI.Indicator.Contains(filter.SearchText));

        // Get total count before pagination
        var totalCount = await query.CountAsync();

        // Apply sorting
        query = filter.SortDirection.ToLower() == "asc"
            ? ApplySortingAscending(query, filter.SortBy)
            : ApplySortingDescending(query, filter.SortBy);

        // Apply pagination
        var alerts = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new PaginatedAlerts<AlertLog>
        {
            Alerts = alerts,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / filter.PageSize),
            HasNextPage = filter.Page * filter.PageSize < totalCount,
            HasPreviousPage = filter.Page > 1
        };
    }

    public async Task<AlertStatistics> GetStatisticsAsync(int days = 30)
    {
        var startDate = DateTime.UtcNow.AddDays(-days);
        var now = DateTime.UtcNow;
        var today = now.Date;

        var alerts = await _dbSet
            .Include(a => a.KPI)
            .Where(a => a.TriggerTime >= startDate)
            .ToListAsync();

        var dailyTrend = alerts
            .GroupBy(a => a.TriggerTime.Date)
            .Select(g => new AlertTrend
            {
                Date = g.Key,
                AlertCount = g.Count(),
                CriticalCount = g.Count(a => a.DeviationPercent >= 50),
                HighCount = g.Count(a => a.DeviationPercent >= 25 && a.DeviationPercent < 50),
                MediumCount = g.Count(a => a.DeviationPercent >= 10 && a.DeviationPercent < 25),
                LowCount = g.Count(a => a.DeviationPercent < 10)
            })
            .OrderBy(t => t.Date)
            .ToList();

        var topAlertingKpis = alerts
            .GroupBy(a => new { a.KpiId, a.KPI.Indicator, a.KPI.Owner })
            .Select(g => new KpiAlertSummary
            {
                KpiId = g.Key.KpiId,
                Indicator = g.Key.Indicator,
                Owner = g.Key.Owner,
                AlertCount = g.Count(),
                UnresolvedCount = g.Count(a => !a.IsResolved),
                LastAlert = g.Max(a => a.TriggerTime),
                AverageDeviation = g.Average(a => a.DeviationPercent ?? 0)
            })
            .OrderByDescending(k => k.AlertCount)
            .Take(10)
            .ToList();

        var resolvedAlerts = alerts.Where(a => a.IsResolved && a.ResolvedTime.HasValue).ToList();
        var averageResolutionTime = resolvedAlerts.Any()
            ? resolvedAlerts.Average(a => (a.ResolvedTime!.Value - a.TriggerTime).TotalHours)
            : 0;

        return new AlertStatistics
        {
            TotalAlerts = alerts.Count,
            UnresolvedAlerts = alerts.Count(a => !a.IsResolved),
            ResolvedAlerts = alerts.Count(a => a.IsResolved),
            AlertsToday = alerts.Count(a => a.TriggerTime >= today),
            AlertsThisWeek = alerts.Count(a => a.TriggerTime >= today.AddDays(-7)),
            AlertsThisMonth = alerts.Count(a => a.TriggerTime >= today.AddDays(-30)),
            CriticalAlerts = alerts.Count(a => a.DeviationPercent >= 50),
            HighPriorityAlerts = alerts.Count(a => a.DeviationPercent >= 25),
            AverageResolutionTimeHours = (decimal)averageResolutionTime,
            DailyTrend = dailyTrend,
            TopAlertingKpis = topAlertingKpis
        };
    }

    public async Task<AlertDashboard> GetDashboardAsync()
    {
        var now = DateTime.UtcNow;
        var today = now.Date;
        var lastHour = now.AddHours(-1);

        var alertsToday = await _dbSet.CountAsync(a => a.TriggerTime >= today);
        var unresolvedAlerts = await _dbSet.CountAsync(a => !a.IsResolved);
        var criticalAlerts = await _dbSet.CountAsync(a => !a.IsResolved && a.DeviationPercent >= 50);
        var alertsLastHour = await _dbSet.CountAsync(a => a.TriggerTime >= lastHour);

        // Calculate trend (compare with yesterday)
        var yesterday = today.AddDays(-1);
        var alertsYesterday = await _dbSet.CountAsync(a => a.TriggerTime >= yesterday && a.TriggerTime < today);
        var alertTrendPercentage = alertsYesterday > 0
            ? ((decimal)(alertsToday - alertsYesterday) / alertsYesterday) * 100
            : alertsToday > 0 ? 100 : 0;

        // Hourly trend for last 24 hours
        var hourlyTrend = new List<AlertTrend>();
        for (int i = 23; i >= 0; i--)
        {
            var hourStart = now.AddHours(-i).Date.AddHours(now.AddHours(-i).Hour);
            var hourEnd = hourStart.AddHours(1);

            var hourlyAlerts = await _dbSet
                .Where(a => a.TriggerTime >= hourStart && a.TriggerTime < hourEnd)
                .ToListAsync();

            hourlyTrend.Add(new AlertTrend
            {
                Date = hourStart,
                AlertCount = hourlyAlerts.Count,
                CriticalCount = hourlyAlerts.Count(a => a.DeviationPercent >= 50),
                HighCount = hourlyAlerts.Count(a => a.DeviationPercent >= 25 && a.DeviationPercent < 50),
                MediumCount = hourlyAlerts.Count(a => a.DeviationPercent >= 10 && a.DeviationPercent < 25),
                LowCount = hourlyAlerts.Count(a => a.DeviationPercent < 10)
            });
        }

        return new AlertDashboard
        {
            TotalAlertsToday = alertsToday,
            UnresolvedAlerts = unresolvedAlerts,
            CriticalAlerts = criticalAlerts,
            AlertsLastHour = alertsLastHour,
            AlertTrendPercentage = alertTrendPercentage,
            HourlyTrend = hourlyTrend
        };
    }

    public async Task<IEnumerable<AlertLog>> GetAlertsByKpiAsync(int kpiId, int days = 30)
    {
        var startDate = DateTime.UtcNow.AddDays(-days);
        return await _dbSet
            .Include(a => a.KPI)
            .Where(a => a.KpiId == kpiId && a.TriggerTime >= startDate)
            .OrderByDescending(a => a.TriggerTime)
            .ToListAsync();
    }

    public async Task<int> GetUnresolvedCountAsync()
    {
        return await _dbSet.CountAsync(a => !a.IsResolved);
    }

    public async Task<IEnumerable<AlertLog>> GetAlertsBySeverityAsync(string severity, int days = 30)
    {
        var startDate = DateTime.UtcNow.AddDays(-days);
        
        return severity.ToLower() switch
        {
            "critical" => await _dbSet.Where(a => a.DeviationPercent >= 50 && a.TriggerTime >= startDate).ToListAsync(),
            "high" => await _dbSet.Where(a => a.DeviationPercent >= 25 && a.DeviationPercent < 50 && a.TriggerTime >= startDate).ToListAsync(),
            "medium" => await _dbSet.Where(a => a.DeviationPercent >= 10 && a.DeviationPercent < 25 && a.TriggerTime >= startDate).ToListAsync(),
            "low" => await _dbSet.Where(a => a.DeviationPercent < 10 && a.TriggerTime >= startDate).ToListAsync(),
            _ => await _dbSet.Where(a => a.TriggerTime >= startDate).ToListAsync()
        };
    }

    public async Task<int> BulkResolveAlertsAsync(IEnumerable<long> alertIds, string resolvedBy, string? resolutionNotes = null)
    {
        var alerts = await _dbSet
            .Where(a => alertIds.Contains(a.AlertId) && !a.IsResolved)
            .ToListAsync();

        if (!alerts.Any())
            return 0;

        var resolvedTime = DateTime.UtcNow;
        foreach (var alert in alerts)
        {
            alert.IsResolved = true;
            alert.ResolvedTime = resolvedTime;
            alert.ResolvedBy = resolvedBy;

            if (!string.IsNullOrEmpty(resolutionNotes))
            {
                alert.Details = string.IsNullOrEmpty(alert.Details) 
                    ? $"Resolution: {resolutionNotes}"
                    : $"{alert.Details}\n\nResolution: {resolutionNotes}";
            }
        }

        await _context.SaveChangesAsync();
        return alerts.Count;
    }

    private static IQueryable<AlertLog> ApplySortingAscending(IQueryable<AlertLog> query, string sortBy)
    {
        return sortBy.ToLower() switch
        {
            "triggertime" => query.OrderBy(a => a.TriggerTime),
            "kpi" => query.OrderBy(a => a.KPI.Indicator),
            "owner" => query.OrderBy(a => a.KPI.Owner),
            "deviation" => query.OrderBy(a => a.DeviationPercent),
            "resolved" => query.OrderBy(a => a.IsResolved),
            _ => query.OrderBy(a => a.TriggerTime)
        };
    }

    private static IQueryable<AlertLog> ApplySortingDescending(IQueryable<AlertLog> query, string sortBy)
    {
        return sortBy.ToLower() switch
        {
            "triggertime" => query.OrderByDescending(a => a.TriggerTime),
            "kpi" => query.OrderByDescending(a => a.KPI.Indicator),
            "owner" => query.OrderByDescending(a => a.KPI.Owner),
            "deviation" => query.OrderByDescending(a => a.DeviationPercent),
            "resolved" => query.OrderByDescending(a => a.IsResolved),
            _ => query.OrderByDescending(a => a.TriggerTime)
        };
    }
}
