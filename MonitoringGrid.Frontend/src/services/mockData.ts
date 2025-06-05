import {
  KpiDto,
  ContactDto,
  AlertLogDto,
  KpiDashboardDto,
  AlertDashboardDto,
  KpiExecutionResultDto,
  KpiMetricsDto,
  AlertStatisticsDto,
  PaginatedAlertsDto,
  AlertFilterDto,
  HealthCheckResponse
} from '@/types/api';

// Mock KPI data
export const mockKpis: KpiDto[] = [
  {
    kpiId: 1,
    indicator: 'Daily Sales Revenue',
    owner: 'John Smith',
    priority: 1,
    priorityName: 'Critical',
    frequency: 60,
    deviation: 15,
    spName: 'sp_GetDailySalesRevenue',
    subjectTemplate: 'Sales Alert: {{indicator}}',
    descriptionTemplate: 'Current: {{current}}, Historical: {{historical}}, Deviation: {{deviation}}%',
    isActive: true,
    lastRun: '2024-01-15T10:30:00Z',
    cooldownMinutes: 30,
    minimumThreshold: 1000,
    createdDate: '2024-01-01T00:00:00Z',
    modifiedDate: '2024-01-15T10:30:00Z',
    contacts: []
  },
  {
    kpiId: 2,
    indicator: 'Customer Satisfaction Score',
    owner: 'Sarah Johnson',
    priority: 2,
    priorityName: 'High',
    frequency: 120,
    deviation: 10,
    spName: 'sp_GetCustomerSatisfaction',
    subjectTemplate: 'Customer Alert: {{indicator}}',
    descriptionTemplate: 'Current: {{current}}, Historical: {{historical}}, Deviation: {{deviation}}%',
    isActive: true,
    lastRun: '2024-01-15T09:00:00Z',
    cooldownMinutes: 60,
    minimumThreshold: 80,
    createdDate: '2024-01-01T00:00:00Z',
    modifiedDate: '2024-01-15T09:00:00Z',
    contacts: []
  },
  {
    kpiId: 3,
    indicator: 'System Response Time',
    owner: 'Mike Wilson',
    priority: 1,
    priorityName: 'Critical',
    frequency: 30,
    deviation: 20,
    spName: 'sp_GetSystemResponseTime',
    subjectTemplate: 'Performance Alert: {{indicator}}',
    descriptionTemplate: 'Current: {{current}}ms, Historical: {{historical}}ms, Deviation: {{deviation}}%',
    isActive: true,
    lastRun: '2024-01-15T10:45:00Z',
    cooldownMinutes: 15,
    minimumThreshold: 500,
    createdDate: '2024-01-01T00:00:00Z',
    modifiedDate: '2024-01-15T10:45:00Z',
    contacts: []
  }
];

// Mock Contact data
export const mockContacts: ContactDto[] = [
  {
    contactId: 1,
    name: 'John Smith',
    email: 'john.smith@company.com',
    phone: '+1-555-0101',
    isActive: true,
    createdDate: '2024-01-01T00:00:00Z',
    modifiedDate: '2024-01-15T10:30:00Z',
    assignedKpis: [
      { kpiId: 1, indicator: 'Daily Sales Revenue', owner: 'John Smith', priority: 1, isActive: true }
    ]
  },
  {
    contactId: 2,
    name: 'Sarah Johnson',
    email: 'sarah.johnson@company.com',
    phone: '+1-555-0102',
    isActive: true,
    createdDate: '2024-01-01T00:00:00Z',
    modifiedDate: '2024-01-15T09:00:00Z',
    assignedKpis: [
      { kpiId: 2, indicator: 'Customer Satisfaction Score', owner: 'Sarah Johnson', priority: 2, isActive: true }
    ]
  },
  {
    contactId: 3,
    name: 'Mike Wilson',
    email: 'mike.wilson@company.com',
    phone: '+1-555-0103',
    isActive: true,
    createdDate: '2024-01-01T00:00:00Z',
    modifiedDate: '2024-01-15T10:45:00Z',
    assignedKpis: [
      { kpiId: 3, indicator: 'System Response Time', owner: 'Mike Wilson', priority: 1, isActive: true }
    ]
  }
];

