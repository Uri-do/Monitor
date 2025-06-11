// Query Key Factories for consistent query key management
// This prevents string-based query key errors and provides type safety

export const queryKeys = {
  // KPI related queries
  kpis: {
    all: ['kpis'] as const,
    lists: () => [...queryKeys.kpis.all, 'list'] as const,
    list: (filters: any) => [...queryKeys.kpis.lists(), filters] as const,
    details: () => [...queryKeys.kpis.all, 'detail'] as const,
    detail: (id: number) => [...queryKeys.kpis.details(), id] as const,
    executions: (id: number) => [...queryKeys.kpis.detail(id), 'executions'] as const,
    analytics: (id: number) => [...queryKeys.kpis.detail(id), 'analytics'] as const,
    dashboard: () => [...queryKeys.kpis.all, 'dashboard'] as const,
  },

  // Alert related queries
  alerts: {
    all: ['alerts'] as const,
    lists: () => [...queryKeys.alerts.all, 'list'] as const,
    list: (filters: any) => [...queryKeys.alerts.lists(), filters] as const,
    details: () => [...queryKeys.alerts.all, 'detail'] as const,
    detail: (id: number) => [...queryKeys.alerts.details(), id] as const,
    statistics: (timeRangeDays?: number) => [...queryKeys.alerts.all, 'statistics', timeRangeDays] as const,
    dashboard: () => [...queryKeys.alerts.all, 'dashboard'] as const,
  },

  // Contact related queries
  contacts: {
    all: ['contacts'] as const,
    lists: () => [...queryKeys.contacts.all, 'list'] as const,
    list: (filters: any) => [...queryKeys.contacts.lists(), filters] as const,
    details: () => [...queryKeys.contacts.all, 'detail'] as const,
    detail: (id: number) => [...queryKeys.contacts.details(), id] as const,
  },

  // User related queries
  users: {
    all: ['users'] as const,
    lists: () => [...queryKeys.users.all, 'list'] as const,
    list: (filters: any) => [...queryKeys.users.lists(), filters] as const,
    details: () => [...queryKeys.users.all, 'detail'] as const,
    detail: (id: string) => [...queryKeys.users.details(), id] as const,
    profile: () => [...queryKeys.users.all, 'profile'] as const,
    roles: (userId?: string) => [...queryKeys.users.all, 'roles', userId] as const,
  },

  // Role related queries
  roles: {
    all: ['roles'] as const,
    lists: () => [...queryKeys.roles.all, 'list'] as const,
    list: (filters?: any) => [...queryKeys.roles.lists(), filters] as const,
    details: () => [...queryKeys.roles.all, 'detail'] as const,
    detail: (id: string) => [...queryKeys.roles.details(), id] as const,
  },

  // Permission related queries
  permissions: {
    all: ['permissions'] as const,
    lists: () => [...queryKeys.permissions.all, 'list'] as const,
    list: (filters?: any) => [...queryKeys.permissions.lists(), filters] as const,
  },

  // Execution History related queries
  executionHistory: {
    all: ['executionHistory'] as const,
    lists: () => [...queryKeys.executionHistory.all, 'list'] as const,
    list: (filters: any) => [...queryKeys.executionHistory.lists(), filters] as const,
    details: () => [...queryKeys.executionHistory.all, 'detail'] as const,
    detail: (id: number) => [...queryKeys.executionHistory.details(), id] as const,
    byKpi: (kpiId: number, limit?: number) => [...queryKeys.executionHistory.all, 'byKpi', kpiId, limit] as const,
    statistics: (timeRangeDays: number) => [...queryKeys.executionHistory.all, 'statistics', timeRangeDays] as const,
    recent: (limit: number) => [...queryKeys.executionHistory.all, 'recent', limit] as const,
  },

  // System related queries
  system: {
    all: ['system'] as const,
    health: () => [...queryKeys.system.all, 'health'] as const,
    analytics: () => [...queryKeys.system.all, 'analytics'] as const,
    settings: () => [...queryKeys.system.all, 'settings'] as const,
  },

  // Dashboard related queries
  dashboard: {
    all: ['dashboard'] as const,
    overview: () => [...queryKeys.dashboard.all, 'overview'] as const,
    realtime: () => [...queryKeys.dashboard.all, 'realtime'] as const,
    metrics: (timeRange: string) => [...queryKeys.dashboard.all, 'metrics', timeRange] as const,
  },
} as const;

// Helper function to invalidate related queries
export const getInvalidationKeys = {
  kpi: (id?: number) => {
    if (id) {
      return [queryKeys.kpis.detail(id), queryKeys.kpis.lists()];
    }
    return [queryKeys.kpis.all];
  },

  alert: (id?: number) => {
    if (id) {
      return [queryKeys.alerts.detail(id), queryKeys.alerts.lists()];
    }
    return [queryKeys.alerts.all];
  },

  contact: (id?: number) => {
    if (id) {
      return [queryKeys.contacts.detail(id), queryKeys.contacts.lists()];
    }
    return [queryKeys.contacts.all];
  },

  user: (id?: string) => {
    if (id) {
      return [queryKeys.users.detail(id), queryKeys.users.lists()];
    }
    return [queryKeys.users.all];
  },

  dashboard: () => [queryKeys.dashboard.all],
  system: () => [queryKeys.system.all],
};
