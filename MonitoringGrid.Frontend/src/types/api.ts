// API Types for Monitoring Grid Frontend

// ===== INDICATOR TYPES (New System) =====

// Indicator Types
export interface IndicatorDto {
  indicatorID: number;
  indicatorName: string;
  indicatorCode: string;
  indicatorDesc?: string;
  collectorID?: number;
  collectorName?: string; // Name of the collector (e.g., "Bingo")
  collectorItemName?: string;
  schedulerID?: number;
  isActive: boolean;
  lastMinutes: number;
  thresholdType?: string;
  thresholdField?: string;
  thresholdComparison?: string;
  thresholdValue?: number;
  priority?: string;
  ownerContactId: number;
  ownerName?: string; // API returns this
  averageLastDays?: number;
  createdDate: string;
  updatedDate?: string;
  modifiedDate?: string; // API returns this instead of updatedDate
  lastRun?: string;
  lastRunResult?: string;
  isCurrentlyRunning: boolean;
  executionStartTime?: string;
  executionContext?: string;
  // Navigation properties
  ownerContact?: ContactDto;
  contacts?: ContactDto[];
  scheduler?: SchedulerDto;
}

export interface CreateIndicatorRequest {
  indicatorName: string;
  indicatorCode: string;
  indicatorDescription?: string; // Backend expects indicatorDescription
  collectorId: number; // Backend expects collectorId (lowercase 'd')
  collectorItemName: string;
  schedulerId?: number; // Backend expects schedulerId (lowercase 'd')
  isActive: boolean;
  lastMinutes: number;
  thresholdType: string; // Changed from optional to required to match backend
  thresholdField: string; // Changed from optional to required to match backend
  thresholdComparison: string; // Changed from optional to required to match backend
  thresholdValue: number; // Changed from optional to required to match backend
  priority: string; // Changed from number to string to match backend
  ownerContactId: number;
  averageLastDays?: number;
  contactIds: number[];
}

export interface UpdateIndicatorRequest extends CreateIndicatorRequest {
  indicatorID: number;
}

export interface TestIndicatorRequest {
  indicatorID: number;
  customFrequency?: number;
}

export interface IndicatorExecutionResultDto {
  indicatorId: number;
  indicatorName: string;
  executionId: string;
  isSuccess: boolean;
  executionTime: number;
  resultValue?: number;
  resultMessage?: string;
  errorMessage?: string;
  executedAt: string;
  executedBy: string;
}

export interface IndicatorDashboardDto {
  totalIndicators: number;
  activeIndicators: number;
  inactiveIndicators: number;
  indicatorsInErrorCount: number;
  indicatorsDue: number;
  indicatorsRunning: number;
  alertsToday: number;
  alertsThisWeek: number;
  lastUpdate: string;
  recentAlerts: IndicatorStatusDto[];
  indicatorsInError: IndicatorStatusDto[];
  dueIndicators: IndicatorStatusDto[];
  runningIndicators: IndicatorStatusDto[];
  nextIndicatorDue?: IndicatorStatusDto;
}

export interface IndicatorStatusDto {
  indicatorId: number;
  indicatorName: string;
  status: string;
  lastExecuted?: string;
  nextDue?: string;
  priority: number;
  ownerContact?: string;
}

// Collector Types (for ProgressPlayDB integration)
export interface CollectorDto {
  collectorId: number;
  collectorCode: string;
  collectorName: string;
  collectorDesc?: string;
  isActive: boolean;
  createdDate: string;
  updatedDate: string;
}

// ===== SCHEDULER TYPES =====

export interface SchedulerDto {
  schedulerID: number;
  schedulerName: string;
  schedulerDescription?: string;
  scheduleType: ScheduleType;
  intervalMinutes?: number;
  cronExpression?: string;
  executionDateTime?: string;
  startDate?: string;
  endDate?: string;
  timezone: string;
  isEnabled: boolean;
  createdDate: string;
  createdBy: string;
  modifiedDate: string;
  modifiedBy: string;
  displayText: string;
  nextExecutionTime?: string;
  isCurrentlyActive: boolean;
  indicatorCount: number;
  // Additional properties referenced in components
  isActive?: boolean;
  nextRunTime?: string;
  lastRunTime?: string;
  description?: string;
}

export enum ScheduleType {
  Interval = 'interval',
  Cron = 'cron',
  OneTime = 'onetime',
}

export interface CreateSchedulerRequest {
  schedulerName: string;
  schedulerDescription?: string;
  scheduleType: string;
  intervalMinutes?: number;
  cronExpression?: string;
  executionDateTime?: string;
  startDate?: string;
  endDate?: string;
  timezone: string;
  isEnabled: boolean;
}

