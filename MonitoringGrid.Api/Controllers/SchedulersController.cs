using Microsoft.AspNetCore.Mvc;
using MonitoringGrid.Core.DTOs;
using MonitoringGrid.Core.Interfaces;

namespace MonitoringGrid.Api.Controllers
{
    /// <summary>
    /// API controller for managing scheduler configurations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class SchedulersController : ControllerBase
    {
        private readonly ISchedulerService _schedulerService;
        private readonly ILogger<SchedulersController> _logger;

        public SchedulersController(ISchedulerService schedulerService, ILogger<SchedulersController> logger)
        {
            _schedulerService = schedulerService;
            _logger = logger;
        }

        /// <summary>
        /// Get all schedulers
        /// </summary>
        /// <param name="includeDisabled">Include disabled schedulers in the result</param>
        /// <returns>List of schedulers</returns>
        [HttpGet]
        public async Task<ActionResult<List<SchedulerDto>>> GetSchedulers([FromQuery] bool includeDisabled = false)
        {
            var result = await _schedulerService.GetSchedulersAsync(includeDisabled);
            
            if (!result.IsSuccess)
            {
                return BadRequest(result.Error);
            }

            return Ok(result.Value);
        }

        /// <summary>
        /// Get scheduler by ID
        /// </summary>
        /// <param name="id">Scheduler ID</param>
        /// <returns>Scheduler details</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<SchedulerDto>> GetScheduler(int id)
        {
            var result = await _schedulerService.GetSchedulerByIdAsync(id);
            
            if (!result.IsSuccess)
            {
                return NotFound(result.Error);
            }

            return Ok(result.Value);
        }

        /// <summary>
        /// Create a new scheduler
        /// </summary>
        /// <param name="request">Scheduler creation request</param>
        /// <returns>Created scheduler</returns>
        [HttpPost]
        public async Task<ActionResult<SchedulerDto>> CreateScheduler([FromBody] CreateSchedulerRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _schedulerService.CreateSchedulerAsync(request, GetCurrentUser());
            
            if (!result.IsSuccess)
            {
                return BadRequest(result.Error);
            }

            return CreatedAtAction(nameof(GetScheduler), new { id = result.Value.SchedulerID }, result.Value);
        }

        /// <summary>
        /// Update an existing scheduler
        /// </summary>
        /// <param name="id">Scheduler ID</param>
        /// <param name="request">Scheduler update request</param>
        /// <returns>Updated scheduler</returns>
        [HttpPut("{id}")]
        public async Task<ActionResult<SchedulerDto>> UpdateScheduler(int id, [FromBody] UpdateSchedulerRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _schedulerService.UpdateSchedulerAsync(id, request, GetCurrentUser());
            
            if (!result.IsSuccess)
            {
                return BadRequest(result.Error);
            }

            return Ok(result.Value);
        }

        /// <summary>
        /// Delete a scheduler
        /// </summary>
        /// <param name="id">Scheduler ID</param>
        /// <returns>Success status</returns>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteScheduler(int id)
        {
            var result = await _schedulerService.DeleteSchedulerAsync(id);
            
            if (!result.IsSuccess)
            {
                return BadRequest(result.Error);
            }

            return NoContent();
        }

        /// <summary>
        /// Toggle scheduler enabled/disabled status
        /// </summary>
        /// <param name="id">Scheduler ID</param>
        /// <param name="isEnabled">New enabled status</param>
        /// <returns>Success status</returns>
        [HttpPatch("{id}/toggle")]
        public async Task<ActionResult> ToggleScheduler(int id, [FromBody] bool isEnabled)
        {
            var result = await _schedulerService.ToggleSchedulerAsync(id, isEnabled, GetCurrentUser());
            
            if (!result.IsSuccess)
            {
                return BadRequest(result.Error);
            }

            return Ok();
        }

        /// <summary>
        /// Validate scheduler configuration
        /// </summary>
        /// <param name="request">Scheduler configuration to validate</param>
        /// <returns>Validation result</returns>
        [HttpPost("validate")]
        public async Task<ActionResult<SchedulerValidationResult>> ValidateScheduler([FromBody] CreateSchedulerRequest request)
        {
            var result = await _schedulerService.ValidateSchedulerConfigurationAsync(request);
            
            if (!result.IsSuccess)
            {
                return BadRequest(result.Error);
            }

            return Ok(result.Value);
        }

        /// <summary>
        /// Get scheduler preset options
        /// </summary>
        /// <returns>List of scheduler presets</returns>
        [HttpGet("presets")]
        public async Task<ActionResult<List<SchedulerPresetDto>>> GetSchedulerPresets()
        {
            var result = await _schedulerService.GetSchedulerPresetsAsync();
            
            if (!result.IsSuccess)
            {
                return BadRequest(result.Error);
            }

            return Ok(result.Value);
        }

        /// <summary>
        /// Get scheduler statistics
        /// </summary>
        /// <returns>Scheduler statistics</returns>
        [HttpGet("statistics")]
        public async Task<ActionResult<SchedulerStatsDto>> GetSchedulerStatistics()
        {
            var result = await _schedulerService.GetSchedulerStatisticsAsync();
            
            if (!result.IsSuccess)
            {
                return BadRequest(result.Error);
            }

            return Ok(result.Value);
        }

        /// <summary>
        /// Get indicators with their scheduler information
        /// </summary>
        /// <returns>List of indicators with scheduler details</returns>
        [HttpGet("indicators")]
        public async Task<ActionResult<List<IndicatorWithSchedulerDto>>> GetIndicatorsWithSchedulers()
        {
            var result = await _schedulerService.GetIndicatorsWithSchedulersAsync();
            
            if (!result.IsSuccess)
            {
                return BadRequest(result.Error);
            }

            return Ok(result.Value);
        }

        /// <summary>
        /// Assign scheduler to indicator
        /// </summary>
        /// <param name="request">Assignment request</param>
        /// <returns>Assignment result</returns>
        [HttpPost("assign")]
        public async Task<ActionResult<AssignSchedulerResponse>> AssignScheduler([FromBody] AssignSchedulerRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _schedulerService.AssignSchedulerToIndicatorAsync(request, GetCurrentUser());
            
            if (!result.IsSuccess)
            {
                return BadRequest(result.Error);
            }

            return Ok(result.Value);
        }

        /// <summary>
        /// Get indicators assigned to a specific scheduler
        /// </summary>
        /// <param name="id">Scheduler ID</param>
        /// <returns>List of indicators using this scheduler</returns>
        [HttpGet("{id}/indicators")]
        public async Task<ActionResult<List<IndicatorWithSchedulerDto>>> GetIndicatorsByScheduler(int id)
        {
            var result = await _schedulerService.GetIndicatorsBySchedulerAsync(id);
            
            if (!result.IsSuccess)
            {
                return BadRequest(result.Error);
            }

            return Ok(result.Value);
        }

        /// <summary>
        /// Get indicators that are due for execution
        /// </summary>
        /// <returns>List of due indicators</returns>
        [HttpGet("due-indicators")]
        public async Task<ActionResult<List<IndicatorWithSchedulerDto>>> GetDueIndicators()
        {
            var result = await _schedulerService.GetDueIndicatorsAsync();
            
            if (!result.IsSuccess)
            {
                return BadRequest(result.Error);
            }

            return Ok(result.Value);
        }

        /// <summary>
        /// Get upcoming executions for the next specified hours
        /// </summary>
        /// <param name="hours">Number of hours to look ahead (default: 24)</param>
        /// <returns>List of upcoming executions</returns>
        [HttpGet("upcoming")]
        public async Task<ActionResult<List<IndicatorWithSchedulerDto>>> GetUpcomingExecutions([FromQuery] int hours = 24)
        {
            var result = await _schedulerService.GetUpcomingExecutionsAsync(hours);
            
            if (!result.IsSuccess)
            {
                return BadRequest(result.Error);
            }

            return Ok(result.Value);
        }

        /// <summary>
        /// Bulk assign scheduler to multiple indicators
        /// </summary>
        /// <param name="indicatorIds">List of indicator IDs</param>
        /// <param name="schedulerId">Scheduler ID to assign (null to remove assignment)</param>
        /// <returns>Success status</returns>
        [HttpPost("bulk-assign")]
        public async Task<ActionResult> BulkAssignScheduler([FromBody] List<long> indicatorIds, [FromQuery] int? schedulerId = null)
        {
            var result = await _schedulerService.BulkAssignSchedulerAsync(indicatorIds, schedulerId, GetCurrentUser());
            
            if (!result.IsSuccess)
            {
                return BadRequest(result.Error);
            }

            return Ok();
        }

        /// <summary>
        /// Bulk update scheduler status
        /// </summary>
        /// <param name="schedulerIds">List of scheduler IDs</param>
        /// <param name="isEnabled">New enabled status</param>
        /// <returns>Success status</returns>
        [HttpPost("bulk-toggle")]
        public async Task<ActionResult> BulkUpdateSchedulerStatus([FromBody] List<int> schedulerIds, [FromQuery] bool isEnabled = true)
        {
            var result = await _schedulerService.BulkUpdateSchedulerStatusAsync(schedulerIds, isEnabled, GetCurrentUser());
            
            if (!result.IsSuccess)
            {
                return BadRequest(result.Error);
            }

            return Ok();
        }

        /// <summary>
        /// Get next execution time for a scheduler
        /// </summary>
        /// <param name="id">Scheduler ID</param>
        /// <param name="lastExecution">Last execution time (optional)</param>
        /// <returns>Next execution time</returns>
        [HttpGet("{id}/next-execution")]
        public async Task<ActionResult<DateTime?>> GetNextExecutionTime(int id, [FromQuery] DateTime? lastExecution = null)
        {
            var result = await _schedulerService.GetNextExecutionTimeAsync(id, lastExecution);
            
            if (!result.IsSuccess)
            {
                return BadRequest(result.Error);
            }

            return Ok(result.Value);
        }

        private string GetCurrentUser()
        {
            // TODO: Implement proper user context when authentication is added
            return User?.Identity?.Name ?? "system";
        }
    }
}
