import { ScheduleConfiguration, ScheduleType, CronPreset } from '@/types/api';

/**
 * Validates a cron expression format
 */
export const validateCronExpression = (expression: string): { isValid: boolean; error?: string } => {
  if (!expression.trim()) {
    return { isValid: false, error: 'Cron expression cannot be empty' };
  }

  const parts = expression.trim().split(/\s+/);
  if (parts.length !== 5) {
    return { isValid: false, error: 'Cron expression must have exactly 5 parts (minute hour day month weekday)' };
  }

  // Basic validation for each part
  const [minute, hour, day, month, weekday] = parts;

  // Validate minute (0-59)
  if (!isValidCronField(minute, 0, 59)) {
    return { isValid: false, error: 'Invalid minute field (0-59)' };
  }

  // Validate hour (0-23)
  if (!isValidCronField(hour, 0, 23)) {
    return { isValid: false, error: 'Invalid hour field (0-23)' };
  }

  // Validate day (1-31)
  if (!isValidCronField(day, 1, 31)) {
    return { isValid: false, error: 'Invalid day field (1-31)' };
  }

  // Validate month (1-12)
  if (!isValidCronField(month, 1, 12)) {
    return { isValid: false, error: 'Invalid month field (1-12)' };
  }

  // Validate weekday (0-7, where 0 and 7 are Sunday)
  if (!isValidCronField(weekday, 0, 7)) {
    return { isValid: false, error: 'Invalid weekday field (0-7)' };
  }

  return { isValid: true };
};

/**
 * Validates a single cron field
 */
const isValidCronField = (field: string, min: number, max: number): boolean => {
  // Allow wildcards
  if (field === '*') return true;

  // Allow step values (e.g., */5)
  if (field.includes('/')) {
    const [range, step] = field.split('/');
    if (range === '*') return !isNaN(parseInt(step)) && parseInt(step) > 0;
    // Handle range/step combinations
    return true; // Simplified validation
  }

  // Allow ranges (e.g., 1-5)
  if (field.includes('-')) {
    const [start, end] = field.split('-').map(Number);
    return !isNaN(start) && !isNaN(end) && start >= min && end <= max && start <= end;
  }

  // Allow lists (e.g., 1,3,5)
  if (field.includes(',')) {
    const values = field.split(',').map(Number);
    return values.every(val => !isNaN(val) && val >= min && val <= max);
  }

  // Single number
  const num = parseInt(field);
  return !isNaN(num) && num >= min && num <= max;
};

/**
 * Converts a schedule configuration to a human-readable description
 */
export const getScheduleDescription = (config: ScheduleConfiguration): string => {
  if (!config.isEnabled) {
    return 'Scheduling disabled';
  }

  switch (config.scheduleType) {
    case ScheduleType.Interval:
      if (!config.intervalMinutes) return 'Invalid interval configuration';
      if (config.intervalMinutes < 60) {
        return `Every ${config.intervalMinutes} minute${config.intervalMinutes !== 1 ? 's' : ''}`;
      } else if (config.intervalMinutes < 1440) {
        const hours = Math.floor(config.intervalMinutes / 60);
        const minutes = config.intervalMinutes % 60;
        let desc = `Every ${hours} hour${hours !== 1 ? 's' : ''}`;
        if (minutes > 0) {
          desc += ` and ${minutes} minute${minutes !== 1 ? 's' : ''}`;
        }
        return desc;
      } else {
        const days = Math.floor(config.intervalMinutes / 1440);
        const remainingMinutes = config.intervalMinutes % 1440;
        let desc = `Every ${days} day${days !== 1 ? 's' : ''}`;
        if (remainingMinutes > 0) {
          const hours = Math.floor(remainingMinutes / 60);
          const minutes = remainingMinutes % 60;
          if (hours > 0) {
            desc += `, ${hours} hour${hours !== 1 ? 's' : ''}`;
          }
          if (minutes > 0) {
            desc += `, ${minutes} minute${minutes !== 1 ? 's' : ''}`;
          }
        }
        return desc;
      }

    case ScheduleType.Cron:
      if (!config.cronExpression) return 'Invalid cron configuration';
      return getCronDescription(config.cronExpression);

    case ScheduleType.OneTime:
      if (!config.startDate) return 'Invalid one-time configuration';
      return `Once on ${new Date(config.startDate).toLocaleString()}`;

    default:
      return 'Unknown schedule type';
  }
};

/**
 * Converts a cron expression to a human-readable description
 */
const getCronDescription = (expression: string): string => {
  // This is a simplified cron description generator
  // In a real application, you might want to use a library like cronstrue
  
  const commonPatterns: Record<string, string> = {
    '* * * * *': 'Every minute',
    '*/5 * * * *': 'Every 5 minutes',
    '*/15 * * * *': 'Every 15 minutes',
    '*/30 * * * *': 'Every 30 minutes',
    '0 * * * *': 'Every hour',
    '0 */2 * * *': 'Every 2 hours',
    '0 */6 * * *': 'Every 6 hours',
    '0 */12 * * *': 'Every 12 hours',
    '0 0 * * *': 'Daily at midnight',
    '0 9 * * *': 'Daily at 9:00 AM',
    '0 0 * * 1': 'Weekly on Monday',
    '0 0 1 * *': 'Monthly on the 1st',
  };

  return commonPatterns[expression] || `Cron: ${expression}`;
};

