// API Types for Monitoring Grid Frontend

export interface KpiDto {
  kpiId: number;
  indicator: string;
  owner: string;
  priority: number;
  priorityName: string;
  frequency: number;
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
}

export interface CreateKpiRequest {
  indicator: string;
  owner: string;
  priority: number;
  frequency: number;
  deviation: number;
  spName: string;
  subjectTemplate: string;
  descriptionTemplate: string;
  isActive: boolean;
  cooldownMinutes: number;
  minimumThreshold?: number;
  contactIds: number[];
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
  isSuccessful: boolean;
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
  page: number;
  pageSize: number;
  sortBy: string;
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