export interface UpdateSchedulerRequest extends CreateSchedulerRequest {
  schedulerID: number;
}

// Running Indicator Type (for real-time monitoring)
export interface RunningIndicator {
  indicatorID: number;
  indicatorName: string;
  indicatorCode: string;
  collectorID?: number;
  collectorItemName?: string;
  startTime: string;
  progress?: number;
  estimatedTimeRemaining?: number;
  executionContext?: string;
  status: 'running' | 'completed' | 'failed';
}

// Worker Service Type
export interface WorkerService {
  name: string;
  status: string;
  lastActivity?: string;
  currentActivity?: string;
  errorMessage?: string;
  description?: string;
  processedCount?: number;
  successCount?: number;
  failureCount?: number;
}

export interface CronPreset {
  name: string;
  expression: string;
  description: string;
}

// Indicator Types
export enum IndicatorType {
  SuccessRate = 'success_rate',
  TransactionVolume = 'transaction_volume',
  Threshold = 'threshold',
  TrendAnalysis = 'trend_analysis',
}

export interface IndicatorTypeDefinition {
  type: IndicatorType;
  name: string;
  description: string;
  requiredFields: string[];
  defaultStoredProcedure?: string;
}

export interface ContactDto {
  contactID: number;
  name: string;
  email?: string;
  phone?: string;
  isActive: boolean;
  createdDate: string;
  modifiedDate: string;
  indicatorCount: number;
  assignedIndicators: IndicatorSummaryDto[];
}

export interface CreateContactRequest {
  name: string;
  email?: string;
  phone?: string;
  isActive: boolean;
}

export interface UpdateContactRequest extends CreateContactRequest {
  contactID: number;
}

export interface IndicatorSummaryDto {
  indicatorID: number;
  indicator: string;
  owner: string;
  priority: number;
  isActive: boolean;
}

export interface AlertLogDto {
  alertID: number;
  indicatorID: number;
  indicatorName: string;
  indicatorOwner: string;
  triggerTime: string;
  message: string;
  details?: string;
  sentVia: number;
  sentViaName: string;
  sentTo: string;
  currentValue?: number;
  historicalValue?: number;
  deviationPercent?: number;
  isResolved: boolean;
  resolvedTime?: string;
  resolvedBy?: string;
  severity: string;
}

export interface KpiStatusDto {
  kpiId: number;
  indicator: string;
  owner: string;
  isActive: boolean;
  lastRun?: string;
  nextRun?: string;
  frequency: number;
  status: string;
  lastAlert?: string;
  alertsToday: number;
  lastCurrentValue?: number;
  lastHistoricalValue?: number;
  lastDeviation?: number;
  minutesUntilDue?: number;

  // Execution state properties
  isCurrentlyRunning: boolean;
  executionStartTime?: string;
  executionContext?: string;
  executionDurationSeconds?: number;
}

export interface KpiDashboardDto {
  totalKpis: number;
  activeKpis: number;
  inactiveKpis: number;
  kpisInErrorCount: number;
  kpisDue: number;
  kpisRunning: number;
  alertsToday: number;
  alertsThisWeek: number;
  lastUpdate: string;
  recentAlerts: KpiStatusDto[];
  kpisInError: KpiStatusDto[];
  dueKpis: KpiStatusDto[];
  runningKpis: KpiStatusDto[];
  nextKpiDue?: KpiStatusDto;
  recentExecutions: ExecutionStatsDto[];
}

export interface AlertDashboardDto {
  totalAlertsToday: number;
  unresolvedAlerts: number;
  criticalAlerts: number;
  alertsLastHour: number;
  alertTrendPercentage: number;
  recentAlerts: AlertLogDto[];
  topAlertingKpis: KpiAlertSummaryDto[];
  hourlyTrend: AlertTrendDto[];
}

export interface KpiAlertSummaryDto {
  kpiId: number;
  indicator: string;
  owner: string;
  alertCount: number;
  unresolvedCount: number;
  lastAlert?: string;
  averageDeviation?: number;
}

export interface AlertTrendDto {
  date: string;
  alertCount: number;
  criticalCount: number;
  highCount: number;
  mediumCount: number;
  lowCount: number;
}

export interface KpiExecutionResultDto {
  kpiId: number;
  indicator: string;
  key: string;
  currentValue: number;
  historicalValue: number;
  deviationPercent: number;
  shouldAlert: boolean;
  errorMessage?: string;
  executionTime: string;
  executionTimeMs?: number;
  executionDetails?: string;
  metadata?: Record<string, any>;
  isSuccessful: boolean;

  // Enhanced execution information
  timingInfo?: ExecutionTimingInfo;
  databaseInfo?: DatabaseExecutionInfo;
  executionSteps?: ExecutionStepInfo[];
}