/**
 * Calculates the next few execution times for a schedule
 */
export const calculateNextExecutions = (config: ScheduleConfiguration, count: number = 3): Date[] => {
  const executions: Date[] = [];
  const now = new Date();

  if (!config.isEnabled) {
    return executions;
  }

  switch (config.scheduleType) {
    case ScheduleType.Interval:
      if (config.intervalMinutes) {
        for (let i = 1; i <= count; i++) {
          const nextExecution = new Date(now.getTime() + (config.intervalMinutes * i * 60000));
          executions.push(nextExecution);
        }
      }
      break;

    case ScheduleType.OneTime:
      if (config.startDate) {
        const executionDate = new Date(config.startDate);
        if (executionDate > now) {
          executions.push(executionDate);
        }
      }
      break;

    case ScheduleType.Cron:
      // For cron expressions, we would need a proper cron parser library
      // This is a placeholder that shows the concept
      executions.push(new Date(now.getTime() + 60000)); // Next minute as placeholder
      break;
  }

  return executions;
};

/**
 * Checks if a schedule configuration is valid
 */
export const isValidScheduleConfiguration = (config: ScheduleConfiguration): { isValid: boolean; errors: string[] } => {
  const errors: string[] = [];

  if (!config.isEnabled) {
    return { isValid: true, errors: [] };
  }

  switch (config.scheduleType) {
    case ScheduleType.Interval:
      if (!config.intervalMinutes || config.intervalMinutes < 1) {
        errors.push('Interval must be at least 1 minute');
      }
      if (config.intervalMinutes && config.intervalMinutes > 10080) { // 1 week
        errors.push('Interval cannot exceed 1 week (10080 minutes)');
      }
      break;

    case ScheduleType.Cron:
      if (!config.cronExpression) {
        errors.push('Cron expression is required');
      } else {
        const validation = validateCronExpression(config.cronExpression);
        if (!validation.isValid && validation.error) {
          errors.push(validation.error);
        }
      }
      break;

    case ScheduleType.OneTime:
      if (!config.startDate) {
        errors.push('Execution date is required for one-time schedules');
      } else {
        const executionDate = new Date(config.startDate);
        if (executionDate <= new Date()) {
          errors.push('Execution date must be in the future');
        }
      }
      break;

    default:
      errors.push('Invalid schedule type');
  }

  // Validate date range if provided
  if (config.startDate && config.endDate) {
    const startDate = new Date(config.startDate);
    const endDate = new Date(config.endDate);
    if (startDate >= endDate) {
      errors.push('End date must be after start date');
    }
  }

  return { isValid: errors.length === 0, errors };
};

/**
 * Common cron presets for quick selection
 */
export const COMMON_CRON_PRESETS: CronPreset[] = [
  { name: 'Every minute', expression: '* * * * *', description: 'Runs every minute' },
  { name: 'Every 5 minutes', expression: '*/5 * * * *', description: 'Runs every 5 minutes' },
  { name: 'Every 15 minutes', expression: '*/15 * * * *', description: 'Runs every 15 minutes' },
  { name: 'Every 30 minutes', expression: '*/30 * * * *', description: 'Runs every 30 minutes' },
  { name: 'Hourly', expression: '0 * * * *', description: 'Runs at the start of every hour' },
  { name: 'Every 2 hours', expression: '0 */2 * * *', description: 'Runs every 2 hours' },
  { name: 'Every 6 hours', expression: '0 */6 * * *', description: 'Runs every 6 hours' },
  { name: 'Every 12 hours', expression: '0 */12 * * *', description: 'Runs every 12 hours' },
  { name: 'Daily at midnight', expression: '0 0 * * *', description: 'Runs daily at 00:00' },
  { name: 'Daily at 6 AM', expression: '0 6 * * *', description: 'Runs daily at 06:00' },
  { name: 'Daily at 9 AM', expression: '0 9 * * *', description: 'Runs daily at 09:00' },
  { name: 'Daily at 6 PM', expression: '0 18 * * *', description: 'Runs daily at 18:00' },
  { name: 'Weekly (Monday)', expression: '0 0 * * 1', description: 'Runs every Monday at midnight' },
  { name: 'Weekly (Friday)', expression: '0 0 * * 5', description: 'Runs every Friday at midnight' },
  { name: 'Monthly', expression: '0 0 1 * *', description: 'Runs on the 1st of every month' },
  { name: 'Quarterly', expression: '0 0 1 */3 *', description: 'Runs on the 1st of every quarter' },
];
