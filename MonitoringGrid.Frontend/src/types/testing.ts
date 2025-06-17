export interface TestableEndpoint {
  controller: string;
  action: string;
  method: string;
  path: string;
  description: string;
  parameters: EndpointParameter[];
  requiredRoles: string[];
  requiresAuthentication: boolean;
  responseExamples: ResponseExample[];
  complexity: TestComplexity;
  tags: string[];
}

export interface EndpointParameter {
  name: string;
  type: string;
  source: string; // Query, Body, Route, Header
  isRequired: boolean;
  defaultValue?: string;
  description?: string;
  allowedValues?: string[];
  validationRules?: string;
}

export interface ResponseExample {
  statusCode: number;
  description: string;
  exampleData?: any;
  headers?: Record<string, string>;
}

export type TestComplexity = 'Simple' | 'Medium' | 'Complex' | 'Advanced';

export interface ExecuteTestRequest {
  method: string;
  path: string;
  parameters?: Record<string, any>;
  headers?: Record<string, string>;
  body?: any;
  includePerformanceMetrics?: boolean;
  includeResponseValidation?: boolean;
  timeoutSeconds?: number;
  testName?: string;
  testDescription?: string;
}

export interface TestExecutionResult {
  testName: string;
  method: string;
  path: string;
  statusCode: number;
  isSuccess: boolean;
  responseBody?: string;
  responseHeaders?: Record<string, string>;
  testDuration: number; // in milliseconds
  errorMessage?: string;
  performanceMetrics?: TestPerformanceMetrics;
  validationResult?: TestValidationResult;
  executedAt: string;
  executedBy?: string;
}

export interface TestPerformanceMetrics {
  responseTime: number;
  responseSizeBytes: number;
  dnsLookupTime: number;
  connectionTime: number;
  sslHandshakeTime: number;
  timeToFirstByte: number;
  contentDownloadTime: number;
  requestsPerSecond: number;
  memoryUsage: number;
  cpuUsage: number;
}

export interface TestValidationResult {
  isValid: boolean;
  issues: ValidationIssue[];
  schemaValidation?: SchemaValidationResult;
  securityValidation?: SecurityValidationResult;
  performanceValidation?: PerformanceValidationResult;
}

export interface ValidationIssue {
  type: string;
  severity: string;
  message: string;
  field?: string;
  expectedValue?: any;
  actualValue?: any;
}

export interface SchemaValidationResult {
  isValid: boolean;
  errors: string[];
  expectedSchema?: string;
  actualSchema?: string;
}

export interface SecurityValidationResult {
  isSecure: boolean;
  issues: SecurityIssue[];
  hasProperAuthentication: boolean;
  hasProperAuthorization: boolean;
  hasInputValidation: boolean;
  hasOutputSanitization: boolean;
}

export interface SecurityIssue {
  type: string;
  severity: string;
  description: string;
  recommendation?: string;
}

export interface PerformanceValidationResult {
  meetsPerformanceThresholds: boolean;
  issues: PerformanceIssue[];
  responseTimeThreshold: number;
  actualResponseTime: number;
  memoryThresholdBytes: number;
  actualMemoryBytes: number;
}

export interface PerformanceIssue {
  metric: string;
  severity: string;
  description: string;
  thresholdValue?: any;
  actualValue?: any;
}

export interface ExecuteBatchTestRequest {
  tests: ExecuteTestRequest[];
  stopOnFirstFailure?: boolean;
  runInParallel?: boolean;
  maxParallelTests?: number;
  includeDetailedResults?: boolean;
  batchName?: string;
  batchDescription?: string;
}

export interface BatchTestExecutionResult {
  batchName: string;
  totalTests: number;
  successfulTests: number;
  failedTests: number;
  skippedTests: number;
  totalDuration: number;
  results: TestExecutionResult[];
  summary: BatchTestSummary;
  executedAt: string;
  executedBy?: string;
}

export interface BatchTestSummary {
  successRate: number;
  averageResponseTime: number;
  fastestResponse: number;
  slowestResponse: number;
  failedEndpoints: string[];
  slowEndpoints: string[];
  statusCodeDistribution: Record<string, number>;
}

export interface ExecuteTestSuiteRequest {
  includeControllers?: string[];
  excludeControllers?: string[];
  includeEndpoints?: string[];
  excludeEndpoints?: string[];
  includePerformanceTests?: boolean;
  includeSecurityTests?: boolean;
  includeValidationTests?: boolean;
  runInParallel?: boolean;
  maxParallelTests?: number;
  suiteName?: string;
  suiteDescription?: string;
}

export interface TestSuiteExecutionResult {
  suiteName: string;
  totalTests: number;
  successfulTests: number;
  failedTests: number;
  skippedTests: number;
  totalDuration: number;
  controllerResults: ControllerTestResult[];
  summary: TestSuiteSummary;
  failedTests: TestExecutionResult[];
  executedAt: string;
  executedBy?: string;
}

