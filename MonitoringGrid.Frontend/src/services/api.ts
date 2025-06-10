/**
 * API Service Layer - Updated for Root-Level API Architecture
 *
 * This file has been updated to work with the new root-level API structure:
 *
 * 🎯 KpiController (/api/kpi/*):
 *   - Core KPI operations (CRUD, execute, metrics, dashboard)
 *   - Alert management (KPI-related alerts)
 *   - Contact management (notification contacts)
 *   - Execution history and analytics
 *
 * 🔐 SecurityController (/api/security/*):
 *   - Authentication (login, register, refresh tokens)
 *   - User and role management
 *   - Security configuration and monitoring
 *   - Audit trail and security events
 *
 * 🔄 RealtimeController (/api/realtime/*):
 *   - Real-time status and monitoring
 *   - SignalR operations
 *   - Live dashboard data
 *
 * ⚙️ WorkerController (/api/worker/*):
 *   - Background worker management
 *   - Worker status and control
 */

import axios, { AxiosResponse } from 'axios';
import {
  KpiDto,
  CreateKpiRequest,
  UpdateKpiRequest,
  ContactDto,
  CreateContactRequest,
  UpdateContactRequest,
  AlertLogDto,
  KpiDashboardDto,
  AlertDashboardDto,
  KpiExecutionResultDto,
  KpiMetricsDto,
  AlertFilterDto,
  PaginatedAlertsDto,
  AlertStatisticsDto,
  ResolveAlertRequest,
  BulkResolveAlertsRequest,
  TestKpiRequest,
  BulkKpiOperationRequest,
  BulkContactOperationRequest,
  HealthCheckResponse,
  SystemAnalyticsDto,
  KpiPerformanceAnalyticsDto,
  OwnerAnalyticsDto,
  SystemHealthDto,
  RealtimeStatusDto,
  LiveDashboardDto,
  WebhookPayloadDto,
  ConnectionInfoDto,
  EnhancedAlertDto,
  ManualAlertRequest,
  PaginatedExecutionHistoryDto,
  ExecutionStatsDto,
  ExecutionHistoryDetailDto,
} from '@/types/api';
// Production API - no mock data

// Create axios instance with base configuration
const api = axios.create({
  baseURL: '/api', // Root-level API endpoints (versioning removed)
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json',
  },
});



