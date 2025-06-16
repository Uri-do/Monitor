import { useQuery } from '@tanstack/react-query';

// Simple placeholder hook for schedulers
export const useSchedulers = () => {
  return useQuery({
    queryKey: ['schedulers'],
    queryFn: async () => {
      // Placeholder data matching SchedulerDto structure
      return [
        {
          schedulerID: 1,
          schedulerName: 'Daily Scheduler',
          schedulerDescription: 'Runs daily at midnight',
          scheduleType: 'cron',
          cronExpression: '0 0 * * *',
          intervalMinutes: null,
          timezone: 'UTC',
          isEnabled: true,
          displayText: 'Daily at 00:00 UTC',
          indicatorCount: 5,
          nextExecutionTime: new Date(Date.now() + 24 * 60 * 60 * 1000).toISOString(),
          createdDate: new Date().toISOString(),
          updatedDate: new Date().toISOString()
        },
        {
          schedulerID: 2,
          schedulerName: 'Hourly Scheduler',
          schedulerDescription: 'Runs every hour',
          scheduleType: 'interval',
          cronExpression: null,
          intervalMinutes: 60,
          timezone: 'UTC',
          isEnabled: true,
          displayText: 'Every 60 minutes',
          indicatorCount: 3,
          nextExecutionTime: new Date(Date.now() + 60 * 60 * 1000).toISOString(),
          createdDate: new Date().toISOString(),
          updatedDate: new Date().toISOString()
        },
        {
          schedulerID: 3,
          schedulerName: 'Weekly Scheduler',
          schedulerDescription: 'Runs weekly on Sunday',
          scheduleType: 'cron',
          cronExpression: '0 0 * * 0',
          intervalMinutes: null,
          timezone: 'UTC',
          isEnabled: false,
          displayText: 'Weekly on Sunday at 00:00 UTC',
          indicatorCount: 2,
          nextExecutionTime: null,
          createdDate: new Date().toISOString(),
          updatedDate: new Date().toISOString()
        }
      ];
    },
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
};
