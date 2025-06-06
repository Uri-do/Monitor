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
  HealthCheckResponse
} from '@/types/api';
import {
  mockKpis,
  mockContacts,
  mockAlerts,
  mockKpiDashboard,
  mockAlertDashboard,
  mockHealthCheck
} from './mockData';

// Enable mock mode for development (set to false when backend is ready)
const USE_MOCK_DATA = false;

// Create axios instance with base configuration
const api = axios.create({
  baseURL: USE_MOCK_DATA ? '/api' : 'https://localhost:57652/api',
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Mock delay function to simulate API calls
const mockDelay = (ms: number = 500) => new Promise(resolve => setTimeout(resolve, ms));

// Request interceptor for logging
api.interceptors.request.use(
  (config) => {
    console.log(`API Request: ${config.method?.toUpperCase()} ${config.url}`);
    return config;
  },
  (error) => {
    console.error('API Request Error:', error);
    return Promise.reject(error);
  }
);

// Response interceptor for error handling
api.interceptors.response.use(
  (response) => {
    return response;
  },
  (error) => {
    console.error('API Response Error:', error);
    
    if (error.response?.status === 401) {
      // Handle unauthorized access
      console.error('Unauthorized access');
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
    if (USE_MOCK_DATA) {
      await mockDelay();
      let filteredKpis = [...mockKpis];
      if (params?.isActive !== undefined) {
        filteredKpis = filteredKpis.filter(kpi => kpi.isActive === params.isActive);
      }
      if (params?.owner) {
        filteredKpis = filteredKpis.filter(kpi => kpi.owner.toLowerCase().includes(params.owner!.toLowerCase()));
      }
      if (params?.priority) {
        filteredKpis = filteredKpis.filter(kpi => kpi.priority === params.priority);
      }
      return filteredKpis;
    }
    const response: AxiosResponse<KpiDto[]> = await api.get('/kpi', { params });
    return response.data;
  },

  // Get KPI by ID
  getKpi: async (id: number): Promise<KpiDto> => {
    if (USE_MOCK_DATA) {
      await mockDelay();
      const kpi = mockKpis.find(k => k.kpiId === id);
      if (!kpi) throw new Error(`KPI with ID ${id} not found`);
      return kpi;
    }
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
    const response: AxiosResponse<KpiExecutionResultDto> = await api.post(`/kpi/${request.kpiId}/execute`, request);
    return response.data;
  },

  // Test KPI (alias for executeKpi for backward compatibility)
  testKpi: async (request: TestKpiRequest): Promise<KpiExecutionResultDto> => {
    const response: AxiosResponse<KpiExecutionResultDto> = await api.post(`/kpi/${request.kpiId}/execute`, request);
    return response.data;
  },

  // Get KPI metrics
  getKpiMetrics: async (id: number, days: number = 30): Promise<KpiMetricsDto> => {
    const response: AxiosResponse<KpiMetricsDto> = await api.get(`/kpi/${id}/metrics`, {
      params: { days }
    });
    return response.data;
  },

  // Get KPI dashboard
  getDashboard: async (): Promise<KpiDashboardDto> => {
    if (USE_MOCK_DATA) {
      await mockDelay();
      return mockKpiDashboard;
    }
    const response: AxiosResponse<KpiDashboardDto> = await api.get('/kpi/dashboard');
    return response.data;
  },



  // Bulk operations
  bulkOperation: async (request: BulkKpiOperationRequest): Promise<{ message: string }> => {
    const response: AxiosResponse<{ message: string }> = await api.post('/kpi/bulk', request);
    return response.data;
  },
};

// Contact API endpoints
export const contactApi = {
  // Get all contacts
  getContacts: async (params?: {
    isActive?: boolean;
    search?: string;
  }): Promise<ContactDto[]> => {
    if (USE_MOCK_DATA) {
      await mockDelay();
      let filteredContacts = [...mockContacts];
      if (params?.isActive !== undefined) {
        filteredContacts = filteredContacts.filter(contact => contact.isActive === params.isActive);
      }
      if (params?.search) {
        const searchLower = params.search.toLowerCase();
        filteredContacts = filteredContacts.filter(contact =>
          contact.name.toLowerCase().includes(searchLower) ||
          contact.email?.toLowerCase().includes(searchLower) ||
          contact.phone?.toLowerCase().includes(searchLower)
        );
      }
      return filteredContacts;
    }
    const response: AxiosResponse<ContactDto[]> = await api.get('/contact', { params });
    return response.data;
  },

  // Get contact by ID
  getContact: async (id: number): Promise<ContactDto> => {
    if (USE_MOCK_DATA) {
      await mockDelay();
      const contact = mockContacts.find(c => c.contactId === id);
      if (!contact) throw new Error(`Contact with ID ${id} not found`);
      return contact;
    }
    const response: AxiosResponse<ContactDto> = await api.get(`/contact/${id}`);
    return response.data;
  },

  // Create new contact
  createContact: async (contact: CreateContactRequest): Promise<ContactDto> => {
    const response: AxiosResponse<ContactDto> = await api.post('/contact', contact);
    return response.data;
  },

  // Update contact
  updateContact: async (contact: UpdateContactRequest): Promise<ContactDto> => {
    const response: AxiosResponse<ContactDto> = await api.put(`/contact/${contact.contactId}`, contact);
    return response.data;
  },

  // Delete contact
  deleteContact: async (id: number): Promise<void> => {
    await api.delete(`/contact/${id}`);
  },

  // Assign contact to KPIs
  assignToKpis: async (id: number, kpiIds: number[]): Promise<{ message: string }> => {
    const response: AxiosResponse<{ message: string }> = await api.post(`/contact/${id}/assign`, {
      contactId: id,
      kpiIds
    });
    return response.data;
  },

  // Bulk operations
  bulkOperation: async (request: BulkContactOperationRequest): Promise<{ message: string }> => {
    const response: AxiosResponse<{ message: string }> = await api.post('/contact/bulk', request);
    return response.data;
  },
};

// Alert API endpoints
export const alertApi = {
  // Get alerts with filtering and pagination
  getAlerts: async (filter: AlertFilterDto): Promise<PaginatedAlertsDto> => {
    if (USE_MOCK_DATA) {
      await mockDelay();
      let filteredAlerts = [...mockAlerts];

      if (filter.isResolved !== undefined) {
        filteredAlerts = filteredAlerts.filter(alert => alert.isResolved === filter.isResolved);
      }

      if (filter.searchText) {
        const searchLower = filter.searchText.toLowerCase();
        filteredAlerts = filteredAlerts.filter(alert =>
          alert.kpiIndicator.toLowerCase().includes(searchLower) ||
          alert.message.toLowerCase().includes(searchLower) ||
          alert.severity.toLowerCase().includes(searchLower)
        );
      }

      return {
        alerts: filteredAlerts,
        totalCount: filteredAlerts.length,
        page: filter.page,
        pageSize: filter.pageSize,
        totalPages: Math.ceil(filteredAlerts.length / filter.pageSize),
        hasNextPage: false,
        hasPreviousPage: false,
      };
    }
    const response: AxiosResponse<PaginatedAlertsDto> = await api.get('/alert', {
      params: filter
    });
    return response.data;
  },

  // Get alert by ID
  getAlert: async (id: number): Promise<AlertLogDto> => {
    if (USE_MOCK_DATA) {
      await mockDelay();
      const alert = mockAlerts.find(a => a.alertId === id);
      if (!alert) throw new Error(`Alert with ID ${id} not found`);
      return alert;
    }
    const response: AxiosResponse<AlertLogDto> = await api.get(`/alert/${id}`);
    return response.data;
  },

  // Resolve alert
  resolveAlert: async (request: ResolveAlertRequest): Promise<{ message: string }> => {
    if (USE_MOCK_DATA) {
      await mockDelay();
      const alertIndex = mockAlerts.findIndex(a => a.alertId === request.alertId);
      if (alertIndex !== -1) {
        mockAlerts[alertIndex] = {
          ...mockAlerts[alertIndex],
          isResolved: true,
          resolvedTime: new Date().toISOString(),
          resolvedBy: request.resolvedBy,
        };
      }
      return { message: 'Alert resolved successfully' };
    }
    const response: AxiosResponse<{ message: string }> = await api.post(`/alert/${request.alertId}/resolve`, request);
    return response.data;
  },

  // Bulk resolve alerts
  bulkResolveAlerts: async (request: BulkResolveAlertsRequest): Promise<{ message: string }> => {
    if (USE_MOCK_DATA) {
      await mockDelay();
      request.alertIds.forEach(alertId => {
        const alertIndex = mockAlerts.findIndex(a => a.alertId === alertId);
        if (alertIndex !== -1) {
          mockAlerts[alertIndex] = {
            ...mockAlerts[alertIndex],
            isResolved: true,
            resolvedTime: new Date().toISOString(),
            resolvedBy: request.resolvedBy,
          };
        }
      });
      return { message: `${request.alertIds.length} alerts resolved successfully` };
    }
    const response: AxiosResponse<{ message: string }> = await api.post('/alert/resolve-bulk', request);
    return response.data;
  },

  // Get alert statistics
  getStatistics: async (days: number = 30): Promise<AlertStatisticsDto> => {
    const response: AxiosResponse<AlertStatisticsDto> = await api.get('/alert/statistics', {
      params: { days }
    });
    return response.data;
  },

  // Get alert dashboard
  getDashboard: async (): Promise<AlertDashboardDto> => {
    if (USE_MOCK_DATA) {
      await mockDelay();
      return mockAlertDashboard;
    }
    const response: AxiosResponse<AlertDashboardDto> = await api.get('/alert/dashboard');
    return response.data;
  },
};

// System API endpoints
export const systemApi = {
  // Get health check
  getHealth: async (): Promise<HealthCheckResponse> => {
    if (USE_MOCK_DATA) {
      await mockDelay();
      return mockHealthCheck;
    }
    const healthApi = axios.create({
      baseURL: 'https://localhost:57652',
      timeout: 30000,
    });
    const response: AxiosResponse<HealthCheckResponse> = await healthApi.get('/health');
    return response.data;
  },

  // Get API info
  getInfo: async (): Promise<{
    name: string;
    version: string;
    environment: string;
    timestamp: string;
  }> => {
    const infoApi = axios.create({
      baseURL: 'https://localhost:57652',
      timeout: 30000,
    });
    const response = await infoApi.get('/api/info');
    return response.data;
  },
};

// Execution History API endpoints
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
    const response: AxiosResponse<PaginatedExecutionHistoryDto> = await api.get('/executionhistory', { params });
    return response.data;
  },

  // Get execution statistics
  getExecutionStats: async (params: {
    kpiId?: number;
    days?: number;
  }): Promise<ExecutionStatsDto[]> => {
    const response: AxiosResponse<ExecutionStatsDto[]> = await api.get('/executionhistory/stats', { params });
    return response.data;
  },

  // Get detailed execution information
  getExecutionDetail: async (historicalId: number): Promise<ExecutionHistoryDetailDto> => {
    const response: AxiosResponse<ExecutionHistoryDetailDto> = await api.get(`/executionhistory/${historicalId}`);
    return response.data;
  },
};

export default api;