export interface ControllerTestResult {
  controllerName: string;
  totalEndpoints: number;
  successfulEndpoints: number;
  failedEndpoints: number;
  duration: number;
  endpointResults: TestExecutionResult[];
}

export interface TestSuiteSummary {
  overallSuccessRate: number;
  averageResponseTime: number;
  controllerSuccessRates: Record<string, number>;
  criticalIssues: string[];
  performanceIssues: string[];
  securityIssues: string[];
  statusCodeDistribution: Record<string, number>;
}

export interface GetTestHistoryRequest {
  days?: number;
  controller?: string;
  endpoint?: string;
  successOnly?: boolean;
  includeDetails?: boolean;
  pageSize?: number;
  page?: number;
}

export interface TestExecutionHistory {
  totalExecutions: number;
  executions: TestExecutionSummary[];
  statistics: TestHistoryStatistics;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface TestExecutionSummary {
  id: string;
  testName: string;
  controller: string;
  endpoint: string;
  method: string;
  isSuccess: boolean;
  statusCode: number;
  duration: number;
  executedAt: string;
  executedBy?: string;
  errorMessage?: string;
}

export interface TestHistoryStatistics {
  successRate: number;
  averageResponseTime: number;
  totalExecutions: number;
  successfulExecutions: number;
  failedExecutions: number;
  controllerExecutions: Record<string, number>;
  controllerSuccessRates: Record<string, number>;
  successRateTrend: TrendDataPoint[];
  responseTimeTrend: TrendDataPoint[];
}

export interface TrendDataPoint {
  date: string;
  value: number;
  count: number;
}

export interface GetTestStatisticsRequest {
  days?: number;
  controller?: string;
  includeTrends?: boolean;
  includePerformanceMetrics?: boolean;
}

export interface TestStatistics {
  overview: TestOverviewStatistics;
  controllerStats: ControllerStatistics[];
  endpointStats: EndpointStatistics[];
  performance: PerformanceStatistics;
  successRateTrend: TrendDataPoint[];
  responseTimeTrend: TrendDataPoint[];
  generatedAt: string;
}

export interface TestOverviewStatistics {
  totalTests: number;
  successfulTests: number;
  failedTests: number;
  successRate: number;
  averageResponseTime: number;
  totalEndpoints: number;
  testedEndpoints: number;
  endpointCoverage: number;
}

export interface ControllerStatistics {
  controllerName: string;
  totalTests: number;
  successfulTests: number;
  failedTests: number;
  successRate: number;
  averageResponseTime: number;
  totalEndpoints: number;
  testedEndpoints: number;
  mostFailedEndpoints: string[];
  slowestEndpoints: string[];
}

export interface EndpointStatistics {
  controller: string;
  method: string;
  path: string;
  totalTests: number;
  successfulTests: number;
  failedTests: number;
  successRate: number;
  averageResponseTime: number;
  minResponseTime: number;
  maxResponseTime: number;
  lastTested: string;
  commonErrors: string[];
}

export interface PerformanceStatistics {
  averageResponseTime: number;
  medianResponseTime: number;
  p95ResponseTime: number;
  p99ResponseTime: number;
  fastestResponse: number;
  slowestResponse: number;
  requestsPerSecond: number;
  averageResponseSize: number;
  slowestEndpoints: string[];
  fastestEndpoints: string[];
}

export interface GenerateTestDataRequest {
  method: string;
  path: string;
  numberOfSamples?: number;
  includeValidData?: boolean;
  includeInvalidData?: boolean;
  includeEdgeCases?: boolean;
  dataType?: string;
}

export interface GeneratedTestData {
  method: string;
  path: string;
  validSamples: TestDataSample[];
  invalidSamples: TestDataSample[];
  edgeCaseSamples: TestDataSample[];
  parameterSchemas: Record<string, any>;
  generatedAt: string;
}

export interface TestDataSample {
  name: string;
  description: string;
  parameters?: Record<string, any>;
  body?: any;
  headers?: Record<string, string>;
  expectedStatusCode: number;
  expectedResult?: string;
}

export interface ValidateEndpointRequest {
  method: string;
  path: string;
  validateAuthentication?: boolean;
  validateAuthorization?: boolean;
  validateInputValidation?: boolean;
  validateOutputFormat?: boolean;
  validatePerformance?: boolean;
  validateSecurity?: boolean;
}

export interface EndpointValidationResult {
  method: string;
  path: string;
  isValid: boolean;
  issues: ValidationIssue[];
  warnings: ValidationIssue[];
  details: EndpointValidationDetails;
  validatedAt: string;
}

export interface EndpointValidationDetails {
  hasAuthentication: boolean;
  hasAuthorization: boolean;
  hasInputValidation: boolean;
  hasOutputValidation: boolean;
  hasErrorHandling: boolean;
  hasDocumentation: boolean;
  hasRateLimiting: boolean;
  hasCaching: boolean;
  securityHeaders: string[];
  supportedContentTypes: string[];
  performanceMetrics: Record<string, any>;
}
