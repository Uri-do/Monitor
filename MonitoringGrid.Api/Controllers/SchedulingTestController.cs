using Microsoft.AspNetCore.Mvc;
using MonitoringGrid.Core.Utilities;

namespace MonitoringGrid.Api.Controllers
{
    /// <summary>
    /// Test controller for whole time scheduling functionality
    /// </summary>
    [ApiController]
    [Route("api/v3/[controller]")]
    public class SchedulingTestController : ControllerBase
    {
        /// <summary>
        /// Test whole time scheduling calculations
        /// </summary>
        [HttpGet("test-scheduling")]
        public IActionResult TestScheduling([FromQuery] int frequencyMinutes = 5, [FromQuery] DateTime? lastRun = null)
        {
            var now = DateTime.UtcNow;
            var testLastRun = lastRun ?? now.AddMinutes(-frequencyMinutes - 1); // Simulate overdue KPI
            
            var nextExecution = WholeTimeScheduler.GetNextWholeTimeExecution(frequencyMinutes, testLastRun);
            var isDue = WholeTimeScheduler.IsKpiDueForWholeTimeExecution(testLastRun, frequencyMinutes, now);
            var description = WholeTimeScheduler.GetScheduleDescription(frequencyMinutes);
            
            return Ok(new
            {
                CurrentTime = now,
                LastRun = testLastRun,
                FrequencyMinutes = frequencyMinutes,
                NextExecution = nextExecution,
                MinutesUntilNext = (nextExecution - now).TotalMinutes,
                IsDue = isDue,
                ScheduleDescription = description,
                Examples = GetSchedulingExamplesData()
            });
        }
        
        /// <summary>
        /// Get examples of different scheduling intervals
        /// </summary>
        [HttpGet("scheduling-examples")]
        public IActionResult GetSchedulingExamples()
        {
            var examples = GetSchedulingExamplesData();
            return Ok(examples);
        }

        private static object GetSchedulingExamplesData()
        {
            var now = DateTime.UtcNow;
            var frequencies = new[] { 1, 5, 10, 15, 30, 60, 120, 180, 240, 360, 720, 1440 };
            
            return frequencies.Select(freq => new
            {
                FrequencyMinutes = freq,
                Description = WholeTimeScheduler.GetScheduleDescription(freq),
                NextExecution = WholeTimeScheduler.GetNextWholeTimeExecution(freq),
                MinutesUntilNext = (WholeTimeScheduler.GetNextWholeTimeExecution(freq) - now).TotalMinutes,
                ExampleTimes = GetExampleExecutionTimes(freq, now)
            }).ToArray();
        }
        
        private static string[] GetExampleExecutionTimes(int frequencyMinutes, DateTime baseTime)
        {
            var examples = new List<string>();
            var current = WholeTimeScheduler.GetNextWholeTimeExecution(frequencyMinutes, baseTime);
            
            for (int i = 0; i < 5; i++)
            {
                examples.Add(current.ToString("HH:mm:ss"));
                current = WholeTimeScheduler.GetNextWholeTimeExecution(frequencyMinutes, current);
            }
            
            return examples.ToArray();
        }
    }
}
