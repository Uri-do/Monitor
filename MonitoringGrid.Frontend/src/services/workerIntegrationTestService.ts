import BaseApiService from './BaseApiService';

// Types for Worker Integration Testing
export interface StartWorkerTestRequest {
  testType: string;
  indicatorIds?: number[];
  durationSeconds?: number;
  concurrentWorkers?: number;
  parameters?: Record<string, any>;
}

export interface WorkerTestExecutionResponse {
  testId: string;
  status: string;
  message: string;
  startTime: string;
  endTime?: string;
  durationSeconds?: number;
}

export interface WorkerIntegrationTestStatus {
  isRunning: boolean;
  activeTests: number;
  totalIndicators: number;
  availableIndicators: IndicatorSummary[];
  recentExecutions: WorkerTestExecution[];
  availableTestTypes: string[];
}

export interface IndicatorSummary {
  id: number;
  name: string;
  description?: string;
  priority: string;
  lastRun?: string;
}

export interface WorkerTestExecution {
  id: string;
  testType: string;
  indicatorIds: number[];
  startTime: string;
  endTime?: string;
  isRunning: boolean;
  status: string;
  progress: number;
  lastUpdate?: string;
  success?: boolean;
  errorMessage?: string;
  results?: WorkerTestResults;
  durationSeconds?: number;
}

export interface WorkerTestResults {
  indicatorsProcessed: number;
  successfulExecutions: number;
  failedExecutions: number;
  averageExecutionTimeMs: number;
  totalExecutionTimeMs: number;
  memoryUsageBytes: number;
  cpuUsagePercent: number;
  alertsTriggered: number;
  indicatorResults: IndicatorExecutionResult[];
  performanceMetrics: Record<string, any>;
}

export interface IndicatorExecutionResult {
  indicatorId: number;
  indicatorName: string;
  success: boolean;
  executionTimeMs: number;
  errorMessage?: string;
  recordsProcessed: number;
  alertsTriggered: boolean;
  startTime: string;
  endTime: string;
}

/**
 * Service for Worker Integration Testing API operations
 */
class WorkerIntegrationTestService extends BaseApiService {
  constructor() {
    super('/api/worker-integration-test');
  }

  /**
   * Get the current status of worker integration tests
   */
  async getStatus(): Promise<WorkerIntegrationTestStatus> {
    const response = await this.get<WorkerIntegrationTestStatus>('/status');
    return response;
  }

  /**
   * Start a worker integration test
   */
  async startTest(request: StartWorkerTestRequest): Promise<WorkerTestExecutionResponse> {
    const response = await this.post<WorkerTestExecutionResponse>('/start', request);
    return response;
  }

  /**
   * Stop a running worker integration test
   */
  async stopTest(testId: string): Promise<WorkerTestExecutionResponse> {
    const response = await this.post<WorkerTestExecutionResponse>(`/stop/${testId}`);
    return response;
  }

  /**
   * Get the status of a specific test execution
   */
  async getTestExecution(testId: string): Promise<WorkerTestExecution> {
    const response = await this.get<WorkerTestExecution>(`/execution/${testId}`);
    return response;
  }

  /**
   * Poll test execution status until completion
   */
  async pollTestExecution(
    testId: string,
    onUpdate?: (execution: WorkerTestExecution) => void,
    intervalMs: number = 2000
  ): Promise<WorkerTestExecution> {
    return new Promise((resolve, reject) => {
      const poll = async () => {
        try {
          const execution = await this.getTestExecution(testId);
          
          if (onUpdate) {
            onUpdate(execution);
          }

          if (!execution.isRunning) {
            resolve(execution);
            return;
          }

          setTimeout(poll, intervalMs);
        } catch (error) {
          reject(error);
        }
      };

      poll();
    });
  }

  /**
   * Get available test types with descriptions
   */
  getAvailableTestTypes(): Array<{ value: string; label: string; description: string }> {
    return [
      {
        value: 'indicator-execution',
        label: 'Indicator Execution Test',
        description: 'Tests actual indicator processing with real data collection and alert triggering'
      },
      {
        value: 'worker-lifecycle',
        label: 'Worker Lifecycle Test',
        description: 'Tests starting, monitoring, and stopping worker processes'
      },
      {
        value: 'real-time-monitoring',
        label: 'Real-time Monitoring Test',
        description: 'Tests SignalR connectivity and real-time updates during execution'
      },
      {
        value: 'stress-test',
        label: 'Stress Test',
        description: 'Tests system performance under concurrent worker load'
      }
    ];
  }

  /**
   * Format test duration for display
   */
  formatDuration(durationSeconds?: number): string {
    if (!durationSeconds) return 'N/A';
    
    if (durationSeconds < 60) {
      return `${durationSeconds.toFixed(1)}s`;
    } else if (durationSeconds < 3600) {
      const minutes = Math.floor(durationSeconds / 60);
      const seconds = Math.floor(durationSeconds % 60);
      return `${minutes}m ${seconds}s`;
    } else {
      const hours = Math.floor(durationSeconds / 3600);
      const minutes = Math.floor((durationSeconds % 3600) / 60);
      return `${hours}h ${minutes}m`;
    }
  }

  /**
   * Format memory usage for display
   */
  formatMemoryUsage(bytes: number): string {
    const units = ['B', 'KB', 'MB', 'GB'];
    let size = bytes;
    let unitIndex = 0;

    while (size >= 1024 && unitIndex < units.length - 1) {
      size /= 1024;
      unitIndex++;
    }

    return `${size.toFixed(1)} ${units[unitIndex]}`;
  }

  /**
   * Get status color based on test result
   */
  getStatusColor(execution: WorkerTestExecution): string {
    if (execution.isRunning) return 'blue';
    if (execution.success === true) return 'green';
    if (execution.success === false) return 'red';
    return 'gray';
  }

  /**
   * Get status icon based on test result
   */
  getStatusIcon(execution: WorkerTestExecution): string {
    if (execution.isRunning) return '⏳';
    if (execution.success === true) return '✅';
    if (execution.success === false) return '❌';
    return '⏸️';
  }

  /**
   * Calculate success rate from test results
   */
  calculateSuccessRate(results?: WorkerTestResults): number {
    if (!results || results.indicatorsProcessed === 0) return 0;
    return (results.successfulExecutions / results.indicatorsProcessed) * 100;
  }

  /**
   * Get performance rating based on execution time
   */
  getPerformanceRating(averageTimeMs: number): { rating: string; color: string } {
    if (averageTimeMs < 1000) return { rating: 'Excellent', color: 'green' };
    if (averageTimeMs < 5000) return { rating: 'Good', color: 'blue' };
    if (averageTimeMs < 10000) return { rating: 'Fair', color: 'yellow' };
    return { rating: 'Poor', color: 'red' };
  }

  /**
   * Export test results to JSON
   */
  exportTestResults(execution: WorkerTestExecution): void {
    const dataStr = JSON.stringify(execution, null, 2);
    const dataBlob = new Blob([dataStr], { type: 'application/json' });
    const url = URL.createObjectURL(dataBlob);
    
    const link = document.createElement('a');
    link.href = url;
    link.download = `worker-test-${execution.id}-${new Date().toISOString().split('T')[0]}.json`;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    
    URL.revokeObjectURL(url);
  }
}

export default new WorkerIntegrationTestService();
