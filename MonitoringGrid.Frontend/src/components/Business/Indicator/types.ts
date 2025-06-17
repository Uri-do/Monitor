/**
 * Shared types for Indicator components
 */

export interface ExecutionLogEntry {
  id: string;
  timestamp: Date;
  indicatorID: number;
  indicator: string;
  type: 'started' | 'completed' | 'error' | 'info';
  message: string;
  duration?: number;
  success?: boolean;
  errorMessage?: string;
  details?: {
    collectorID?: number;
    collectorItemName?: string;
    lastMinutes?: number;
    availableItems?: string[];
    value?: number;
    executionContext?: string;
    alertsGenerated?: number;
  };
}

export interface RunningIndicator {
  indicatorID: number;
  indicator: string;
  owner: string;
  startTime: string;
  progress?: number;
  estimatedCompletion?: string;
  currentStep?: string;
  elapsedTime?: number;
  status?: 'running' | 'completed' | 'failed';
  completedAt?: string;
  duration?: number;
  value?: number;
  errorMessage?: string;
}
