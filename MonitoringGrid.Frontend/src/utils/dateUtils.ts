import { format } from 'date-fns';

/**
 * Safely formats a date string or Date object
 * @param dateValue - The date value to format (string, Date, or null/undefined)
 * @param formatString - The format string for date-fns
 * @param fallback - The fallback value if date is invalid
 * @returns Formatted date string or fallback value
 */
export const safeFormatDate = (
  dateValue: string | Date | null | undefined,
  formatString: string = 'MMM dd, yyyy HH:mm',
  fallback: string = 'Unknown'
): string => {
  if (!dateValue) {
    return fallback;
  }

  try {
    const date = typeof dateValue === 'string' ? new Date(dateValue) : dateValue;
    
    // Check if the date is valid
    if (isNaN(date.getTime())) {
      console.warn('Invalid date value:', dateValue);
      return fallback;
    }

    return format(date, formatString);
  } catch (error) {
    console.error('Error formatting date:', error, 'Date value:', dateValue);
    return fallback;
  }
};

/**
 * Safely calculates duration between two dates
 * @param startDate - The start date
 * @param endDate - The end date (defaults to current time)
 * @param unit - The unit to return ('seconds', 'minutes', 'hours', 'days')
 * @returns Duration in the specified unit or null if invalid
 */
export const safeDateDuration = (
  startDate: string | Date | null | undefined,
  endDate: string | Date | null | undefined = new Date(),
  unit: 'seconds' | 'minutes' | 'hours' | 'days' = 'seconds'
): number | null => {
  if (!startDate) {
    return null;
  }

  try {
    const start = typeof startDate === 'string' ? new Date(startDate) : startDate;
    const end = typeof endDate === 'string' ? new Date(endDate) : endDate;

    if (isNaN(start.getTime()) || isNaN(end.getTime())) {
      return null;
    }

    const diffMs = end.getTime() - start.getTime();

    switch (unit) {
      case 'seconds':
        return Math.floor(diffMs / 1000);
      case 'minutes':
        return Math.floor(diffMs / (1000 * 60));
      case 'hours':
        return Math.floor(diffMs / (1000 * 60 * 60));
      case 'days':
        return Math.floor(diffMs / (1000 * 60 * 60 * 24));
      default:
        return Math.floor(diffMs / 1000);
    }
  } catch (error) {
    console.error('Error calculating date duration:', error);
    return null;
  }
};

/**
 * Formats a duration in seconds to a human-readable string
 * @param seconds - Duration in seconds
 * @returns Human-readable duration string
 */
export const formatDuration = (seconds: number | null): string => {
  if (seconds === null || seconds < 0) {
    return 'Unknown';
  }

  if (seconds < 60) {
    return `${seconds} seconds`;
  }

  const minutes = Math.floor(seconds / 60);
  const remainingSeconds = seconds % 60;

  if (minutes < 60) {
    return remainingSeconds > 0 ? `${minutes}m ${remainingSeconds}s` : `${minutes} minutes`;
  }

  const hours = Math.floor(minutes / 60);
  const remainingMinutes = minutes % 60;

  if (hours < 24) {
    return remainingMinutes > 0 ? `${hours}h ${remainingMinutes}m` : `${hours} hours`;
  }

  const days = Math.floor(hours / 24);
  const remainingHours = hours % 24;

  return remainingHours > 0 ? `${days}d ${remainingHours}h` : `${days} days`;
};

/**
 * Checks if a date string or Date object is valid
 * @param dateValue - The date value to check
 * @returns True if the date is valid, false otherwise
 */
export const isValidDate = (dateValue: string | Date | null | undefined): boolean => {
  if (!dateValue) {
    return false;
  }

  try {
    const date = typeof dateValue === 'string' ? new Date(dateValue) : dateValue;
    return !isNaN(date.getTime());
  } catch {
    return false;
  }
};
