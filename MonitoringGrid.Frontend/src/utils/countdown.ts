/**
 * Utility functions for formatting countdown timers
 */

/**
 * Formats countdown seconds into a human-readable string
 * Shows minutes until the last minute, then switches to seconds
 *
 * @param seconds - Number of seconds remaining
 * @returns Formatted countdown string
 *
 * Examples:
 * - 3661 seconds -> "1h 1m"
 * - 125 seconds -> "2m"
 * - 59 seconds -> "59s"
 * - 30 seconds -> "30s"
 * - 0 seconds -> "Due Now!"
 */
export const formatCountdown = (seconds: number | null): string => {
  if (seconds === null || seconds <= 0) return 'Due Now!';

  const days = Math.floor(seconds / (24 * 60 * 60));
  const hours = Math.floor((seconds % (24 * 60 * 60)) / (60 * 60));
  const mins = Math.floor((seconds % (60 * 60)) / 60);
  const secs = seconds % 60;

  // Show seconds only when less than 1 minute (60 seconds)
  if (seconds < 60) {
    return `${secs}s`;
  }

  // For 1 minute or more, show in minutes (and larger units)
  if (days > 0) {
    return `${days}d ${hours}h ${mins}m`;
  } else if (hours > 0) {
    return `${hours}h ${mins}m`;
  } else {
    // Show minutes only (no seconds) when 1 minute or more
    return `${mins}m`;
  }
};

/**
 * Formats countdown with additional context for accessibility
 *
 * @param seconds - Number of seconds remaining
 * @returns Object with formatted string and accessibility text
 */
export const formatCountdownWithContext = (
  seconds: number | null
): {
  display: string;
  ariaLabel: string;
  isUrgent: boolean;
} => {
  const display = formatCountdown(seconds);

  if (seconds === null || seconds <= 0) {
    return {
      display,
      ariaLabel: 'KPI execution is due now',
      isUrgent: true,
    };
  }

  const isUrgent = seconds < 60;
  const mins = Math.floor(seconds / 60);

  let ariaLabel: string;
  if (isUrgent) {
    ariaLabel = `KPI execution in ${seconds} seconds`;
  } else if (mins === 1) {
    ariaLabel = `KPI execution in 1 minute`;
  } else {
    ariaLabel = `KPI execution in ${mins} minutes`;
  }

  return {
    display,
    ariaLabel,
    isUrgent,
  };
};

/**
 * Gets the appropriate color/severity based on countdown time
 *
 * @param seconds - Number of seconds remaining
 * @returns Color severity level
 */
export const getCountdownSeverity = (seconds: number | null): 'success' | 'warning' | 'error' => {
  if (seconds === null || seconds <= 0) return 'error';
  if (seconds < 60) return 'error'; // Less than 1 minute - urgent
  if (seconds < 300) return 'warning'; // Less than 5 minutes - warning
  return 'success'; // 5+ minutes - normal
};

/**
 * Determines if countdown should have pulsing animation
 *
 * @param seconds - Number of seconds remaining
 * @returns Whether countdown should pulse
 */
export const shouldCountdownPulse = (seconds: number | null): boolean => {
  return seconds !== null && seconds > 0 && seconds < 60;
};
