import { vi } from 'vitest';
import { createMockIndicator, createMockUser, createMockAlert, createMockExecutionHistory } from '../utils';

// Mock API responses
export const mockApiResponses = {
  // Indicators
  indicators: {
    getAll: vi.fn().mockResolvedValue([
      createMockIndicator({ id: 1, name: 'Indicator 1' }),
      createMockIndicator({ id: 2, name: 'Indicator 2' }),
    ]),
    getById: vi.fn().mockResolvedValue(createMockIndicator()),
    create: vi.fn().mockResolvedValue(createMockIndicator()),
    update: vi.fn().mockResolvedValue(createMockIndicator()),
    delete: vi.fn().mockResolvedValue(undefined),
  },

  // Users
  users: {
    getAll: vi.fn().mockResolvedValue([
      createMockUser({ id: 1, username: 'user1' }),
      createMockUser({ id: 2, username: 'user2' }),
    ]),
    getById: vi.fn().mockResolvedValue(createMockUser()),
    create: vi.fn().mockResolvedValue(createMockUser()),
    update: vi.fn().mockResolvedValue(createMockUser()),
    delete: vi.fn().mockResolvedValue(undefined),
  },

  // Alerts
  alerts: {
    getAll: vi.fn().mockResolvedValue([
      createMockAlert({ id: 1, title: 'Alert 1' }),
      createMockAlert({ id: 2, title: 'Alert 2' }),
    ]),
    getById: vi.fn().mockResolvedValue(createMockAlert()),
    resolve: vi.fn().mockResolvedValue(createMockAlert({ isResolved: true })),
  },

  // Execution History
  executionHistory: {
    getAll: vi.fn().mockResolvedValue([
      createMockExecutionHistory({ id: 1 }),
      createMockExecutionHistory({ id: 2 }),
    ]),
    getById: vi.fn().mockResolvedValue(createMockExecutionHistory()),
  },

  // Auth
  auth: {
    login: vi.fn().mockResolvedValue({
      token: 'mock-token',
      user: createMockUser(),
    }),
    logout: vi.fn().mockResolvedValue(undefined),
    register: vi.fn().mockResolvedValue({
      token: 'mock-token',
      user: createMockUser(),
    }),
    getCurrentUser: vi.fn().mockResolvedValue(createMockUser()),
  },
};

// Mock the entire API service
export const mockApiService = {
  // Indicators
  getIndicators: mockApiResponses.indicators.getAll,
  getIndicator: mockApiResponses.indicators.getById,
  createIndicator: mockApiResponses.indicators.create,
  updateIndicator: mockApiResponses.indicators.update,
  deleteIndicator: mockApiResponses.indicators.delete,

  // Users
  getUsers: mockApiResponses.users.getAll,
  getUser: mockApiResponses.users.getById,
  createUser: mockApiResponses.users.create,
  updateUser: mockApiResponses.users.update,
  deleteUser: mockApiResponses.users.delete,

  // Alerts
  getAlerts: mockApiResponses.alerts.getAll,
  getAlert: mockApiResponses.alerts.getById,
  resolveAlert: mockApiResponses.alerts.resolve,

  // Execution History
  getExecutionHistory: mockApiResponses.executionHistory.getAll,
  getExecutionHistoryItem: mockApiResponses.executionHistory.getById,

  // Auth
  login: mockApiResponses.auth.login,
  logout: mockApiResponses.auth.logout,
  register: mockApiResponses.auth.register,
  getCurrentUser: mockApiResponses.auth.getCurrentUser,
};

// Helper to reset all mocks
export const resetApiMocks = () => {
  Object.values(mockApiResponses).forEach(category => {
    Object.values(category).forEach(mock => {
      if (vi.isMockFunction(mock)) {
        mock.mockClear();
      }
    });
  });
};

// Helper to make API calls fail
export const makeApiCallFail = (method: keyof typeof mockApiService, error = new Error('API Error')) => {
  const mock = mockApiService[method] as any;
  if (vi.isMockFunction(mock)) {
    mock.mockRejectedValueOnce(error);
  }
};

// Helper to make API calls succeed with custom data
export const makeApiCallSucceed = (method: keyof typeof mockApiService, data: any) => {
  const mock = mockApiService[method] as any;
  if (vi.isMockFunction(mock)) {
    mock.mockResolvedValueOnce(data);
  }
};
