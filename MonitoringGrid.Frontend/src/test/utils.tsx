import React, { ReactElement } from 'react';
import { render, RenderOptions } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { BrowserRouter } from 'react-router-dom';
import { ThemeProvider, createTheme } from '@mui/material/styles';
import { CssBaseline } from '@mui/material';

// Create a test theme
const testTheme = createTheme({
  palette: {
    mode: 'light',
  },
});

// Create a test query client
const createTestQueryClient = () =>
  new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
        gcTime: 0,
      },
      mutations: {
        retry: false,
      },
    },
  });

interface AllTheProvidersProps {
  children: React.ReactNode;
}

const AllTheProviders: React.FC<AllTheProvidersProps> = ({ children }) => {
  const queryClient = createTestQueryClient();

  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <ThemeProvider theme={testTheme}>
          <CssBaseline />
          {children}
        </ThemeProvider>
      </BrowserRouter>
    </QueryClientProvider>
  );
};

const customRender = (
  ui: ReactElement,
  options?: Omit<RenderOptions, 'wrapper'>
) => render(ui, { wrapper: AllTheProviders, ...options });

// Mock user for testing
export const mockUser = {
  userId: 'test-user-id',
  username: 'testuser',
  email: 'test@example.com',
  firstName: 'Test',
  lastName: 'User',
  isActive: true,
  roles: ['User'],
  permissions: ['KPI:Read', 'Alert:Read', 'Contact:Read'],
  createdAt: new Date().toISOString(),
  updatedAt: new Date().toISOString(),
};

// Mock KPI data
export const mockKpi = {
  kpiId: 1,
  indicator: 'Test KPI',
  description: 'Test KPI Description',
  query: 'SELECT COUNT(*) FROM test_table',
  expectedValue: 100,
  tolerance: 10,
  frequency: 60,
  isActive: true,
  priority: 1,
  owner: 'test@example.com',
  lastExecuted: new Date().toISOString(),
  lastResult: 95,
  status: 'Healthy' as const,
  createdAt: new Date().toISOString(),
  updatedAt: new Date().toISOString(),
};

// Mock Alert data
export const mockAlert = {
  alertId: 1,
  kpiId: 1,
  kpiName: 'Test KPI',
  message: 'Test alert message',
  severity: 'Warning' as const,
  triggerTime: new Date().toISOString(),
  isResolved: false,
  resolvedAt: null,
  resolvedBy: null,
  resolution: null,
  sentVia: 'Email',
  recipientCount: 1,
  createdAt: new Date().toISOString(),
  updatedAt: new Date().toISOString(),
};

// Mock Contact data
export const mockContact = {
  contactId: 1,
  name: 'Test Contact',
  email: 'contact@example.com',
  phone: '+1234567890',
  isActive: true,
  createdAt: new Date().toISOString(),
  updatedAt: new Date().toISOString(),
};

// Helper function to wait for async operations
export const waitForLoadingToFinish = () =>
  new Promise(resolve => setTimeout(resolve, 0));

// Helper function to create mock API responses
export const createMockApiResponse = <T,>(data: T) => ({
  data,
  status: 200,
  statusText: 'OK',
  headers: {},
  config: {},
});

// Helper function to create mock paginated response
export const createMockPaginatedResponse = <T,>(
  items: T[],
  page = 1,
  pageSize = 10
) => ({
  items,
  totalCount: items.length,
  page,
  pageSize,
  totalPages: Math.ceil(items.length / pageSize),
});

// Re-export everything from testing-library
export * from '@testing-library/react';
export { customRender as render };
