// API Types for Monitoring Grid Frontend

// Scheduler Types
export interface ScheduleConfiguration {
  scheduleType: ScheduleType;
  cronExpression?: string;
  intervalMinutes?: number;
  startDate?: string;
  endDate?: string;
  timezone?: string;
  isEnabled: boolean;
}

export enum ScheduleType {
  Interval = 'interval',
  Cron = 'cron',
  OneTime = 'onetime'
}

export interface CronPreset {
  name: string;
  expression: string;
  description: string;
}

// KPI Types
export enum KpiType {
  SuccessRate = 'success_rate',
  TransactionVolume = 'transaction_volume',
  Threshold = 'threshold',
  TrendAnalysis = 'trend_analysis'
}

export interface KpiTypeDefinition {
  type: KpiType;
  name: string;
  description: string;
  requiredFields: string[];
  defaultStoredProcedure?: string;
}

export interface KpiDto {
  kpiId: number;
  indicator: string;
  owner: string;
  priority: number;
  priorityName: string;
  frequency: number;
  lastMinutes: number;
  deviation: number;
  spName: string;
  subjectTemplate: string;
  descriptionTemplate: string;
  isActive: boolean;
  lastRun?: string;
  cooldownMinutes: number;
  minimumThreshold?: number;
  createdDate: string;
  modifiedDate: string;
  contacts: ContactDto[];
  // New fields for enhanced KPI system
  kpiType?: KpiType;
  scheduleConfiguration?: ScheduleConfiguration;
  thresholdValue?: number;
  comparisonOperator?: 'gt' | 'lt' | 'eq' | 'gte' | 'lte';
}

export interface CreateKpiRequest {
  indicator: string;
  owner: string;
  priority: number;
  frequency: number;
  lastMinutes: number;
  deviation: number;
  spName: string;
  subjectTemplate: string;
  descriptionTemplate: string;
  isActive: boolean;
  cooldownMinutes: number;
  minimumThreshold?: number;
  contactIds: number[];
  // New fields for enhanced KPI system
  kpiType?: KpiType;
  scheduleConfiguration?: ScheduleConfiguration;
  thresholdValue?: number;
  comparisonOperator?: 'gt' | 'lt' | 'eq' | 'gte' | 'lte';
}

export interface UpdateKpiRequest extends CreateKpiRequest {
  kpiId: number;
}

export interface ContactDto {
  contactId: number;
  name: string;
  email?: string;
  phone?: string;
  isActive: boolean;
  createdDate: string;
  modifiedDate: string;
  assignedKpis: KpiSummaryDto[];
}

export interface CreateContactRequest {
  name: string;
  email?: string;
  phone?: string;
  isActive: boolean;
}

export interface UpdateContactRequest extends CreateContactRequest {
  contactId: number;
}

export interface KpiSummaryDto {
  kpiId: number;
  indicator: string;
  owner: string;
  priority: number;
  isActive: boolean;
}

export interface AlertLogDto {
  alertId: number;
  kpiId: number;
  kpiIndicator: string;
  kpiOwner: string;
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
}

export interface KpiDashboardDto {
  totalKpis: number;
  activeKpis: number;
  inactiveKpis: number;
  kpisInErrorCount: number;
  kpisDue: number;
  alertsToday: number;
  alertsThisWeek: number;
  lastUpdate: string;
  recentAlerts: KpiStatusDto[];
  kpisInError: KpiStatusDto[];
  dueKpis: KpiStatusDto[];
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