export interface KpiMetricsDto {
  kpiId: number;
  indicator: string;
  totalExecutions: number;
  successfulExecutions: number;
  failedExecutions: number;
  successRate: number;
  totalAlerts: number;
  lastExecution?: string;
  lastAlert?: string;
  averageExecutionTime: string;
  trendData: KpiTrendDataDto[];
}

export interface KpiTrendDataDto {
  timestamp: string;
  value: number;
  metricKey: string;
  period: number;
}

export interface AlertFilterDto {
  startDate?: string;
  endDate?: string;
  kpiIds?: number[];
  owners?: string[];
  isResolved?: boolean;
  sentVia?: number[];
  minDeviation?: number;
  maxDeviation?: number;
  searchText?: string;
  severity?: string;
  page: number;
  pageSize: number;
  sortBy?: string;
  sortDirection: string;
}

export interface PaginatedAlertsDto {
  alerts: AlertLogDto[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface AlertStatisticsDto {
  totalAlerts: number;
  unresolvedAlerts: number;
  resolvedAlerts: number;
  alertsToday: number;
  alertsThisWeek: number;
  alertsThisMonth: number;
  criticalAlerts: number;
  highPriorityAlerts: number;
  averageResolutionTimeHours: number;
  dailyTrend: AlertTrendDto[];
  topAlertingKpis: KpiAlertSummaryDto[];
  // Additional properties for enhanced analytics
  successRate?: number;
  avgResponseTime?: number;
  trendData?: TrendDataDto[];
  severityDistribution?: AlertSeverityStatsDto[];
}

export interface AlertSeverityStatsDto {
  severity: string;
  count: number;
  percentage: number;
  color?: string;
}

export interface ResolveAlertRequest {
  alertId: number;
  resolvedBy: string;
  resolutionNotes?: string;
}

export interface BulkResolveAlertsRequest {
  alertIds: number[];
  resolvedBy: string;
  resolutionNotes?: string;
}

export interface TestKpiRequest {
  kpiId: number;
  customFrequency?: number;
}

export interface BulkKpiOperationRequest {
  kpiIds: number[];
  operation: string;
}

export interface BulkContactOperationRequest {
  contactIds: number[];
  operation: string;
}

export interface ApiResponse<T> {
  data: T;
  success: boolean;
  message?: string;
  errors?: string[];
}

export interface HealthCheckResponse {
  status: string;
  checks: HealthCheck[];
  totalDuration: number;
}

export interface HealthCheck {
  name: string;
  status: string;
  description?: string;
  duration: number;
}

export interface ExecutionHistoryDto {
  historicalId: number;
  kpiId: number;
  indicator: string;
  kpiOwner: string;
  spName: string;
  timestamp: string;
  executedBy?: string;
  executionMethod?: string;
  currentValue: number;
  historicalValue?: number;
  deviationPercent?: number;
  period: number;
  metricKey: string;
  isSuccessful: boolean;
  errorMessage?: string;
  executionTimeMs?: number;
  databaseName?: string;
  serverName?: string;
  shouldAlert: boolean;
  alertSent: boolean;
  sessionId?: string;
  ipAddress?: string;
  sqlCommand?: string;
  rawResponse?: string;
  executionContext?: string;
  performanceCategory: string;
  deviationCategory: string;
}

export interface ExecutionHistoryDetailDto extends ExecutionHistoryDto {
  userAgent?: string;
  sqlParameters?: string;
  connectionString?: string;
  // Additional properties referenced in components
  alertThreshold?: number;
  alertOperator?: string;
  resultCount?: number;
}

export interface PaginatedExecutionHistoryDto {
  executions: ExecutionHistoryDto[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface ExecutionStatsDto {
  kpiId: number;
  indicator: string;
  owner: string;
  totalExecutions: number;
  successfulExecutions: number;
  failedExecutions: number;
  successRate: number;
  avgExecutionTimeMs?: number;
  minExecutionTimeMs?: number;
  maxExecutionTimeMs?: number;
  alertsTriggered: number;
  alertsSent: number;
  lastExecution?: string;
  uniqueExecutors: number;
  executionMethods: number;
  isSuccessful?: boolean;
}

// Enhanced Analytics API Types
export interface SystemAnalyticsDto {
  period: number;
  startDate: string;
  endDate: string;
  totalKpis: number;
  activeKpis: number;
  inactiveKpis: number;
  totalExecutions: number;
  totalAlerts: number;
  resolvedAlerts: number;
  unresolvedAlerts: number;
  criticalAlerts: number;
  averageExecutionsPerDay: number;
  averageAlertsPerDay: number;
  topPerformingKpis: KpiSummaryAnalyticsDto[];
  worstPerformingKpis: KpiSummaryAnalyticsDto[];
  alertTrends: TrendDataDto[];
  executionTrends: TrendDataDto[];
  kpiHealthDistribution: HealthDistributionDto;
}

export interface KpiPerformanceAnalyticsDto {
  kpiId: number;
  indicator: string;
  owner: string;
  period: number;
  startDate: string;
  endDate: string;
  totalExecutions: number;
  successfulExecutions: number;
  failedExecutions: number;
  successRate: number;
  totalAlerts: number;
  criticalAlerts: number;
  averageDeviation: number;
  maxDeviation: number;
  averageExecutionTime: number;
  trendDirection: string;
  performanceScore: number;
  recommendations: string[];
  detailedTrends: KpiTrendPointDto[];
}

export interface OwnerAnalyticsDto {
  owner: string;
  ownerDomain: string;
  totalKpis: number;
  activeKpis: number;
  inactiveKpis: number;
  totalAlerts: number;
  criticalAlerts: number;
  totalExecutions: number;
  successfulExecutions: number;
  successRate: number;
  averageDeviation: number;
  performanceScore: number;
}

export interface SystemHealthDto {
  timestamp: string;
  overallHealthScore: number;
  totalKpis: number;
  activeKpis: number;
  healthyKpis: number;
  warningKpis: number;
  criticalKpis: number;
  alertsLast24Hours: number;
  alertsLastHour: number;
  unresolvedAlerts: number;
  criticalAlerts: number;
  systemStatus: string;
  issues: string[];
  recommendations: string[];
}

export interface KpiSummaryAnalyticsDto {
  kpiId: number;
  indicator: string;
  owner: string;
  performanceScore: number;
}

export interface TrendDataDto {
  date: string;
  value: number;
}

export interface HealthDistributionDto {
  healthy: number;
  warning: number;
  critical: number;
  inactive: number;
}

export interface KpiTrendPointDto {
  timestamp: string;
  value: number;
  deviationPercent: number;
  executionTimeMs: number;
  isSuccessful: boolean;
  triggeredAlert: boolean;
}

// Real-time API Types
export interface RealtimeStatusDto {
  timestamp: string;
  activeKpis: number;
  dueKpis: number;
  recentAlerts: number;
  unresolvedAlerts: number;
  systemLoad: number;
  lastUpdate: string;
}

export interface LiveDashboardDto {
  timestamp: string;
  totalKpis: number;
  activeKpis: number;
  dueKpis: number;
  executionsLastHour: number;
  alertsLast24Hours: number;
  unresolvedAlerts: number;
  criticalAlerts: number;
  systemHealth: string;
  recentExecutions: RecentExecutionDto[];
  recentAlerts: RecentAlertDto[];
}

export interface RecentExecutionDto {
  kpiId: number;
  indicator: string;
  timestamp: string;
  value: number;
  deviationPercent: number;
  isSuccessful: boolean;
  executionTimeMs: number;
}

export interface RecentAlertDto {
  kpiId: number;
  indicator: string;
  owner: string;
  triggerTime: string;
  deviation: number;
  severity: string;
}

export interface WebhookPayloadDto {
  type: string;
  source: string;
  timestamp: string;
  data: Record<string, any>;
}

export interface ConnectionInfoDto {
  hubUrl: string;
  accessToken: string;
  connectionId: string;
  supportedEvents: string[];
}

// Enhanced Alert Types
export interface EnhancedAlertDto extends AlertLogDto {
  severityColor: string;
  requiresImmediateAttention: boolean;
  deviationCategory: string;
}

export interface AlertNotificationDto {
  kpiId: number;
  indicator: string;
  owner: string;
  priority: number;
  currentValue: number;
  historicalValue: number;
  deviation: number;
  subject: string;
  description: string;
  triggerTime: string;
  severity: string;
  notifiedContacts: string[];
}

export interface ManualAlertRequest {
  kpiId: number;
  message: string;
  details?: string;
  priority: number;
}

// Enhanced Execution Information Types
export interface ExecutionTimingInfo {
  startTime: string;
  endTime: string;
  totalExecutionMs: number;
  databaseConnectionMs: number;
  storedProcedureExecutionMs: number;
  resultProcessingMs: number;
  historicalDataSaveMs: number;
}

export interface DatabaseExecutionInfo {
  connectionString: string;
  databaseName: string;
  serverName: string;
  sqlCommand: string;
  sqlParameters: string;
  rawResponse: string;
  rowsReturned: number;
  resultSetsReturned: number;
}

export interface ExecutionStepInfo {
  stepName: string;
  startTime: string;
  endTime: string;
  durationMs: number;
  status: string; // "Success", "Error", "Warning", "Active"
  details?: string;
  errorMessage?: string;
  stepMetadata?: Record<string, any>;
}