// Request interceptor for authentication
api.interceptors.request.use(
  config => {
    // Add authentication token if available
    const token = localStorage.getItem('auth_token');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  error => {
    return Promise.reject(error);
  }
);

// Response interceptor for error handling
api.interceptors.response.use(
  response => {
    return response;
  },
  error => {
    console.error('API Response Error:', error);

    if (error.response?.status === 401) {
      // Handle unauthorized access
      console.error('Unauthorized access - redirecting to login');

      // Clear stored tokens
      localStorage.removeItem('auth_token');
      localStorage.removeItem('refresh_token');

      // Redirect to login page if not already there
      if (window.location.pathname !== '/login' && window.location.pathname !== '/auth-test') {
        window.location.href = '/login';
      }
    } else if (error.response?.status >= 500) {
      // Handle server errors
      console.error('Server error occurred');
    }

    return Promise.reject(error);
  }
);

// KPI API endpoints
export const kpiApi = {
  // Get all KPIs with optional filtering
  getKpis: async (params?: {
    isActive?: boolean;
    owner?: string;
    priority?: number;
  }): Promise<KpiDto[]> => {
    const response: AxiosResponse<KpiDto[]> = await api.get('/kpi', { params });
    return response.data;
  },

  // Get KPI by ID
  getKpi: async (id: number): Promise<KpiDto> => {
    const response: AxiosResponse<KpiDto> = await api.get(`/kpi/${id}`);
    return response.data;
  },

  // Create new KPI
  createKpi: async (kpi: CreateKpiRequest): Promise<KpiDto> => {
    const response: AxiosResponse<KpiDto> = await api.post('/kpi', kpi);
    return response.data;
  },

  // Update KPI
  updateKpi: async (kpi: UpdateKpiRequest): Promise<KpiDto> => {
    const response: AxiosResponse<KpiDto> = await api.put(`/kpi/${kpi.kpiId}`, kpi);
    return response.data;
  },

  // Delete KPI
  deleteKpi: async (id: number): Promise<void> => {
    await api.delete(`/kpi/${id}`);
  },

  // Execute KPI manually
  executeKpi: async (request: TestKpiRequest): Promise<KpiExecutionResultDto> => {
    const response: AxiosResponse<KpiExecutionResultDto> = await api.post(
      `/kpi/${request.kpiId}/execute`,
      request
    );
    return response.data;
  },

  // Test KPI (alias for executeKpi for backward compatibility)
  testKpi: async (request: TestKpiRequest): Promise<KpiExecutionResultDto> => {
    const response: AxiosResponse<KpiExecutionResultDto> = await api.post(
      `/kpi/${request.kpiId}/execute`,
      request
    );
    return response.data;
  },

  // Get KPI metrics
  getKpiMetrics: async (id: number, days: number = 30): Promise<KpiMetricsDto> => {
    const response: AxiosResponse<KpiMetricsDto> = await api.get(`/kpi/${id}/metrics`, {
      params: { days },
    });
    return response.data;
  },

  // Get KPI dashboard
  getDashboard: async (): Promise<KpiDashboardDto> => {
    const response: AxiosResponse<KpiDashboardDto> = await api.get('/kpi/dashboard');
    return response.data;
  },

  // Bulk operations
  bulkOperation: async (request: BulkKpiOperationRequest): Promise<{ message: string }> => {
    const response: AxiosResponse<{ message: string }> = await api.post('/kpi/bulk', request);
    return response.data;
  },
};

// Contact API endpoints (now consolidated under KPI controller)
export const contactApi = {
  // Get all contacts
  getContacts: async (params?: { isActive?: boolean; search?: string }): Promise<ContactDto[]> => {
    // Updated to use KPI controller's contact endpoints
    const response: AxiosResponse<ContactDto[]> = await api.get('/kpi/contacts', { params });
    return response.data;
  },

  // Get contact by ID
  getContact: async (id: number): Promise<ContactDto> => {
    // Updated to use KPI controller's contact endpoints
    const response: AxiosResponse<ContactDto> = await api.get(`/kpi/contacts/${id}`);
    return response.data;
  },

  // Create new contact
  createContact: async (contact: CreateContactRequest): Promise<ContactDto> => {
    // Updated to use KPI controller's contact endpoints
    const response: AxiosResponse<ContactDto> = await api.post('/kpi/contacts', contact);
    return response.data;
  },

  // Update contact
  updateContact: async (contact: UpdateContactRequest): Promise<ContactDto> => {
    // Updated to use KPI controller's contact endpoints
    const response: AxiosResponse<ContactDto> = await api.put(
      `/kpi/contacts/${contact.contactId}`,
      contact
    );
    return response.data;
  },

  // Delete contact
  deleteContact: async (id: number): Promise<void> => {
    // Updated to use KPI controller's contact endpoints
    await api.delete(`/kpi/contacts/${id}`);
  },

  // Assign contact to KPIs
  assignToKpis: async (id: number, kpiIds: number[]): Promise<{ message: string }> => {
    // Updated to use KPI controller's contact endpoints
    const response: AxiosResponse<{ message: string }> = await api.post(`/kpi/contacts/${id}/assign`, {
      contactId: id,
      kpiIds,
    });
    return response.data;
  },

  // Bulk operations
  bulkOperation: async (request: BulkContactOperationRequest): Promise<{ message: string }> => {
    // Updated to use KPI controller's contact endpoints
    const response: AxiosResponse<{ message: string }> = await api.post('/kpi/contacts/bulk', request);
    return response.data;
  },
};

// Alert API endpoints (now consolidated under KPI controller)
export const alertApi = {
  // Get alerts with filtering and pagination
  getAlerts: async (filter: AlertFilterDto): Promise<PaginatedAlertsDto> => {
    // Updated to use KPI controller's alert endpoints
    const response: AxiosResponse<PaginatedAlertsDto> = await api.get('/kpi/alerts', {
      params: filter,
    });
    return response.data;
  },

  // Get alert by ID
  getAlert: async (id: number): Promise<AlertLogDto> => {
    // Updated to use KPI controller's alert endpoints
    const response: AxiosResponse<AlertLogDto> = await api.get(`/kpi/alerts/${id}`);
    return response.data;
  },

  // Resolve alert
  resolveAlert: async (request: ResolveAlertRequest): Promise<{ message: string }> => {
    // Updated to use KPI controller's alert endpoints
    const response: AxiosResponse<{ message: string }> = await api.post(
      `/kpi/alerts/${request.alertId}/resolve`,
      request
    );
    return response.data;
  },

  // Bulk resolve alerts
  bulkResolveAlerts: async (request: BulkResolveAlertsRequest): Promise<{ message: string }> => {
    // Updated to use KPI controller's alert endpoints
    const response: AxiosResponse<{ message: string }> = await api.post(
      '/kpi/alerts/resolve-bulk',
      request
    );
    return response.data;
  },

  // Get alert statistics
  getStatistics: async (days: number = 30): Promise<AlertStatisticsDto> => {
    // Updated to use KPI controller's alert endpoints
    const response: AxiosResponse<AlertStatisticsDto> = await api.get('/kpi/alerts/statistics', {
      params: { days },
    });
    return response.data;
  },

  // Get alert dashboard
  getDashboard: async (): Promise<AlertDashboardDto> => {
    const response: AxiosResponse<AlertDashboardDto> = await api.get('/alerts/dashboard');
    return response.data;
  },

  // Get critical alerts requiring immediate attention
  getCriticalAlerts: async (): Promise<EnhancedAlertDto[]> => {
    const response: AxiosResponse<EnhancedAlertDto[]> = await api.get('/kpi/alerts/critical');
    return response.data;
  },

  // Get unresolved alerts
  getUnresolvedAlerts: async (): Promise<EnhancedAlertDto[]> => {
    const response: AxiosResponse<EnhancedAlertDto[]> = await api.get('/kpi/alerts/unresolved');
    return response.data;
  },

  // Send manual alert
  sendManualAlert: async (
    request: ManualAlertRequest
  ): Promise<{ message: string; alertId: number }> => {
    const response: AxiosResponse<{ message: string; alertId: number }> = await api.post(
      '/kpi/alerts/manual',
      request
    );
    return response.data;
  },
};

// System API endpoints
export const systemApi = {
  // Get health check
  getHealth: async (): Promise<HealthCheckResponse> => {
    // Health endpoint is not versioned, so use absolute path
    const response: AxiosResponse<HealthCheckResponse> = await axios.get('/health');
    return response.data;
  },

  // Get API info
  getInfo: async (): Promise<{
    name: string;
    version: string;
    environment: string;
    timestamp: string;
  }> => {
    const response = await api.get('/info');
    return response.data;
  },
};

// Execution History API endpoints (now consolidated under KPI controller)
export const executionHistoryApi = {
  // Get execution history with pagination and filters
  getExecutionHistory: async (params: {
    kpiId?: number;
    executedBy?: string;
    executionMethod?: string;
    isSuccessful?: boolean;
    startDate?: string;
    endDate?: string;
    pageSize?: number;
    pageNumber?: number;
  }): Promise<PaginatedExecutionHistoryDto> => {
    // Updated to use KPI controller's execution history endpoints
    const response: AxiosResponse<PaginatedExecutionHistoryDto> = await api.get(
      '/kpi/execution-history',
      { params }
    );
    return response.data;
  },

  // Get execution statistics
  getExecutionStats: async (params: {
    kpiId?: number;
    days?: number;
  }): Promise<ExecutionStatsDto[]> => {
    // Updated to use KPI controller's execution history endpoints
    const response: AxiosResponse<ExecutionStatsDto[]> = await api.get('/kpi/execution-stats', {
      params,
    });
    return response.data;
  },

  // Get detailed execution information
  getExecutionDetail: async (historicalId: number): Promise<ExecutionHistoryDetailDto> => {
    // Updated to use KPI controller's execution history endpoints
    const response: AxiosResponse<ExecutionHistoryDetailDto> = await api.get(
      `/kpi/execution-history/${historicalId}`
    );
    return response.data;
  },

  // Test database connection and recent records
  testDatabaseConnection: async (): Promise<any> => {
    // Updated to use KPI controller's execution history endpoints
    const response = await api.get('/kpi/execution-history/test');
    return response.data;
  },
};

// Enhanced Analytics API endpoints (now consolidated under KPI controller)
export const analyticsApi = {
  // Get system-wide analytics
  getSystemAnalytics: async (days: number = 30): Promise<SystemAnalyticsDto> => {
    // Updated to use KPI controller's analytics endpoints
    const response: AxiosResponse<SystemAnalyticsDto> = await api.get('/kpi/analytics/system', {
      params: { days },
    });
    return response.data;
  },

  // Get KPI performance analytics
  getKpiPerformanceAnalytics: async (
    id: number,
    days: number = 30
  ): Promise<KpiPerformanceAnalyticsDto> => {
    // Updated to use KPI controller's analytics endpoints
    const response: AxiosResponse<KpiPerformanceAnalyticsDto> = await api.get(
      `/kpi/${id}/analytics`,
      {
        params: { days },
      }
    );
    return response.data;
  },

  // Get owner-based analytics
  getOwnerAnalytics: async (days: number = 30): Promise<OwnerAnalyticsDto[]> => {
    // Updated to use KPI controller's analytics endpoints
    const response: AxiosResponse<OwnerAnalyticsDto[]> = await api.get('/kpi/analytics/owners', {
      params: { days },
    });
    return response.data;
  },

  // Get real-time system health
  getSystemHealth: async (): Promise<SystemHealthDto> => {
    // Updated to use KPI controller's analytics endpoints
    const response: AxiosResponse<SystemHealthDto> = await api.get('/kpi/health');
    return response.data;
  },
};

// Real-time Monitoring API endpoints
export const realtimeApi = {
  // Get real-time system status
  getRealtimeStatus: async (): Promise<RealtimeStatusDto> => {
    const response: AxiosResponse<RealtimeStatusDto> = await api.get('/realtime/status');
    return response.data;
  },

  // Execute KPI in real-time
  executeKpiRealtime: async (id: number): Promise<KpiExecutionResultDto> => {
    const response: AxiosResponse<KpiExecutionResultDto> = await api.post(
      `/realtime/execute/${id}`
    );
    return response.data;
  },

  // Get live dashboard data
  getLiveDashboard: async (): Promise<LiveDashboardDto> => {
    const response: AxiosResponse<LiveDashboardDto> = await api.get('/realtime/dashboard');
    return response.data;
  },

  // Send webhook
  sendWebhook: async (payload: WebhookPayloadDto): Promise<{ message: string }> => {
    const response: AxiosResponse<{ message: string }> = await api.post(
      '/realtime/webhook',
      payload
    );
    return response.data;
  },

  // Get SignalR connection info
  getConnectionInfo: async (): Promise<ConnectionInfoDto> => {
    const response: AxiosResponse<ConnectionInfoDto> = await api.get('/realtime/connection-info');
    return response.data;
  },
};



// Security API endpoints (consolidated authentication, authorization, and security management)
export const securityApi = {
  // Authentication endpoints
  login: async (credentials: { username: string; password: string }): Promise<{
    isSuccess: boolean;
    token?: any;
    user?: any;
    errorMessage?: string;
    requiresTwoFactor?: boolean;
    requiresPasswordChange?: boolean;
  }> => {
    const response = await api.post('/security/auth/login', credentials);
    return response.data;
  },

  register: async (userData: {
    username: string;
    email: string;
    password: string;
    firstName: string;
    lastName: string;
  }): Promise<{
    isSuccess: boolean;
    message: string;
    data?: any;
    errors?: string[];
  }> => {
    const response = await api.post('/security/auth/register', userData);
    return response.data;
  },

  refreshToken: async (refreshToken: string): Promise<any> => {
    const response = await api.post('/security/auth/refresh', { refreshToken });
    return response.data;
  },

  // Security configuration endpoints
  getSecurityConfig: async (): Promise<any> => {
    const response = await api.get('/security/config');
    return response.data;
  },

  updateSecurityConfig: async (config: any): Promise<any> => {
    const response = await api.put('/security/config', config);
    return response.data;
  },

  // User management endpoints
  getUsers: async (): Promise<any[]> => {
    const response = await api.get('/security/users');
    return response.data;
  },

  updateUserRoles: async (userId: string, roles: string[]): Promise<{ message: string }> => {
    const response = await api.put(`/security/users/${userId}/roles`, { roles });
    return response.data;
  },

  // Role and permission management
  getRoles: async (): Promise<any[]> => {
    const response = await api.get('/security/roles');
    return response.data;
  },

  getPermissions: async (): Promise<any[]> => {
    const response = await api.get('/security/permissions');
    return response.data;
  },

  // Security events and audit
  getSecurityEvents: async (params?: {
    startDate?: string;
    endDate?: string;
    userId?: string;
  }): Promise<any[]> => {
    const response = await api.get('/security/events', { params });
    return response.data;
  },

  getUserSecurityEvents: async (userId: string, params?: {
    startDate?: string;
    endDate?: string;
  }): Promise<any[]> => {
    const response = await api.get(`/security/events/user/${userId}`, { params });
    return response.data;
  },
};

// Worker API endpoints
export const workerApi = {
  // Get worker status
  getStatus: async (): Promise<any> => {
    const response = await api.get('/worker/status');
    return response.data;
  },

  // Start worker service
  start: async (): Promise<{ success: boolean; message: string }> => {
    const response = await api.post('/worker/start');
    return response.data;
  },

  // Stop worker service
  stop: async (): Promise<{ success: boolean; message: string }> => {
    const response = await api.post('/worker/stop');
    return response.data;
  },

  // Restart worker service
  restart: async (): Promise<{ success: boolean; message: string }> => {
    const response = await api.post('/worker/restart');
    return response.data;
  },

  // Get worker logs
  getLogs: async (lines?: number): Promise<string[]> => {
    const response = await api.get('/worker/logs', {
      params: { lines },
    });
    return response.data;
  },
};

export default api;
