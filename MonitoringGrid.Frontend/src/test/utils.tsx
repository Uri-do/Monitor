import React, { ReactElement } from 'react';
import { render, RenderOptions } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { BrowserRouter } from 'react-router-dom';
import { ThemeProvider, createTheme } from '@mui/material/styles';
import { CssBaseline } from '@mui/material';
import { I18nextProvider } from 'react-i18next';
import i18n from './i18n-mock';

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

// Mock auth context
const MockAuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  return <div data-testid="mock-auth-provider">{children}</div>;
};

// All the providers wrapper
interface AllTheProvidersProps {
  children: React.ReactNode;
  queryClient?: QueryClient;
  initialEntries?: string[];
}

const AllTheProviders: React.FC<AllTheProvidersProps> = ({
  children,
  queryClient = createTestQueryClient(),
  initialEntries = ['/'],
}) => {
  return (
    <QueryClientProvider client={queryClient}>
      <I18nextProvider i18n={i18n}>
        <ThemeProvider theme={testTheme}>
          <CssBaseline />
          <BrowserRouter>
            <MockAuthProvider>{children}</MockAuthProvider>
          </BrowserRouter>
        </ThemeProvider>
      </I18nextProvider>
    </QueryClientProvider>
  );
};

// Custom render function
interface CustomRenderOptions extends Omit<RenderOptions, 'wrapper'> {
  queryClient?: QueryClient;
  initialEntries?: string[];
}

const customRender = (
  ui: ReactElement,
  {
    queryClient = createTestQueryClient(),
    initialEntries = ['/'],
    ...renderOptions
  }: CustomRenderOptions = {}
) => {
  const Wrapper: React.FC<{ children: React.ReactNode }> = ({ children }) => (
    <AllTheProviders queryClient={queryClient} initialEntries={initialEntries}>
      {children}
    </AllTheProviders>
  );

  return render(ui, { wrapper: Wrapper, ...renderOptions });
};

// Mock data factories
export const createMockIndicator = (overrides = {}) => ({
  id: 1,
  name: 'Test Indicator',
  description: 'Test Description',
  isActive: true,
  lastMinutes: 60,
  createdAt: '2024-01-01T00:00:00Z',
  updatedAt: '2024-01-01T00:00:00Z',
  ...overrides,
});

export const createMockUser = (overrides = {}) => ({
  id: 1,
  username: 'testuser',
  email: 'test@example.com',
  firstName: 'Test',
  lastName: 'User',
  isActive: true,
  roles: ['User'],
  ...overrides,
});

export const createMockAlert = (overrides = {}) => ({
  id: 1,
  title: 'Test Alert',
  message: 'Test alert message',
  severity: 'warning' as const,
  isResolved: false,
  createdAt: '2024-01-01T00:00:00Z',
  ...overrides,
});

export const createMockExecutionHistory = (overrides = {}) => ({
  id: 1,
  indicatorId: 1,
  executedAt: '2024-01-01T00:00:00Z',
  status: 'Success' as const,
  duration: 1000,
  result: 'Test result',
  ...overrides,
});

// Wait utilities
export const waitForLoadingToFinish = () =>
  new Promise(resolve => setTimeout(resolve, 0));

// Re-export everything
export * from '@testing-library/react';
export { customRender as render };
export { createTestQueryClient };
export { AllTheProviders };
