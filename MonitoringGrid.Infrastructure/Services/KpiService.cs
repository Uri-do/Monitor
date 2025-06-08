using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Infrastructure.Data;

namespace MonitoringGrid.Infrastructure.Services;

/// <summary>
/// Service implementation for KPI management operations
/// </summary>
public class KpiService : IKpiService
{
    private readonly MonitoringContext _context;
    private readonly ILogger<KpiService> _logger;

    public KpiService(MonitoringContext context, ILogger<KpiService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<KPI>> GetAllKpisAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving all KPIs");
        
        return await _context.KPIs
            .Include(k => k.KpiContacts)
                .ThenInclude(kc => kc.Contact)
            .OrderBy(k => k.Indicator)
            .ToListAsync(cancellationToken);
    }

    public async Task<KPI?> GetKpiByIdAsync(int kpiId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving KPI with ID {KpiId}", kpiId);
        
        return await _context.KPIs
            .Include(k => k.KpiContacts)
                .ThenInclude(kc => kc.Contact)
            .FirstOrDefaultAsync(k => k.KpiId == kpiId, cancellationToken);
    }

    public async Task<List<KPI>> GetActiveKpisAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving active KPIs");
        
        return await _context.KPIs
            .Include(k => k.KpiContacts)
                .ThenInclude(kc => kc.Contact)
            .Where(k => k.IsActive)
            .OrderBy(k => k.Indicator)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<KPI>> GetDueKpisAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving KPIs due for execution using whole time scheduling");

        var allActiveKpis = await _context.KPIs
            .Include(k => k.KpiContacts)
                .ThenInclude(kc => kc.Contact)
            .Where(k => k.IsActive)
            .ToListAsync(cancellationToken);

        // Filter using whole time scheduling logic
        var dueKpis = allActiveKpis
            .Where(k => MonitoringGrid.Infrastructure.Utilities.WholeTimeScheduler
                .IsKpiDueForWholeTimeExecution(k.LastRun, k.Frequency))
            .OrderBy(k => k.LastRun ?? DateTime.MinValue)
            .ToList();