// Mock Alert data
export const mockAlerts: AlertLogDto[] = [
  {
    alertId: 1,
    kpiId: 1,
    kpiIndicator: 'Daily Sales Revenue',
    kpiOwner: 'John Smith',
    triggerTime: '2024-01-15T10:30:00Z',
    message: 'Sales revenue dropped by 18% compared to historical average',
    details: 'Current: $45,000, Historical: $55,000, Deviation: -18.2%',
    sentVia: 1,
    sentViaName: 'Email',
    sentTo: 'john.smith@company.com',
    currentValue: 45000,
    historicalValue: 55000,
    deviationPercent: -18.2,
    isResolved: false,
    severity: 'Critical'
  },
  {
    alertId: 2,
    kpiId: 3,
    kpiIndicator: 'System Response Time',
    kpiOwner: 'Mike Wilson',
    triggerTime: '2024-01-15T10:45:00Z',
    message: 'System response time increased by 25% above normal',
    details: 'Current: 750ms, Historical: 600ms, Deviation: +25.0%',
    sentVia: 2,
    sentViaName: 'SMS',
    sentTo: '+1-555-0103',
    currentValue: 750,
    historicalValue: 600,
    deviationPercent: 25.0,
    isResolved: true,
    resolvedTime: '2024-01-15T11:00:00Z',
    resolvedBy: 'Mike Wilson',
    severity: 'High'
  }
];

// Mock Dashboard data
export const mockKpiDashboard: KpiDashboardDto = {
  totalKpis: 3,
  activeKpis: 3,
  inactiveKpis: 0,
  kpisInError: 1,
  kpisDue: 1,
  alertsToday: 2,
  alertsThisWeek: 8,
  lastUpdate: '2024-01-15T10:45:00Z',
  recentAlerts: [
    {
      kpiId: 1,
      indicator: 'Daily Sales Revenue',
      owner: 'John Smith',
      isActive: true,
      lastRun: '2024-01-15T10:30:00Z',
      frequency: 60,
      status: 'Due',
      alertsToday: 1,
      lastCurrentValue: 45000,
      lastHistoricalValue: 55000,
      lastDeviation: -18.2
    }
  ],
  kpisInErrorList: [],
  dueKpis: [
    {
      kpiId: 2,
      indicator: 'Customer Satisfaction Score',
      owner: 'Sarah Johnson',
      isActive: true,
      lastRun: '2024-01-15T09:00:00Z',
      frequency: 120,
      status: 'Due Soon',
      alertsToday: 0
    }
  ]
};

export const mockAlertDashboard: AlertDashboardDto = {
  totalAlertsToday: 2,
  unresolvedAlerts: 1,
  criticalAlerts: 1,
  alertsLastHour: 1,
  alertTrendPercentage: 15.5,
  recentAlerts: mockAlerts,
  topAlertingKpis: [
    {
      kpiId: 1,
      indicator: 'Daily Sales Revenue',
      owner: 'John Smith',
      alertCount: 5,
      unresolvedCount: 1,
      lastAlert: '2024-01-15T10:30:00Z',
      averageDeviation: 16.8
    }
  ],
  hourlyTrend: [
    {
      date: '2024-01-15T10:00:00Z',
      alertCount: 1,
      criticalCount: 1,
      highCount: 0,
      mediumCount: 0,
      lowCount: 0
    }
  ]
};

// Mock Health Check
export const mockHealthCheck: HealthCheckResponse = {
  status: 'Healthy',
  checks: [
    { name: 'Database', status: 'Healthy', description: 'SQL Server connection OK', duration: 45 },
    { name: 'Email Service', status: 'Healthy', description: 'SMTP server responding', duration: 120 },
    { name: 'SMS Service', status: 'Healthy', description: 'SMS gateway available', duration: 89 }
  ],
  totalDuration: 254
};
