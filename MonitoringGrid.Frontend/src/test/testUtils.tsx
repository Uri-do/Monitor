import React, { ReactElement } from 'react';
import { render, RenderOptions, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { ThemeProvider } from '@mui/material/styles';
import { CssBaseline } from '@mui/material';
import { theme } from '@/theme/theme';
import { useAppStore } from '@/stores/appStore';

// Mock data generators
export const mockKpi = (overrides = {}) => ({
  kpiId: 1,
  indicator: 'Test KPI',
  description: 'Test KPI Description',
  owner: 'Test Owner',
  spName: 'test_sp',
  frequency: 'Hourly',
  isActive: true,
  lastRun: new Date().toISOString(),
  nextRun: new Date(Date.now() + 3600000).toISOString(),
  threshold: 100,
  alertOnDeviation: true,
  deviationPercentage: 10,
  createdAt: new Date().toISOString(),
  updatedAt: new Date().toISOString(),
  ...overrides,
});

export const mockAlert = (overrides = {}) => ({
  alertId: 1,
  kpiId: 1,
  indicator: 'Test KPI',
  message: 'Test alert message',
  severity: 'High' as const,
  isAcknowledged: false,
  acknowledgedBy: null,
  acknowledgedAt: null,
  createdAt: new Date().toISOString(),
  currentValue: 150,
  threshold: 100,
  deviationPercent: 50,
  ...overrides,
});

export const mockExecutionHistory = (overrides = {}) => ({
  historicalId: 1,
  kpiId: 1,
  indicator: 'Test KPI',
  kpiOwner: 'Test Owner',
  spName: 'test_sp',
  executedBy: 'System',
  executionMethod: 'Scheduled',
  timestamp: new Date().toISOString(),
  currentValue: 100,
  historicalValue: 90,
  deviationPercent: 11.11,
  isSuccessful: true,
  executionTimeMs: 250,
  performanceCategory: 'Normal',
  shouldAlert: false,
  alertSent: false,
  deviationCategory: 'Normal',
  ...overrides,
});

export const mockUser = (overrides = {}) => ({
  userId: 1,
  username: 'testuser',
  email: 'test@example.com',
  firstName: 'Test',
  lastName: 'User',
  isActive: true,
  roles: ['User'],
  createdAt: new Date().toISOString(),
  lastLogin: new Date().toISOString(),
  ...overrides,
});

// Custom render function with providers
interface CustomRenderOptions extends Omit<RenderOptions, 'wrapper'> {
  initialEntries?: string[];
  queryClient?: QueryClient;
  preloadedState?: any;
}

export const renderWithProviders = (ui: ReactElement, options: CustomRenderOptions = {}) => {
  const {
    initialEntries = ['/'],
    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false },
        mutations: { retry: false },
      },
    }),
    preloadedState,
    ...renderOptions
  } = options;

  // Reset store state if provided
  if (preloadedState) {
    useAppStore.setState(preloadedState);
  }

  const Wrapper = ({ children }: { children: React.ReactNode }) => (
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={initialEntries}>
        <ThemeProvider theme={theme}>
          <CssBaseline />
          {children}
        </ThemeProvider>
      </MemoryRouter>
    </QueryClientProvider>
  );

  return {
    user: userEvent.setup(),
    ...render(ui, { wrapper: Wrapper, ...renderOptions }),
  };
};

// Custom hooks testing utilities
export const createTestQueryClient = () => {
  return new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
        cacheTime: 0,
      },
      mutations: {
        retry: false,
      },
    },
  });
};

// Mock API responses
export const mockApiResponse = <T,>(data: T, delay = 0) => {
  return new Promise<T>(resolve => {
    setTimeout(() => resolve(data), delay);
  });
};

export const mockApiError = (message = 'API Error', _status = 500, delay = 0) => {
  return new Promise((_, reject) => {
    setTimeout(() => {
      reject(new Error(message));
    }, delay);
  });
};

// Form testing utilities
export const fillForm = async (
  user: ReturnType<typeof userEvent.setup>,
  formData: Record<string, string>
) => {
  for (const [field, value] of Object.entries(formData)) {
    const input = screen.getByLabelText(new RegExp(field, 'i'));
    await user.clear(input);
    await user.type(input, value);
  }
};

export const submitForm = async (
  user: ReturnType<typeof userEvent.setup>,
  submitText = /submit/i
) => {
  const submitButton = screen.getByRole('button', { name: submitText });
  await user.click(submitButton);
};

// Wait utilities
export const waitForLoadingToFinish = async () => {
  await waitFor(() => {
    expect(screen.queryByText(/loading/i)).not.toBeInTheDocument();
  });
};

export const waitForErrorToAppear = async (errorText?: string | RegExp) => {
  await waitFor(() => {
    if (errorText) {
      expect(screen.getByText(errorText)).toBeInTheDocument();
    } else {
      expect(screen.getByText(/error/i)).toBeInTheDocument();
    }
  });
};

// Mock implementations
export const mockIntersectionObserver = () => {
  global.IntersectionObserver = jest.fn().mockImplementation((_callback: any) => ({
    observe: jest.fn(),
    unobserve: jest.fn(),
    disconnect: jest.fn(),
    root: null,
    rootMargin: '',
    thresholds: [],
  }));
};

export const mockResizeObserver = () => {
  global.ResizeObserver = jest.fn().mockImplementation((_callback: any) => ({
    observe: jest.fn(),
    unobserve: jest.fn(),
    disconnect: jest.fn(),
  }));
};

export const mockMatchMedia = (matches = false) => {
  Object.defineProperty(window, 'matchMedia', {
    writable: true,
    value: jest.fn().mockImplementation(query => ({
      matches,
      media: query,
      onchange: null,
      addListener: jest.fn(),
      removeListener: jest.fn(),
      addEventListener: jest.fn(),
      removeEventListener: jest.fn(),
      dispatchEvent: jest.fn(),
    })),
  });
};

// Re-export everything from testing-library
export * from '@testing-library/react';
export { userEvent };