        _logger.LogDebug("Found {Count} KPIs due for execution", dueKpis.Count);
        return dueKpis;
    }

    public async Task<KPI> CreateKpiAsync(KPI kpi, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating new KPI: {Indicator}", kpi.Indicator);
        
        // Validate the KPI
        var validationResult = await ValidateKpiAsync(kpi, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ArgumentException($"KPI validation failed: {string.Join(", ", validationResult.Errors)}");
        }

        kpi.CreatedDate = DateTime.UtcNow;
        kpi.ModifiedDate = DateTime.UtcNow;

        _context.KPIs.Add(kpi);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created KPI {KpiId}: {Indicator}", kpi.KpiId, kpi.Indicator);
        return kpi;
    }

    public async Task<KPI> UpdateKpiAsync(KPI kpi, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating KPI {KpiId}: {Indicator}", kpi.KpiId, kpi.Indicator);
        
        // Validate the KPI
        var validationResult = await ValidateKpiAsync(kpi, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ArgumentException($"KPI validation failed: {string.Join(", ", validationResult.Errors)}");
        }

        var existingKpi = await _context.KPIs.FindAsync(new object[] { kpi.KpiId }, cancellationToken);
        if (existingKpi == null)
        {
            throw new ArgumentException($"KPI with ID {kpi.KpiId} not found");
        }

        // Update properties
        existingKpi.Indicator = kpi.Indicator;
        existingKpi.Owner = kpi.Owner;
        existingKpi.Priority = kpi.Priority;
        existingKpi.ScheduleConfiguration = kpi.ScheduleConfiguration; // Update schedule configuration instead of frequency
        existingKpi.LastMinutes = kpi.LastMinutes;
        existingKpi.Deviation = kpi.Deviation;
        existingKpi.SpName = kpi.SpName;
        existingKpi.SubjectTemplate = kpi.SubjectTemplate;
        existingKpi.DescriptionTemplate = kpi.DescriptionTemplate;
        existingKpi.IsActive = kpi.IsActive;
        existingKpi.CooldownMinutes = kpi.CooldownMinutes;
        existingKpi.MinimumThreshold = kpi.MinimumThreshold;
        existingKpi.ModifiedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated KPI {KpiId}: {Indicator}", kpi.KpiId, kpi.Indicator);
        return existingKpi;
    }

    public async Task<bool> DeleteKpiAsync(int kpiId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting KPI {KpiId}", kpiId);
        
        var kpi = await _context.KPIs.FindAsync(new object[] { kpiId }, cancellationToken);
        if (kpi == null)
        {
            _logger.LogWarning("KPI {KpiId} not found for deletion", kpiId);
            return false;
        }

        _context.KPIs.Remove(kpi);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted KPI {KpiId}: {Indicator}", kpiId, kpi.Indicator);
        return true;
    }

    public async Task<List<KPI>> GetKpisByOwnerAsync(string owner, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving KPIs for owner: {Owner}", owner);
        
        return await _context.KPIs
            .Include(k => k.KpiContacts)
                .ThenInclude(kc => kc.Contact)
            .Where(k => k.Owner == owner)
            .OrderBy(k => k.Indicator)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<KPI>> GetKpisByPriorityAsync(byte priority, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving KPIs with priority: {Priority}", priority);
        
        return await _context.KPIs
            .Include(k => k.KpiContacts)
                .ThenInclude(kc => kc.Contact)
            .Where(k => k.Priority == priority)
            .OrderBy(k => k.Indicator)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ActivateKpiAsync(int kpiId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Activating KPI {KpiId}", kpiId);
        
        var kpi = await _context.KPIs.FindAsync(new object[] { kpiId }, cancellationToken);
        if (kpi == null)
        {
            _logger.LogWarning("KPI {KpiId} not found for activation", kpiId);
            return false;
        }

        kpi.IsActive = true;
        kpi.ModifiedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Activated KPI {KpiId}: {Indicator}", kpiId, kpi.Indicator);
        return true;
    }

    public async Task<bool> DeactivateKpiAsync(int kpiId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deactivating KPI {KpiId}", kpiId);
        
        var kpi = await _context.KPIs.FindAsync(new object[] { kpiId }, cancellationToken);
        if (kpi == null)
        {
            _logger.LogWarning("KPI {KpiId} not found for deactivation", kpiId);
            return false;
        }

        kpi.IsActive = false;
        kpi.ModifiedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deactivated KPI {KpiId}: {Indicator}", kpiId, kpi.Indicator);
        return true;
    }

    public async Task<KpiStatistics> GetKpiStatisticsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving KPI statistics");
        
        var totalKpis = await _context.KPIs.CountAsync(cancellationToken);
        var activeKpis = await _context.KPIs.CountAsync(k => k.IsActive, cancellationToken);
        var runningKpis = await _context.KPIs.CountAsync(k => k.IsCurrentlyRunning, cancellationToken);
        
        var now = DateTime.UtcNow;
        var dueKpis = await _context.KPIs.CountAsync(k => 
            k.IsActive && 
            (!k.LastRun.HasValue || k.LastRun.Value.AddMinutes(k.Frequency) <= now), 
            cancellationToken);

        return new KpiStatistics
        {
            TotalKpis = totalKpis,
            ActiveKpis = activeKpis,
            InactiveKpis = totalKpis - activeKpis,
            RunningKpis = runningKpis,
            DueKpis = dueKpis
        };
    }

    public Task<ValidationResult> ValidateKpiAsync(KPI kpi, CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        // Basic validation
        if (string.IsNullOrWhiteSpace(kpi.Indicator))
            errors.Add("Indicator is required");

        if (string.IsNullOrWhiteSpace(kpi.Owner))
            errors.Add("Owner is required");

        if (string.IsNullOrWhiteSpace(kpi.SpName))
            errors.Add("Stored procedure name is required");

        if (kpi.Priority < 1 || kpi.Priority > 2)
            errors.Add("Priority must be 1 (SMS + Email) or 2 (Email Only)");

        if (kpi.Frequency <= 0)
            errors.Add("Frequency must be greater than 0");

        if (kpi.LastMinutes <= 0)
            errors.Add("LastMinutes must be greater than 0");

        if (kpi.Deviation < 0)
            errors.Add("Deviation cannot be negative");

        if (kpi.CooldownMinutes < 0)
            errors.Add("Cooldown minutes cannot be negative");

        // Business logic warnings
        if (kpi.Frequency < 5)
            warnings.Add("Frequency less than 5 minutes may cause performance issues");

        if (kpi.CooldownMinutes > kpi.Frequency)
            warnings.Add("Cooldown period is longer than execution frequency");

        var result = new ValidationResult
        {
            IsValid = !errors.Any(),
            Errors = errors,
            Warnings = warnings
        };

        return Task.FromResult(result);
    }
}
