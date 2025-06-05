using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Models;

namespace MonitoringGrid.Core.Interfaces;

/// <summary>
/// Service responsible for executing KPI stored procedures and calculating deviations
/// </summary>
public interface IKpiExecutionService
{
    /// <summary>
    /// Executes a KPI stored procedure and returns the result
    /// </summary>
    Task<KpiExecutionResult> ExecuteKpiAsync(KPI kpi, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates the percentage deviation between current and historical values
    /// </summary>
    decimal CalculateDeviation(decimal current, decimal historical);

    /// <summary>
    /// Determines if an alert should be triggered based on KPI configuration and results
    /// </summary>
    bool ShouldTriggerAlert(KPI kpi, KpiExecutionResult result);

    /// <summary>
    /// Validates that a KPI's stored procedure exists and is executable
    /// </summary>
    Task<bool> ValidateKpiStoredProcedureAsync(string spName, CancellationToken cancellationToken = default);
}
