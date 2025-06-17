import { apiClient } from './apiClient';
import {
  TestableEndpoint,
  ExecuteTestRequest,
  TestExecutionResult,
  ExecuteBatchTestRequest,
  BatchTestExecutionResult,
  ExecuteTestSuiteRequest,
  TestSuiteExecutionResult,
  GetTestHistoryRequest,
  TestExecutionHistory,
  GetTestStatisticsRequest,
  TestStatistics,
  GenerateTestDataRequest,
  GeneratedTestData,
  ValidateEndpointRequest,
  EndpointValidationResult
} from '../types/testing';

export const apiTestingService = {
  /**
   * Get all testable endpoints
   */
  async getTestableEndpoints(): Promise<TestableEndpoint[]> {
    const response = await apiClient.get('/api/testing/endpoints');
    return response.data.data;
  },

  /**
   * Execute a single test
   */
  async executeTest(request: ExecuteTestRequest): Promise<TestExecutionResult> {
    const response = await apiClient.post('/api/testing/execute', request);
    return response.data.data;
  },

  /**
   * Execute multiple tests in batch
   */
  async executeBatchTests(request: ExecuteBatchTestRequest): Promise<BatchTestExecutionResult> {
    const response = await apiClient.post('/api/testing/execute-batch', request);
    return response.data.data;
  },

  /**
   * Execute comprehensive test suite
   */
  async executeTestSuite(request: ExecuteTestSuiteRequest): Promise<TestSuiteExecutionResult> {
    const response = await apiClient.post('/api/testing/execute-suite', request);
    return response.data.data;
  },

  /**
   * Get test execution history
   */
  async getTestHistory(request: GetTestHistoryRequest): Promise<TestExecutionHistory> {
    const response = await apiClient.get('/api/testing/history', { params: request });
    return response.data.data;
  },

  /**
   * Get test statistics
   */
  async getTestStatistics(request: GetTestStatisticsRequest): Promise<TestStatistics> {
    const response = await apiClient.get('/api/testing/statistics', { params: request });
    return response.data.data;
  },

  /**
   * Generate test data for an endpoint
   */
  async generateTestData(request: GenerateTestDataRequest): Promise<GeneratedTestData> {
    const response = await apiClient.post('/api/testing/generate-test-data', request);
    return response.data.data;
  },

  /**
   * Validate an endpoint configuration
   */
  async validateEndpoint(request: ValidateEndpointRequest): Promise<EndpointValidationResult> {
    const response = await apiClient.post('/api/testing/validate-endpoint', request);
    return response.data.data;
  }
};
