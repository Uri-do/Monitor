/**
 * API Service Layer - Updated for Root-Level API Architecture
 *
 * This file has been updated to work with the new root-level API structure:
 *
 * 🎯 IndicatorController (/api/indicator/*):
 *   - Core Indicator operations (CRUD, execute, metrics, dashboard)
 *   - Alert management (Indicator-related alerts)
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
  // Indicator types (New system)
  IndicatorDto,
  CreateIndicatorRequest,
  UpdateIndicatorRequest,
  TestIndicatorRequest,
  IndicatorExecutionResultDto,
  IndicatorDashboardDto,
  CollectorDto,
  // Scheduler types
  SchedulerDto,
  CreateSchedulerRequest,
  UpdateSchedulerRequest,
  // Contact and Alert types
  ContactDto,
  CreateContactRequest,
  UpdateContactRequest,
  AlertLogDto,
  AlertDashboardDto,
  AlertFilterDto,
  PaginatedAlertsDto,
  AlertStatisticsDto,
  ResolveAlertRequest,
  BulkResolveAlertsRequest,
  BulkContactOperationRequest,
  // System and Health types
  HealthCheckResponse,
  SystemAnalyticsDto,
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
  timeout: 60000, // Increased timeout to 60 seconds for dashboard queries
  headers: {
    'Content-Type': 'application/json',
  },
});

// Request interceptor for authentication
api.interceptors.request.use(
  config => {
    // Add authentication token if available
    const token = sessionStorage.getItem('auth_token');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
      // Log token usage for debugging (only first 20 chars for security)
      console.debug(`API Request with token: ${token.substring(0, 20)}...`);
    } else {
      console.debug('API Request without token (anonymous access)');
    }
    return config;
  },
  error => {
    console.error('API Request interceptor error:', error);
    return Promise.reject(error);
  }
);

// Token refresh state management
let isRefreshing = false;
let failedQueue: Array<{
  resolve: (value?: any) => void;
  reject: (error?: any) => void;
}> = [];

const processQueue = (error: any, token: string | null = null) => {
  failedQueue.forEach(({ resolve, reject }) => {
    if (error) {
      reject(error);
    } else {
      resolve(token);
    }
  });

  failedQueue = [];
};

// Response interceptor for error handling with automatic token refresh
api.interceptors.response.use(
  response => {
    return response;
  },
  async error => {
    const originalRequest = error.config;
    console.error('API Response Error:', error);

    if (error.response?.status === 401 && !originalRequest._retry) {
      if (isRefreshing) {
        // If we're already refreshing, queue this request
        return new Promise((resolve, reject) => {
          failedQueue.push({ resolve, reject });
        })
          .then(token => {
            originalRequest.headers.Authorization = `Bearer ${token}`;
            return api(originalRequest);
          })
          .catch(err => {
            return Promise.reject(err);
          });
      }

      originalRequest._retry = true;
      isRefreshing = true;

      try {
        // Attempt to refresh the token
        const refreshToken = sessionStorage.getItem('refresh_token');
        if (!refreshToken) {
          throw new Error('No refresh token available');
        }

        console.log('Attempting to refresh token...');
        const response = await fetch('/api/auth/refresh', {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
          },
          body: JSON.stringify({ refreshToken }),
        });

        if (!response.ok) {
          throw new Error('Token refresh failed');
        }

        const result = await response.json();
        const newToken = result.token?.accessToken || result.accessToken;

        if (!newToken) {
          throw new Error('No access token in refresh response');
        }

        // Update stored tokens
        sessionStorage.setItem('auth_token', newToken);
        if (result.token?.refreshToken || result.refreshToken) {
          sessionStorage.setItem('refresh_token', result.token?.refreshToken || result.refreshToken);
        }

        // Update the authorization header for the original request
        originalRequest.headers.Authorization = `Bearer ${newToken}`;

        // Process the queue with the new token
        processQueue(null, newToken);

        console.log('Token refresh successful, retrying original request');
        return api(originalRequest);
      } catch (refreshError) {
        console.error('Token refresh failed:', refreshError);

        // Process the queue with the error
        processQueue(refreshError, null);

        // Clear stored tokens
        sessionStorage.removeItem('auth_token');
        sessionStorage.removeItem('refresh_token');

        // Redirect to login page if not already there or in demo mode
        const currentPath = window.location.pathname;
        const isPublicPath =
          currentPath === '/login' ||
          currentPath === '/auth-test' ||
          currentPath.startsWith('/demo');

        if (!isPublicPath) {
          console.log('Redirecting to login due to authentication failure');
          window.location.href = '/login';
        }

        return Promise.reject(refreshError);
      } finally {
        isRefreshing = false;
      }
    } else if (error.response?.status >= 500) {
      // Handle server errors
      console.error('Server error occurred');
    }

    return Promise.reject(error);
  }
);

// Indicator API endpoints (New system replacing KPIs)
export const indicatorApi = {
  // Get all Indicators with optional filtering
  getIndicators: async (params?: {
    isActive?: boolean;
    search?: string;
    page?: number;
    pageSize?: number;
    sortBy?: string;
    sortDirection?: 'asc' | 'desc';
  }): Promise<IndicatorDto[]> => {
    const response: AxiosResponse<IndicatorDto[]> = await api.get('/indicator', { params });
    return response.data;
  },

  // Get Indicator by ID
  getIndicator: async (id: number): Promise<IndicatorDto> => {
    const response: AxiosResponse<any> = await api.get(`/indicator/${id}`);

    let rawData;
    // Handle wrapped API response
    if (response.data?.data) {
      rawData = response.data.data;
    } else {
      // Fallback for direct response
      rawData = response.data;
    }

    // Map API response to frontend-expected format
    const mappedData: IndicatorDto = {
      indicatorID: rawData.indicatorID,
      indicatorName: rawData.indicatorName,
      indicatorCode: rawData.indicatorCode || rawData.indicatorName || 'Unknown', // Fallback to name if code missing
      indicatorDesc: rawData.indicatorDescription || rawData.indicatorDesc,
      collectorID: rawData.collectorId || rawData.collectorID, // API uses collectorId
      collectorName: rawData.collectorName, // API provides collector name
      collectorItemName: rawData.collectorItemName || 'Not specified', // Keep the actual item name
      schedulerID: rawData.schedulerId || rawData.schedulerID, // API uses schedulerId
      isActive: rawData.isActive,
      lastMinutes: rawData.lastMinutes,
      thresholdType: rawData.thresholdType || 'threshold_value', // Default threshold type
      thresholdField: rawData.thresholdField || 'Total', // Default threshold field
      thresholdComparison: rawData.alertOperator || rawData.thresholdComparison || 'gt', // API uses alertOperator
      thresholdValue: rawData.alertThreshold !== undefined ? rawData.alertThreshold : rawData.thresholdValue, // API uses alertThreshold
      priority: rawData.priority || 'medium', // Default priority if not provided
      ownerContactId: rawData.ownerContactId,
      ownerName: rawData.ownerName,
      averageLastDays: rawData.averageLastDays,
      createdDate: rawData.createdDate,
      updatedDate: rawData.updatedDate || rawData.modifiedDate, // API uses modifiedDate
      modifiedDate: rawData.modifiedDate,
      lastRun: rawData.lastRun,
      lastRunResult: rawData.lastRunResult,
      isCurrentlyRunning: rawData.isCurrentlyRunning || false,
      executionStartTime: rawData.executionStartTime,
      executionContext: rawData.executionContext,
      ownerContact: rawData.ownerContact,
      contacts: rawData.contacts || [], // Default to empty array if not provided
      scheduler: rawData.scheduler
    };

    // Debug log to see what we're mapping
    console.log('API Raw Data:', rawData);
    console.log('Mapped Data:', mappedData);

    return mappedData;
  },

  // Create new Indicator
  createIndicator: async (indicator: CreateIndicatorRequest): Promise<IndicatorDto> => {
    const response: AxiosResponse<IndicatorDto> = await api.post('/indicator', indicator);
    return response.data;
  },

  // Update Indicator
  updateIndicator: async (indicator: UpdateIndicatorRequest): Promise<IndicatorDto> => {
    const response: AxiosResponse<IndicatorDto> = await api.put(
      `/indicator/${indicator.indicatorID}`,
      indicator
    );
    return response.data;
  },

  // Delete Indicator
  deleteIndicator: async (id: number): Promise<void> => {
    await api.delete(`/indicator/${id}`);
  },

  // Execute Indicator manually
  executeIndicator: async (request: TestIndicatorRequest): Promise<IndicatorExecutionResultDto> => {
    const response: AxiosResponse<IndicatorExecutionResultDto> = await api.post(
      `/indicator/${request.indicatorID}/execute`,
      request
    );
    return response.data;
  },

  // Get Indicator dashboard data
  getDashboard: async (): Promise<IndicatorDashboardDto> => {
    const response: AxiosResponse<IndicatorDashboardDto> = await api.get('/indicator/dashboard');
    return response.data;
  },
};

// Collector API endpoints (for ProgressPlayDB integration)
export const collectorApi = {
  // Get all collectors
  getCollectors: async (): Promise<CollectorDto[]> => {
    const response: AxiosResponse<CollectorDto[]> = await api.get('/collector');
    return response.data;
  },

  // Get collector item names for a specific collector
  getCollectorItemNames: async (collectorId: number): Promise<string[]> => {
    const response: AxiosResponse<string[]> = await api.get(`/collector/${collectorId}/items`);
    return response.data;
  },
};

// Monitor Statistics API endpoints (New system using ProgressPlayDBTest)
export const monitorStatisticsApi = {
  // Get active collectors
  getActiveCollectors: async (): Promise<any[]> => {
    const response: AxiosResponse<{ isSuccess: boolean; data: any[] }> = await api.get(
      '/monitorstatistics/collectors?activeOnly=true'
    );
    const collectors = response.data.data || [];

    // Ensure collectors is an array before mapping
    const collectorsArray = Array.isArray(collectors) ? collectors : [];
    // Transform the data to match the expected format
    return collectorsArray.map((collector: any) => ({
      id: collector.collectorID,
      collectorID: collector.collectorID,
      collectorCode: collector.collectorCode,
      collectorDesc: collector.collectorDesc,
      displayName: collector.collectorCode || `Collector ${collector.collectorID}`,
      frequencyMinutes: collector.frequencyMinutes || 0,
      lastMinutes: collector.lastMinutes,
      storeProcedure: collector.storeProcedure,
      isActive: collector.isActive,
      isActiveStatus: collector.isActive || false,
      updatedDate: collector.updatedDate,
      lastRun: collector.lastRun,
      lastRunResult: collector.lastRunResult,
      frequencyDisplay: `${collector.frequencyMinutes || 0} minutes`,
      lastRunDisplay: collector.lastRun ? new Date(collector.lastRun).toLocaleString() : 'Never',
      statusDisplay: collector.isActive ? 'Active' : 'Inactive',
      statisticsCount: 0,
      itemNames: []
    }));
  },

  // Get all collectors
  getAllCollectors: async (): Promise<any[]> => {
    const response: AxiosResponse<{ isSuccess: boolean; data: any[] }> = await api.get(
      '/monitorstatistics/collectors?activeOnly=false'
    );
    const collectors = response.data.data || [];

    // Ensure collectors is an array before mapping
    const collectorsArray = Array.isArray(collectors) ? collectors : [];
    // Transform the data to match the expected format
    return collectorsArray.map((collector: any) => ({
      id: collector.collectorID,
      collectorID: collector.collectorID,
      collectorCode: collector.collectorCode,
      collectorDesc: collector.collectorDesc,
      displayName: collector.collectorCode || `Collector ${collector.collectorID}`,
      frequencyMinutes: collector.frequencyMinutes || 0,
      lastMinutes: collector.lastMinutes,
      storeProcedure: collector.storeProcedure,
      isActive: collector.isActive,
      isActiveStatus: collector.isActive || false,
      updatedDate: collector.updatedDate,
      lastRun: collector.lastRun,
      lastRunResult: collector.lastRunResult,
      frequencyDisplay: `${collector.frequencyMinutes || 0} minutes`,
      lastRunDisplay: collector.lastRun ? new Date(collector.lastRun).toLocaleString() : 'Never',
      statusDisplay: collector.isActive ? 'Active' : 'Inactive',
      statisticsCount: 0,
      itemNames: []
    }));
  },

  // Get collector by ID
  getCollector: async (collectorId: number): Promise<any> => {
    const response: AxiosResponse<{ isSuccess: boolean; data: any }> = await api.get(
      `/monitorstatistics/collectors/${collectorId}`
    );
    const collector = response.data.data;

    if (!collector) return null;

    // Transform the data to match the expected format
    return {
      id: collector.collectorID,
      collectorID: collector.collectorID,
      collectorCode: collector.collectorCode,
      collectorDesc: collector.collectorDesc,
      displayName: collector.collectorCode || `Collector ${collector.collectorID}`,
      frequencyMinutes: collector.frequencyMinutes || 0,
      lastMinutes: collector.lastMinutes,
      storeProcedure: collector.storeProcedure,
      isActive: collector.isActive,
      isActiveStatus: collector.isActive || false,
      updatedDate: collector.updatedDate,
      lastRun: collector.lastRun,
      lastRunResult: collector.lastRunResult,
      frequencyDisplay: `${collector.frequencyMinutes || 0} minutes`,
      lastRunDisplay: collector.lastRun ? new Date(collector.lastRun).toLocaleString() : 'Never',
      statusDisplay: collector.isActive ? 'Active' : 'Inactive',
      statisticsCount: 0,
      itemNames: []
    };
  },

  // Get collector item names
  getCollectorItemNames: async (collectorId: number): Promise<string[]> => {
    const response: AxiosResponse<{ isSuccess: boolean; data: string[] }> = await api.get(
      `/monitorstatistics/collectors/${collectorId}/items`
    );
    return response.data.data || [];
  },

  // Get collector statistics
  getCollectorStatistics: async (
    collectorId: number,
    options?: {
      fromDate?: string;
      toDate?: string;
      hours?: number;
    }
  ): Promise<any[]> => {
    const params = new URLSearchParams();
    if (options?.fromDate && options?.toDate) {
      params.append('fromDate', options.fromDate);
      params.append('toDate', options.toDate);
    } else if (options?.hours) {
      params.append('hours', options.hours.toString());
    }

    const url = `/monitorstatistics/collectors/${collectorId}/statistics${params.toString() ? `?${params.toString()}` : ''}`;
    const response: AxiosResponse<{ isSuccess: boolean; data: any[] }> = await api.get(url);
    return response.data.data || [];
  },
};

// Scheduler API endpoints
export const schedulerApi = {
  // Get all schedulers
  getSchedulers: async (includeDisabled: boolean = false): Promise<SchedulerDto[]> => {
    try {
      const response: AxiosResponse<any> = await api.get('/schedulers', {
        params: { includeDisabled }
      });

      // Handle wrapped API response
      if (response.data?.data?.schedulers) {
        return response.data.data.schedulers || [];
      }

      // Fallback for direct array response
      if (Array.isArray(response.data)) {
        return response.data;
      }

      // Fallback for data property containing array
      if (Array.isArray(response.data?.data)) {
        return response.data.data;
      }

      console.warn('Unexpected scheduler API response format:', response.data);
      return [];
    } catch (error: any) {
      console.error('Failed to fetch schedulers:', error);
      // Return empty array for any error (including 401 Unauthorized)
      return [];
    }
  },

  // Get scheduler by ID
  getScheduler: async (id: number): Promise<SchedulerDto> => {
    const response: AxiosResponse<any> = await api.get(`/schedulers/${id}`);

    // Handle wrapped API response
    if (response.data?.data) {
      return response.data.data;
    }

    // Fallback for direct response
    return response.data;
  },

  // Create new scheduler
  createScheduler: async (scheduler: CreateSchedulerRequest): Promise<SchedulerDto> => {
    const response: AxiosResponse<SchedulerDto> = await api.post('/schedulers', scheduler);
    return response.data;
  },

  // Update scheduler
  updateScheduler: async (scheduler: UpdateSchedulerRequest): Promise<SchedulerDto> => {
    const response: AxiosResponse<SchedulerDto> = await api.put(
      `/schedulers/${scheduler.schedulerID}`,
      scheduler
    );
    return response.data;
  },

  // Delete scheduler
  deleteScheduler: async (id: number): Promise<void> => {
    await api.delete(`/schedulers/${id}`);
  },

  // Enable/disable scheduler
  toggleScheduler: async (id: number, enabled: boolean): Promise<SchedulerDto> => {
    const response: AxiosResponse<SchedulerDto> = await api.patch(`/schedulers/${id}/toggle`, {
      enabled,
    });
    return response.data;
  },

  // Get indicators with their scheduler information
  getIndicatorsWithSchedulers: async (): Promise<any[]> => {
    try {
      const response: AxiosResponse<any[]> = await api.get('/schedulers/indicators');
      return response.data || [];
    } catch (error: any) {
      console.error('Failed to fetch indicators with schedulers:', error);
      return [];
    }
  },

  // Get indicators assigned to a specific scheduler
  getIndicatorsByScheduler: async (id: number): Promise<any[]> => {
    try {
      const response: AxiosResponse<any[]> = await api.get(`/schedulers/${id}/indicators`);
      return response.data || [];
    } catch (error: any) {
      console.error('Failed to fetch indicators by scheduler:', error);
      return [];
    }
  },

  // Get indicators that are due for execution
  getDueIndicators: async (): Promise<any[]> => {
    try {
      const response: AxiosResponse<any[]> = await api.get('/schedulers/due-indicators');
      return response.data || [];
    } catch (error: any) {
      console.error('Failed to fetch due indicators:', error);
      return [];
    }
  },

  // Get upcoming executions for the next specified hours
  getUpcomingExecutions: async (hours: number = 24): Promise<any[]> => {
    try {
      const response: AxiosResponse<any[]> = await api.get('/schedulers/upcoming', {
        params: { hours }
      });
      return response.data || [];
    } catch (error: any) {
      console.error('Failed to fetch upcoming executions:', error);
      return [];
    }
  },
};

// Legacy KPI API endpoints have been migrated to Indicator API
// Use indicatorApi instead for all new development

// Contact API endpoints (now under Indicator controller)
export const contactApi = {
  // Get all contacts
  getContacts: async (params?: { isActive?: boolean; search?: string }): Promise<ContactDto[]> => {
    // Updated to use Indicator controller's contact endpoints
    const response: AxiosResponse<ContactDto[]> = await api.get('/indicator/contacts', { params });
    return response.data;
  },

  // Get contact by ID
  getContact: async (id: number): Promise<ContactDto> => {
    // Updated to use Indicator controller's contact endpoints
    const response: AxiosResponse<ContactDto> = await api.get(`/indicator/contacts/${id}`);
    return response.data;
  },

  // Create new contact
  createContact: async (contact: CreateContactRequest): Promise<ContactDto> => {
    // Updated to use Indicator controller's contact endpoints
    const response: AxiosResponse<ContactDto> = await api.post('/indicator/contacts', contact);
    return response.data;
  },

  // Update contact
  updateContact: async (contact: UpdateContactRequest): Promise<ContactDto> => {
    // Updated to use Indicator controller's contact endpoints
    const response: AxiosResponse<ContactDto> = await api.put(
      `/indicator/contacts/${contact.contactID}`,
      contact
    );
    return response.data;
  },

  // Delete contact
  deleteContact: async (id: number): Promise<void> => {
    // Updated to use Indicator controller's contact endpoints
    await api.delete(`/indicator/contacts/${id}`);
  },

  // Assign contact to Indicators
  assignToIndicators: async (id: number, indicatorIDs: number[]): Promise<{ message: string }> => {
    // Updated to use Indicator controller's contact endpoints
    const response: AxiosResponse<{ message: string }> = await api.post(
      `/indicator/contacts/${id}/assign`,
      {
        contactID: id,
        indicatorIDs,
      }
    );
    return response.data;
  },

  // Bulk operations
  bulkOperation: async (request: BulkContactOperationRequest): Promise<{ message: string }> => {
    // Updated to use Indicator controller's contact endpoints
    const response: AxiosResponse<{ message: string }> = await api.post(
      '/indicator/contacts/bulk',
      request
    );
    return response.data;
  },
};

// Alert API endpoints (using dedicated AlertController)
export const alertApi = {
  // Get alerts with filtering and pagination
  getAlerts: async (filter: AlertFilterDto): Promise<PaginatedAlertsDto> => {
    try {
      const response: AxiosResponse<PaginatedAlertsDto> = await api.get('/alert', {
        params: filter,
      });
      return response.data;
    } catch (error: any) {
      if (error.response?.status === 503) {
        // Service temporarily unavailable - return empty result
        return {
          alerts: [],
          totalCount: 0,
          page: filter.page || 1,
          pageSize: filter.pageSize || 20,
          totalPages: 0,
          hasNextPage: false,
          hasPreviousPage: false,
        };
      }
      throw error;
    }
  },

  // Get alert by ID
  getAlert: async (id: number): Promise<AlertLogDto> => {
    try {
      const response: AxiosResponse<AlertLogDto> = await api.get(`/alert/${id}`);
      return response.data;
    } catch (error: any) {
      if (error.response?.status === 503) {
        throw new Error('Alert service temporarily unavailable. Please try again.');
      }
      throw error;
    }
  },

  // Resolve alert
  resolveAlert: async (request: ResolveAlertRequest): Promise<{ message: string }> => {
    try {
      const response: AxiosResponse<{ message: string }> = await api.post(
        `/alert/${request.alertId}/resolve`,
        request
      );
      return response.data;
    } catch (error: any) {
      if (error.response?.status === 503) {
        throw new Error('Alert service temporarily unavailable. Please try again.');
      }
      throw error;
    }
  },

  // Bulk resolve alerts
  bulkResolveAlerts: async (request: BulkResolveAlertsRequest): Promise<{ message: string }> => {
    try {
      const response: AxiosResponse<{ message: string }> = await api.post(
        '/alert/bulk-resolve',
        request
      );
      return response.data;
    } catch (error: any) {
      if (error.response?.status === 503) {
        throw new Error('Alert service temporarily unavailable. Please try again.');
      }
      throw error;
    }
  },

  // Get alert statistics
  getStatistics: async (days: number = 30): Promise<AlertStatisticsDto> => {
    // Return empty statistics until implemented
    return {
      totalAlerts: 0,
      resolvedAlerts: 0,
      unresolvedAlerts: 0,
      criticalAlerts: 0,
      averageResolutionTimeHours: 0,
      dailyTrend: [],
      topAlertingKpis: [],
      alertsToday: 0,
      alertsThisWeek: 0,
      alertsThisMonth: 0,
      highPriorityAlerts: 0,
      successRate: 0,
      avgResponseTime: 0,
      trendData: [],
      severityDistribution: [],
    };
  },

  // Get alert dashboard
  getDashboard: async (): Promise<AlertDashboardDto> => {
    try {
      const response: AxiosResponse<AlertDashboardDto> = await api.get('/alert/dashboard');
      return response.data;
    } catch (error: any) {
      if (error.response?.status === 503) {
        // Return empty dashboard when service unavailable
        return {
          totalAlertsToday: 0,
          unresolvedAlerts: 0,
          criticalAlerts: 0,
          alertsLastHour: 0,
          alertTrendPercentage: 0,
          recentAlerts: [],
          topAlertingKpis: [],
          hourlyTrend: [],
        };
      }
      // Return empty dashboard for other errors too
      return {
        totalAlertsToday: 0,
        unresolvedAlerts: 0,
        criticalAlerts: 0,
        alertsLastHour: 0,
        alertTrendPercentage: 0,
        recentAlerts: [],
        topAlertingKpis: [],
        hourlyTrend: [],
      };
    }
  },

  // Get critical alerts requiring immediate attention
  getCriticalAlerts: async (): Promise<EnhancedAlertDto[]> => {
    try {
      // Return empty array until critical alerts endpoint is implemented
      return [];
    } catch (error: any) {
      if (error.response?.status === 503) {
        return [];
      }
      throw error;
    }
  },

  // Get unresolved alerts
  getUnresolvedAlerts: async (): Promise<EnhancedAlertDto[]> => {
    try {
      // Return empty array until unresolved alerts endpoint is implemented
      return [];
    } catch (error: any) {
      if (error.response?.status === 503) {
        return [];
      }
      throw error;
    }
  },

  // Send manual alert
  sendManualAlert: async (
    request: ManualAlertRequest
  ): Promise<{ message: string; alertId: number }> => {
    try {
      // Return mock response until manual alert endpoint is implemented
      return {
        message: 'Manual alert functionality not yet implemented',
        alertId: 0,
      };
    } catch (error: any) {
      if (error.response?.status === 503) {
        throw new Error('Alert service temporarily unavailable. Please try again.');
      }
      throw error;
    }
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

  // Get system analytics
  getAnalytics: async (timeRange: string): Promise<any> => {
    // Return mock data until implemented
    return {
      totalRequests: 0,
      averageResponseTime: 0,
      errorRate: 0,
      uptime: '100%',
    };
  },

  // Get system settings
  getSettings: async (): Promise<any> => {
    // Return mock data until implemented
    return {
      maintenanceMode: false,
      debugMode: false,
      logLevel: 'info',
    };
  },

  // Get dashboard overview
  getDashboardOverview: async (): Promise<any> => {
    // Return mock data until implemented
    return {
      totalIndicators: 0,
      activeIndicators: 0,
      totalAlerts: 0,
      systemHealth: 'healthy',
    };
  },

  // Get dashboard metrics
  getDashboardMetrics: async (timeRange: string): Promise<any> => {
    // Return mock data until implemented
    return {
      metrics: [],
      timeRange,
    };
  },

  // Get realtime dashboard
  getRealtimeDashboard: async (): Promise<any> => {
    // Return mock data until implemented
    return {
      liveMetrics: [],
      connections: 0,
      lastUpdate: new Date().toISOString(),
    };
  },
};

// Execution History API endpoints (now under Indicator controller)
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
    search?: string;
    indicatorId?: number;
  }): Promise<PaginatedExecutionHistoryDto> => {
    // Updated to use Indicator controller's execution history endpoints
    const response = await api.get('/indicator/execution-history', { params });

    // Handle wrapped API response (ApiResponse<T> structure)
    if (response.data?.data) {
      return response.data.data;
    }

    // Fallback for direct response
    return response.data;
  },

  // Get execution statistics
  getExecutionStats: async (params: {
    kpiId?: number;
    days?: number;
  }): Promise<ExecutionStatsDto[]> => {
    // Updated to use Indicator controller's execution history endpoints
    const response = await api.get('/indicator/execution-stats', { params });

    // Handle wrapped API response (ApiResponse<T> structure)
    if (response.data?.data) {
      return response.data.data;
    }

    // Fallback for direct response
    return response.data;
  },

  // Get detailed execution information
  getExecutionDetail: async (historicalId: number): Promise<ExecutionHistoryDetailDto> => {
    // Updated to use Indicator controller's execution history endpoints
    const response = await api.get(`/indicator/execution-history/${historicalId}`);

    // Handle wrapped API response (ApiResponse<T> structure)
    if (response.data?.data) {
      return response.data.data;
    }

    // Fallback for direct response
    return response.data;
  },

  // Test database connection and recent records
  testDatabaseConnection: async (): Promise<any> => {
    // Updated to use KPI controller's execution history endpoints
    const response = await api.get('/kpi/execution-history/test');
    return response.data;
  },
};

// Analytics API endpoints (now under KPI controller)
export const analyticsApi = {
  // Get system-wide analytics
  getSystemAnalytics: async (days: number = 30): Promise<SystemAnalyticsDto> => {
    // Updated to use Indicator controller's analytics endpoints
    const response: AxiosResponse<SystemAnalyticsDto> = await api.get('/indicator/analytics/system', {
      params: { days },
    });
    return response.data;
  },

  // Get Indicator performance analytics
  getIndicatorPerformanceAnalytics: async (id: number, days: number = 30): Promise<any> => {
    // Updated to use Indicator controller's analytics endpoints
    const response: AxiosResponse<any> = await api.get(`/indicator/${id}/analytics`, {
      params: { days },
    });
    return response.data;
  },

  // Get owner-based analytics
  getOwnerAnalytics: async (days: number = 30): Promise<OwnerAnalyticsDto[]> => {
    // Updated to use Indicator controller's analytics endpoints
    const response: AxiosResponse<OwnerAnalyticsDto[]> = await api.get(
      '/indicator/analytics/owners',
      {
        params: { days },
      }
    );
    return response.data;
  },

  // Get real-time system health
  getSystemHealth: async (): Promise<SystemHealthDto> => {
    // Updated to use Indicator controller's analytics endpoints
    const response: AxiosResponse<SystemHealthDto> = await api.get('/indicator/health');
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

  // Execute Indicator in real-time
  executeIndicatorRealtime: async (id: number): Promise<IndicatorExecutionResultDto> => {
    const response: AxiosResponse<IndicatorExecutionResultDto> = await api.post(
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
  login: async (credentials: {
    username: string;
    password: string;
  }): Promise<{
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

  getUserSecurityEvents: async (
    userId: string,
    params?: {
      startDate?: string;
      endDate?: string;
    }
  ): Promise<any[]> => {
    const response = await api.get(`/security/events/user/${userId}`, { params });
    return response.data;
  },
};

// Worker API endpoints
export const workerApi = {
  // Get worker status with detailed information
  getStatus: async (includeDetails: boolean = true, includeMetrics: boolean = true, includeHistory: boolean = false): Promise<any> => {
    try {
      const response = await api.get('/worker/status', {
        params: { includeDetails, includeMetrics, includeHistory }
      });

      // Handle wrapped API response
      if (response.data?.data) {
        return response.data.data;
      }

      // Fallback for direct response
      return response.data;
    } catch (error: any) {
      console.error('Failed to fetch worker status:', error);
      return null;
    }
  },

  // Start worker service
  start: async (): Promise<{ success: boolean; message: string }> => {
    const response = await api.post('/worker/start');
    return response.data?.data || response.data;
  },

  // Stop worker service
  stop: async (): Promise<{ success: boolean; message: string }> => {
    const response = await api.post('/worker/stop');
    return response.data?.data || response.data;
  },

  // Restart worker service
  restart: async (): Promise<{ success: boolean; message: string }> => {
    const response = await api.post('/worker/restart');
    return response.data?.data || response.data;
  },

  // Restart API (for integrated worker services)
  restartApi: async (): Promise<{ success: boolean; message: string }> => {
    const response = await api.post('/worker/restart-api');
    return response.data?.data || response.data;
  },

  // Force stop all worker processes (emergency cleanup)
  forceStop: async (): Promise<{ success: boolean; message: string }> => {
    const response = await api.post('/worker/force-stop');
    return response.data?.data || response.data;
  },

  // Get debug information about indicators
  getDebugIndicators: async (): Promise<any> => {
    try {
      const response = await api.get('/worker/debug-indicators');
      return response.data?.data || response.data;
    } catch (error: any) {
      console.error('Failed to fetch debug indicators:', error);
      return null;
    }
  },

  // Get cleanup status
  getCleanupStatus: async (): Promise<any> => {
    try {
      const response = await api.get('/worker/cleanup-status');
      return response.data?.data || response.data;
    } catch (error: any) {
      console.error('Failed to fetch cleanup status:', error);
      return null;
    }
  },

  // Execute a specific indicator
  executeIndicator: async (indicatorId: number): Promise<any> => {
    try {
      const response = await api.post(`/worker/execute-indicator/${indicatorId}`);
      return response.data?.data || response.data;
    } catch (error: any) {
      console.error('Failed to execute indicator:', error);
      throw error;
    }
  },

  // Execute all due indicators
  executeDueIndicators: async (): Promise<any> => {
    try {
      const response = await api.post('/worker/execute-due-indicators');
      return response.data?.data || response.data;
    } catch (error: any) {
      console.error('Failed to execute due indicators:', error);
      throw error;
    }
  },

  // Test worker logic
  testWorkerLogic: async (): Promise<any> => {
    try {
      const response = await api.post('/worker/test-worker-logic');
      return response.data?.data || response.data;
    } catch (error: any) {
      console.error('Failed to test worker logic:', error);
      throw error;
    }
  },

  // Cleanup workers
  cleanupWorkers: async (): Promise<any> => {
    try {
      const response = await api.post('/worker/cleanup-workers');
      return response.data?.data || response.data;
    } catch (error: any) {
      console.error('Failed to cleanup workers:', error);
      throw error;
    }
  },
};

export default api;
