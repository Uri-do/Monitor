import React, { ReactElement } from 'react';
import { render, RenderOptions, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { BrowserRouter } from 'react-router-dom';
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

export const renderWithProviders = (
  ui: ReactElement,
  options: CustomRenderOptions = {}
) => {
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
      <BrowserRouter>
        <ThemeProvider theme={theme}>
          <CssBaseline />
          {children}
        </ThemeProvider>
      </BrowserRouter>
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
  return new Promise<T>((resolve) => {
    setTimeout(() => resolve(data), delay);
  });
};

export const mockApiError = (message = 'API Error', status = 500, delay = 0) => {
  return new Promise((_, reject) => {
    setTimeout(() => {
      reject(new Error(message));
    }, delay);
  });
};

// Performance testing utilities
export const measureRenderTime = async (renderFn: () => void) => {
  const start = performance.now();
  renderFn();
  await waitFor(() => {
    // Wait for render to complete
  });
  const end = performance.now();
  return end - start;
};

// Accessibility testing helpers
export const checkAccessibility = async (container: HTMLElement) => {
  const issues: string[] = [];

  // Check for missing alt text on images
  const images = container.querySelectorAll('img');
  images.forEach((img, index) => {
    if (!img.alt && !img.getAttribute('aria-label')) {
      issues.push(`Image ${index + 1} missing alt text`);
    }
  });

  // Check for missing form labels
  const inputs = container.querySelectorAll('input, select, textarea');
  inputs.forEach((input, index) => {
    const hasLabel = input.getAttribute('aria-label') || 
                    input.getAttribute('aria-labelledby') ||
                    container.querySelector(`label[for="${input.id}"]`);
    if (!hasLabel) {
      issues.push(`Form input ${index + 1} missing label`);
    }
  });

  // Check for missing headings hierarchy
  const headings = Array.from(container.querySelectorAll('h1, h2, h3, h4, h5, h6'));
  let lastLevel = 0;
  headings.forEach((heading, index) => {
    const level = parseInt(heading.tagName.charAt(1));
    if (index === 0 && level !== 1) {
      issues.push('Page should start with h1');
    }
    if (level > lastLevel + 1) {
      issues.push(`Heading level skipped: ${heading.tagName} after h${lastLevel}`);
    }
    lastLevel = level;
  });

  // Check for missing focus indicators
  const focusableElements = container.querySelectorAll(
    'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'
  );
  focusableElements.forEach((element, index) => {
    const styles = window.getComputedStyle(element);
    if (styles.outline === 'none' && !styles.boxShadow.includes('focus')) {
      issues.push(`Focusable element ${index + 1} missing focus indicator`);
    }
  });

  return issues;
};

// Data table testing utilities
export const getTableData = (container: HTMLElement) => {
  const table = container.querySelector('table');
  if (!table) return null;

  const headers = Array.from(table.querySelectorAll('thead th')).map(th => th.textContent?.trim());
  const rows = Array.from(table.querySelectorAll('tbody tr')).map(tr => 
    Array.from(tr.querySelectorAll('td')).map(td => td.textContent?.trim())
  );

  return { headers, rows };
};

// Form testing utilities
export const fillForm = async (user: ReturnType<typeof userEvent.setup>, formData: Record<string, string>) => {
  for (const [field, value] of Object.entries(formData)) {
    const input = screen.getByLabelText(new RegExp(field, 'i'));
    await user.clear(input);
    await user.type(input, value);
  }
};

export const submitForm = async (user: ReturnType<typeof userEvent.setup>, submitText = /submit/i) => {
  const submitButton = screen.getByRole('button', { name: submitText });
  await user.click(submitButton);
};

// Dialog testing utilities
export const openDialog = async (user: ReturnType<typeof userEvent.setup>, triggerText: string | RegExp) => {
  const trigger = screen.getByRole('button', { name: triggerText });
  await user.click(trigger);
  return screen.getByRole('dialog');
};

export const closeDialog = async (user: ReturnType<typeof userEvent.setup>) => {
  const closeButton = screen.getByRole('button', { name: /close|cancel/i });
  await user.click(closeButton);
};

// Navigation testing utilities
export const navigateToPage = async (user: ReturnType<typeof userEvent.setup>, linkText: string | RegExp) => {
  const link = screen.getByRole('link', { name: linkText });
  await user.click(link);
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
  global.IntersectionObserver = jest.fn().mockImplementation((callback) => ({
    observe: jest.fn(),
    unobserve: jest.fn(),
    disconnect: jest.fn(),
    root: null,
    rootMargin: '',
    thresholds: [],
  }));
};

export const mockResizeObserver = () => {
  global.ResizeObserver = jest.fn().mockImplementation((callback) => ({
    observe: jest.fn(),
    unobserve: jest.fn(),
    disconnect: jest.fn(),
  }));
};

export const mockMatchMedia = (matches = false) => {
  Object.defineProperty(window, 'matchMedia', {
    writable: true,
    value: jest.fn().mockImplementation((query) => ({
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

// Custom matchers
export const customMatchers = {
  toBeAccessible: async (received: HTMLElement) => {
    const issues = await checkAccessibility(received);
    return {
      pass: issues.length === 0,
      message: () => 
        issues.length === 0 
          ? 'Element is accessible'
          : `Element has accessibility issues: ${issues.join(', ')}`,
    };
  },
};

// Re-export everything from testing-library
export * from '@testing-library/react';
export { userEvent };
