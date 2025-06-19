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
  executionHistoryId?: number; // Link to detailed execution history
  details?: {
    collectorID?: number;
    collectorItemName?: string;
    lastMinutes?: number;
    availableItems?: string[];
    value?: number;
    executionContext?: string;
    alertsGenerated?: number;
    thresholdBreached?: boolean;
    alertThreshold?: number;
    alertOperator?: string;
  };
}

export interface DetailedExecutionInfo {
  sqlQuery?: string;
  sqlParameters?: string;
  rawResponse?: string;
  databaseName?: string;
  serverName?: string;
  sessionId?: string;
  connectionString?: string;
  resultCount?: number;
  thresholdAnalysis?: {
    threshold?: number;
    operator?: string;
    currentValue?: number;
    historicalValue?: number;
    breached: boolean;
    severity?: string;
    deviationPercent?: number;
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
