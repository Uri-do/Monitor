# Whole Time Scheduling

## Overview

The MonitoringGrid system now uses **Whole Time Scheduling** for KPI execution. This ensures that KPIs run at clean, predictable time intervals rather than arbitrary times based on when they were last executed.

## How It Works

Instead of running KPIs exactly X minutes after their last execution, the system calculates the next "whole time" boundary based on the frequency:

### Examples

| Frequency | Execution Times | Description |
|-----------|----------------|-------------|
| 1 minute | xx:00, xx:01, xx:02, xx:03... | Every minute at 0 seconds |
| 5 minutes | xx:00, xx:05, xx:10, xx:15... | Every 5 minutes |
| 10 minutes | xx:00, xx:10, xx:20, xx:30... | Every 10 minutes |
| 15 minutes | xx:00, xx:15, xx:30, xx:45 | Every 15 minutes |
| 30 minutes | xx:00, xx:30 | Every 30 minutes |
| 60 minutes | xx:00 | Every hour on the hour |
| 120 minutes | 00:00, 02:00, 04:00, 06:00... | Every 2 hours |
| 360 minutes | 00:00, 06:00, 12:00, 18:00 | Every 6 hours |
| 1440 minutes | 00:00 | Daily at midnight |

## Benefits

1. **Predictable Execution**: KPIs always run at clean time boundaries
2. **Better Coordination**: Multiple KPIs with the same frequency run together
3. **Easier Monitoring**: Clear patterns make it easier to track and debug
4. **Resource Optimization**: Batched executions can be more efficient
5. **User-Friendly**: Intuitive scheduling that matches user expectations

## Implementation

### Backend Components

- **`WholeTimeScheduler`**: Utility class with scheduling logic
- **`KPI.IsDue()`**: Updated to use whole time scheduling
- **`KPI.GetNextRunTime()`**: Returns next whole time boundary
- **Worker Services**: All background services use the new scheduling

### API Endpoints

- **`/api/v3/SchedulingTest/scheduling-examples`**: View scheduling examples
- **`/api/v3/SchedulingTest/test-scheduling`**: Test specific scenarios
- **`/api/v3/kpi/dashboard`**: Shows next execution with whole time scheduling

### Frontend Updates

- **Dashboard**: Shows accurate countdown timers
- **KPI Forms**: Helper text explains whole time scheduling
- **Next Execution Card**: Displays "whole time scheduling" indicator

## Configuration

No configuration changes are required. The system automatically calculates whole time boundaries based on the existing frequency settings.

## Migration

Existing KPIs will automatically use whole time scheduling on their next execution. No data migration is required.

## Testing

Use the test endpoints to verify scheduling behavior:

```bash
# View all scheduling examples
GET /api/v3/SchedulingTest/scheduling-examples

# Test specific frequency
GET /api/v3/SchedulingTest/test-scheduling?frequencyMinutes=5

# Test with specific last run time
GET /api/v3/SchedulingTest/test-scheduling?frequencyMinutes=15&lastRun=2024-01-01T10:30:00Z
```

## Technical Details

The `WholeTimeScheduler` class handles:

- **Minute boundaries**: For frequencies < 60 minutes
- **Hour boundaries**: For frequencies that are multiples of 60 minutes
- **Day boundaries**: For daily schedules (1440 minutes)
- **Generic intervals**: For any other frequency using minute-based calculations

All calculations are performed in UTC to ensure consistency across time zones.
