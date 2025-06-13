using MonitoringGrid.Core.DTOs;
using MonitoringGrid.Core.Common;

namespace MonitoringGrid.Core.Interfaces
{
    /// <summary>
    /// Service interface for managing scheduler configurations
    /// </summary>
    public interface ISchedulerService
    {
        // Scheduler CRUD operations
        Task<Result<List<SchedulerDto>>> GetSchedulersAsync(bool includeDisabled = false);
        Task<Result<SchedulerDto>> GetSchedulerByIdAsync(int schedulerId);
        Task<Result<SchedulerDto>> CreateSchedulerAsync(CreateSchedulerRequest request, string createdBy = "system");
        Task<Result<SchedulerDto>> UpdateSchedulerAsync(int schedulerId, UpdateSchedulerRequest request, string modifiedBy = "system");
        Task<Result<bool>> DeleteSchedulerAsync(int schedulerId);
        Task<Result<bool>> ToggleSchedulerAsync(int schedulerId, bool isEnabled, string modifiedBy = "system");

        // Scheduler validation and utilities
        Task<Result<SchedulerValidationResult>> ValidateSchedulerConfigurationAsync(CreateSchedulerRequest request);
        Task<Result<List<SchedulerPresetDto>>> GetSchedulerPresetsAsync();
        Task<Result<SchedulerStatsDto>> GetSchedulerStatisticsAsync();

        // Indicator-Scheduler relationships
        Task<Result<List<IndicatorWithSchedulerDto>>> GetIndicatorsWithSchedulersAsync();
        Task<Result<AssignSchedulerResponse>> AssignSchedulerToIndicatorAsync(AssignSchedulerRequest request, string modifiedBy = "system");
        Task<Result<List<IndicatorWithSchedulerDto>>> GetIndicatorsBySchedulerAsync(int schedulerId);

        // Execution planning
        Task<Result<List<IndicatorWithSchedulerDto>>> GetDueIndicatorsAsync();
        Task<Result<DateTime?>> GetNextExecutionTimeAsync(int schedulerId, DateTime? lastExecution = null);
        Task<Result<List<IndicatorWithSchedulerDto>>> GetUpcomingExecutionsAsync(int hours = 24);

        // Bulk operations
        Task<Result<bool>> BulkAssignSchedulerAsync(List<long> indicatorIds, int? schedulerId, string modifiedBy = "system");
        Task<Result<bool>> BulkUpdateSchedulerStatusAsync(List<int> schedulerIds, bool isEnabled, string modifiedBy = "system");
    }
}
