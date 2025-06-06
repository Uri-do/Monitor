using MonitoringGrid.Api.Observability;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.EventSourcing;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Core.Services;
using System.Collections.Concurrent;

namespace MonitoringGrid.Api.BackgroundServices;

/// <summary>
/// Enhanced KPI scheduler service with better monitoring and error handling
/// </summary>
public class EnhancedKpiSchedulerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EnhancedKpiSchedulerService> _logger;
    private readonly MetricsService _metricsService;
    private readonly ConcurrentDictionary<int, KpiExecutionInfo> _runningExecutions = new();
    private readonly Timer _healthCheckTimer;
    private DateTime _lastHealthCheck = DateTime.UtcNow;

    public EnhancedKpiSchedulerService(
        IServiceProvider serviceProvider,
        ILogger<EnhancedKpiSchedulerService> logger,
        MetricsService metricsService)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _metricsService = metricsService;
        
        // Health check timer - runs every 30 seconds
        _healthCheckTimer = new Timer(PerformHealthCheck, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Enhanced KPI Scheduler Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessScheduledKpisAsync(stoppingToken);
                await CleanupCompletedExecutionsAsync();
                
                // Wait for next cycle (configurable, default 1 minute)
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("KPI Scheduler Service is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in KPI Scheduler Service main loop");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); // Wait before retrying
            }
        }

        _logger.LogInformation("Enhanced KPI Scheduler Service stopped");
    }

    private async Task ProcessScheduledKpisAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var kpiExecutionService = scope.ServiceProvider.GetRequiredService<IKpiExecutionService>();
        var eventSourcingService = scope.ServiceProvider.GetRequiredService<IEventSourcingService>();

        try
        {
            var kpiRepository = unitOfWork.Repository<KPI>();
            var now = DateTime.UtcNow;

            // Get KPIs that are due for execution
            var dueKpis = await GetDueKpisAsync(kpiRepository, now, cancellationToken);

            _logger.LogInformation("Found {DueKpiCount} KPIs due for execution", dueKpis.Count);

            var executionTasks = new List<Task>();

            foreach (var kpi in dueKpis)
            {
                // Skip if already running
                if (_runningExecutions.ContainsKey(kpi.KpiId))
                {
                    _logger.LogDebug("KPI {KpiId} ({Indicator}) is already running, skipping", kpi.KpiId, kpi.Indicator);
                    continue;
                }

                // Create execution task
                var executionTask = ExecuteKpiWithMonitoringAsync(kpi, kpiExecutionService, eventSourcingService, cancellationToken);
                executionTasks.Add(executionTask);

                // Limit concurrent executions
                if (executionTasks.Count >= 5)
                {
                    await Task.WhenAny(executionTasks);
                    executionTasks.RemoveAll(t => t.IsCompleted);
                }
            }

            // Wait for remaining executions to complete
            if (executionTasks.Any())
            {
                await Task.WhenAll(executionTasks);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing scheduled KPIs");
        }
    }

    private async Task<List<KPI>> GetDueKpisAsync(IRepository<KPI> kpiRepository, DateTime now, CancellationToken cancellationToken)
    {
        var allKpis = await kpiRepository.GetAllAsync(cancellationToken);
        var dueKpis = new List<KPI>();

        foreach (var kpi in allKpis.Where(k => k.IsActive))
        {
            if (IsKpiDue(kpi, now))
            {
                dueKpis.Add(kpi);
            }
        }

        return dueKpis;
    }

    private static bool IsKpiDue(KPI kpi, DateTime now)
    {
        if (!kpi.LastRun.HasValue)
            return true; // Never run before

        var timeSinceLastRun = now - kpi.LastRun.Value;
        var frequencyTimeSpan = TimeSpan.FromMinutes(kpi.Frequency);

        return timeSinceLastRun >= frequencyTimeSpan;
    }

    private async Task ExecuteKpiWithMonitoringAsync(
        KPI kpi, 
        IKpiExecutionService kpiExecutionService, 
        IEventSourcingService eventSourcingService,
        CancellationToken cancellationToken)
    {
        var executionInfo = new KpiExecutionInfo
        {
            KpiId = kpi.KpiId,
            Indicator = kpi.Indicator,
            StartTime = DateTime.UtcNow,
            Status = KpiExecutionStatus.Running
        };

        _runningExecutions.TryAdd(kpi.KpiId, executionInfo);

        using var activity = KpiActivitySource.StartKpiExecution(kpi.KpiId, kpi.Indicator);
        activity?.SetTag("execution.type", "scheduled")
                ?.SetTag("kpi.owner", kpi.Owner)
                ?.SetTag("kpi.frequency", kpi.Frequency);

        try
        {
            _logger.LogKpiExecutionStart(kpi.KpiId, kpi.Indicator, kpi.Owner);

            var result = await kpiExecutionService.ExecuteKpiAsync(kpi, cancellationToken);
            var duration = DateTime.UtcNow - executionInfo.StartTime;

            // Update execution info
            executionInfo.EndTime = DateTime.UtcNow;
            executionInfo.Duration = duration;
            executionInfo.Status = result.IsSuccessful ? KpiExecutionStatus.Completed : KpiExecutionStatus.Failed;
            executionInfo.Result = result;

            // Record metrics
            _metricsService.RecordKpiExecution(kpi.Indicator, kpi.Owner, duration.TotalSeconds, result.IsSuccessful);

            // Update KPI last run time
            using var scope = _serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var kpiRepository = unitOfWork.Repository<KPI>();
            
            kpi.UpdateLastRun();
            await kpiRepository.UpdateAsync(kpi, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            // For now, we'll skip domain events publishing in the background service
            // In a full implementation, this would publish KpiExecutedEvent

            _logger.LogKpiExecutionCompleted(kpi.KpiId, kpi.Indicator, duration, result.IsSuccessful, result.GetSummary());

            KpiActivitySource.RecordSuccess(activity, result.GetSummary());
            KpiActivitySource.RecordPerformanceMetrics(activity, (long)duration.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - executionInfo.StartTime;
            
            executionInfo.EndTime = DateTime.UtcNow;
            executionInfo.Duration = duration;
            executionInfo.Status = KpiExecutionStatus.Failed;
            executionInfo.ErrorMessage = ex.Message;

            _metricsService.RecordKpiExecution(kpi.Indicator, kpi.Owner, duration.TotalSeconds, false);
            _logger.LogKpiExecutionError(kpi.KpiId, kpi.Indicator, ex, duration);

            KpiActivitySource.RecordError(activity, ex);
        }
    }

    private async Task CleanupCompletedExecutionsAsync()
    {
        var completedExecutions = _runningExecutions
            .Where(kvp => kvp.Value.Status != KpiExecutionStatus.Running)
            .Where(kvp => kvp.Value.EndTime.HasValue && DateTime.UtcNow - kvp.Value.EndTime.Value > TimeSpan.FromMinutes(5))
            .ToList();

        foreach (var execution in completedExecutions)
        {
            _runningExecutions.TryRemove(execution.Key, out _);
        }

        if (completedExecutions.Any())
        {
            _logger.LogDebug("Cleaned up {Count} completed KPI executions", completedExecutions.Count);
        }

        await Task.CompletedTask;
    }

    private void PerformHealthCheck(object? state)
    {
        try
        {
            var now = DateTime.UtcNow;
            var runningCount = _runningExecutions.Count(kvp => kvp.Value.Status == KpiExecutionStatus.Running);
            var stuckExecutions = _runningExecutions
                .Where(kvp => kvp.Value.Status == KpiExecutionStatus.Running)
                .Where(kvp => now - kvp.Value.StartTime > TimeSpan.FromMinutes(30)) // Consider stuck after 30 minutes
                .ToList();

            _logger.LogSystemHealthUpdate("KpiScheduler", "Running", null, new Dictionary<string, object>
            {
                ["RunningExecutions"] = runningCount,
                ["StuckExecutions"] = stuckExecutions.Count,
                ["TotalTrackedExecutions"] = _runningExecutions.Count,
                ["LastHealthCheck"] = _lastHealthCheck
            });

            // Handle stuck executions
            foreach (var stuckExecution in stuckExecutions)
            {
                _logger.LogWarning("KPI execution appears stuck: {KpiId} ({Indicator}) running for {Duration}",
                    stuckExecution.Value.KpiId, stuckExecution.Value.Indicator, now - stuckExecution.Value.StartTime);

                // Mark as failed and remove from tracking
                stuckExecution.Value.Status = KpiExecutionStatus.Failed;
                stuckExecution.Value.ErrorMessage = "Execution timeout - marked as stuck";
                stuckExecution.Value.EndTime = now;
            }

            _lastHealthCheck = now;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during KPI Scheduler health check");
        }
    }

    public override void Dispose()
    {
        _healthCheckTimer?.Dispose();
        base.Dispose();
    }

    /// <summary>
    /// Get current execution status for monitoring
    /// </summary>
    public IEnumerable<KpiExecutionInfo> GetCurrentExecutions()
    {
        return _runningExecutions.Values.ToList();
    }
}

/// <summary>
/// Information about a KPI execution
/// </summary>
public class KpiExecutionInfo
{
    public int KpiId { get; set; }
    public string Indicator { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public TimeSpan? Duration { get; set; }
    public KpiExecutionStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public object? Result { get; set; }
}

/// <summary>
/// KPI execution status
/// </summary>
public enum KpiExecutionStatus
{
    Running,
    Completed,
    Failed
}
