import { BaseApiService } from './BaseApiService';

export interface TestResult {
  id: string;
  name: string;
  category: string;
  status: 'passed' | 'failed' | 'skipped' | 'running';
  duration: number;
  message?: string;
  stackTrace?: string;
}

export interface TestSuiteStatus {
  isRunning: boolean;
  totalTests: number;
  lastRun?: string;
  availableCategories: string[];
  testResults: TestResult[];
}

export interface RunTestsRequest {
  categories?: string[];
  parallel?: boolean;
  maxParallelism?: number;
  timeoutMs?: number;
  retryFailedTests?: boolean;
  maxRetries?: number;
  generateReport?: boolean;
  verboseOutput?: boolean;
  stopOnFirstFailure?: boolean;
}

export interface TestExecutionResponse {
  executionId: string;
  status: string;
  categories: string[];
  startTime: string;
}

export interface TestExecutionStatus {
  executionId: string;
  isRunning: boolean;
  progress: number;
  currentTest?: string;
  results?: TestResult[];
  startTime: string;
  endTime?: string;
  duration: string;
}

export interface TestExecutionSummary {
  executionId: string;
  startTime: string;
  endTime: string;
  duration: string;
  categories: string[];
  totalTests: number;
  passedTests: number;
  failedTests: number;
  successRate: number;
}

export interface TestHistoryResponse {
  executions: TestExecutionSummary[];
  page: number;
  pageSize: number;
  totalCount: number;
}

class TestSuiteService extends BaseApiService {
  constructor() {
    super('testsuite');
  }

  /**
   * Get the current status of the test suite
   */
  async getStatus(): Promise<TestSuiteStatus> {
    const response = await this.get<TestSuiteStatus>('/status');
    return response;
  }

  /**
   * Run tests for specified categories
   */
  async runTests(request: RunTestsRequest = {}): Promise<TestExecutionResponse> {
    const response = await this.post<TestExecutionResponse>('/run', request);
    return response;
  }

  /**
   * Stop a running test execution
   */
  async stopTests(executionId: string): Promise<void> {
    await this.post(`/stop/${executionId}`);
  }

  /**
   * Get test execution progress
   */
  async getExecutionStatus(executionId: string): Promise<TestExecutionStatus> {
    const response = await this.get<TestExecutionStatus>(`/execution/${executionId}`);
    return response;
  }

  /**
   * Get test results history
   */
  async getTestHistory(page: number = 1, pageSize: number = 10): Promise<TestHistoryResponse> {
    const response = await this.get<TestHistoryResponse>(`/history?page=${page}&pageSize=${pageSize}`);
    return response;
  }

  /**
   * Poll execution status until completion
   */
  async pollExecutionStatus(
    executionId: string,
    onProgress?: (status: TestExecutionStatus) => void,
    intervalMs: number = 1000
  ): Promise<TestExecutionStatus> {
    return new Promise((resolve, reject) => {
      const poll = async () => {
        try {
          const status = await this.getExecutionStatus(executionId);
          
          if (onProgress) {
            onProgress(status);
          }

          if (!status.isRunning) {
            resolve(status);
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
   * Run tests and poll for completion
   */
  async runTestsAndWait(
    request: RunTestsRequest = {},
    onProgress?: (status: TestExecutionStatus) => void
  ): Promise<TestExecutionStatus> {
    const execution = await this.runTests(request);
    return this.pollExecutionStatus(execution.executionId, onProgress);
  }
}

export const testSuiteService = new TestSuiteService();
