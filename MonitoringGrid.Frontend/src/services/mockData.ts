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
    indicator: 'Deposits',
    owner: 'Armon',
    priority: 1,
    priorityName: 'Critical',
    frequency: 5,
    lastMinutes: 1440,
    deviation: 15,
    spName: 'sp_GetDeposits',
    subjectTemplate: 'Deposits Alert: {{indicator}}',
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
    indicator: 'Transaction Volume',
    owner: 'Tech Team',
    priority: 1,
    priorityName: 'Critical',
    frequency: 15,
    lastMinutes: 720,
    deviation: 10,
    spName: 'sp_GetTransactionVolume',
    subjectTemplate: 'Transaction Volume Alert: {{indicator}}',
    descriptionTemplate: 'Current: {{current}}, Historical: {{historical}}, Deviation: {{deviation}}%',
    isActive: true,
    lastRun: '2024-01-15T09:45:00Z',
    cooldownMinutes: 60,
    minimumThreshold: 80,
    createdDate: '2024-01-01T00:00:00Z',
    modifiedDate: '2024-01-15T09:45:00Z',
    contacts: []
  },
  {
    kpiId: 3,
    indicator: 'Settlement Companies',
    owner: 'Mike',
    priority: 1,
    priorityName: 'Critical',
    frequency: 15,
    lastMinutes: 2880,
    deviation: 12,
    spName: 'sp_GetSettlementCompanies',
    subjectTemplate: 'Settlement Alert: {{indicator}}',
    descriptionTemplate: 'Current: {{current}}, Historical: {{historical}}, Deviation: {{deviation}}%',
    isActive: true,
    lastRun: '2024-01-15T07:30:00Z',
    cooldownMinutes: 45,
    minimumThreshold: 50,
    createdDate: '2024-01-01T00:00:00Z',
    modifiedDate: '2024-01-15T07:30:00Z',
    contacts: []
  },
  {
    kpiId: 4,
    indicator: 'Transactions',
    owner: 'Gabriel',
    priority: 2,
    priorityName: 'High',
    frequency: 30,
    lastMinutes: 1440,
    deviation: 8,
    spName: 'sp_GetTransactions',
    subjectTemplate: 'Transactions Alert: {{indicator}}',
    descriptionTemplate: 'Current: {{current}}, Historical: {{historical}}, Deviation: {{deviation}}%',
    isActive: true,
    lastRun: '2024-01-15T10:15:00Z',
    cooldownMinutes: 30,
    minimumThreshold: 100,
    createdDate: '2024-01-01T00:00:00Z',
    modifiedDate: '2024-01-15T10:15:00Z',
    contacts: []
  },
  {
    kpiId: 5,
    indicator: 'White Label Performance',
    owner: 'Ilia',
    priority: 2,
    priorityName: 'High',
    frequency: 30,
    lastMinutes: 1440,
    deviation: 20,
    spName: 'sp_GetWhiteLabelPerformance',
    subjectTemplate: 'White Label Alert: {{indicator}}',
    descriptionTemplate: 'Current: {{current}}, Historical: {{historical}}, Deviation: {{deviation}}%',
    isActive: true,
    lastRun: '2024-01-15T09:30:00Z',
    cooldownMinutes: 60,
    minimumThreshold: 75,
    createdDate: '2024-01-01T00:00:00Z',
    modifiedDate: '2024-01-15T09:30:00Z',
    contacts: []
  },
  {
    kpiId: 6,
    indicator: 'Legacy System Monitor',
    owner: 'System Admin',
    priority: 2,
    priorityName: 'High',
    frequency: 60,
    lastMinutes: 10080,
    deviation: 25,
    spName: 'sp_GetLegacySystemStatus',
    subjectTemplate: 'Legacy System Alert: {{indicator}}',
    descriptionTemplate: 'Current: {{current}}, Historical: {{historical}}, Deviation: {{deviation}}%',
    isActive: false,
    lastRun: '2024-01-14T15:00:00Z',
    cooldownMinutes: 120,
    minimumThreshold: 90,
    createdDate: '2024-01-01T00:00:00Z',
    modifiedDate: '2024-01-14T15:00:00Z',
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
  totalKpis: 6,
  activeKpis: 5,
  inactiveKpis: 1,
  kpisInErrorCount: 1,
  kpisDue: 3,
  alertsToday: 2,
  alertsThisWeek: 8,
  lastUpdate: '2024-01-15T10:45:00Z',
  recentAlerts: [],
  kpisInError: [],
  dueKpis: [
    {
      kpiId: 1,
      indicator: 'Deposits',
      owner: 'Armon',
      isActive: true,
      lastRun: '2024-01-15T08:00:00Z',
      frequency: 5,
      status: 'Never Run',
      alertsToday: 0
    },
    {
      kpiId: 2,
      indicator: 'Transaction Volume',
      owner: 'Tech Team',
      isActive: true,
      lastRun: '2024-01-15T09:45:00Z',
      frequency: 15,
      status: 'Never Run',
      alertsToday: 0
    },
    {
      kpiId: 3,
      indicator: 'Settlement Companies',
      owner: 'Mike',
      isActive: true,
      lastRun: '2024-01-15T07:30:00Z',
      frequency: 15,
      status: 'Never Run',
      alertsToday: 0
    },
    {
      kpiId: 4,
      indicator: 'Transactions',
      owner: 'Gabriel',
      isActive: true,
      lastRun: '2024-01-15T10:15:00Z',
      frequency: 30,
      status: 'Never Run',
      alertsToday: 0
    },
    {
      kpiId: 5,
      indicator: 'White Label Performance',
      owner: 'Ilia',
      isActive: true,
      lastRun: '2024-01-15T09:30:00Z',
      frequency: 30,
      status: 'Never Run',
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
      indicator: 'Deposits',
      owner: 'Armon',
      alertCount: 5,
      unresolvedCount: 2,
      lastAlert: '2024-01-15T10:30:00Z',
      averageDeviation: 18.5
    },
    {
      kpiId: 2,
      indicator: 'Transaction Volume',
      owner: 'Tech Team',
      alertCount: 3,
      unresolvedCount: 1,
      lastAlert: '2024-01-15T08:45:00Z',
      averageDeviation: 12.3
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
