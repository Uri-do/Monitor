/**
 * Advanced Testing Framework
 * Enterprise-grade testing utilities and patterns
 */

import { render, screen, waitFor, fireEvent, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { BrowserRouter } from 'react-router-dom';
import { ThemeProvider } from '@mui/material/styles';
import { CssBaseline } from '@mui/material';
import { I18nextProvider } from 'react-i18next';
import i18n from '@/i18n';
import { theme } from '@/theme';

// Advanced test configuration
export interface TestConfig {
  withRouter?: boolean;
  withQuery?: boolean;
  withTheme?: boolean;
  withI18n?: boolean;
  initialRoute?: string;
  queryClient?: QueryClient;
  customWrappers?: React.ComponentType<{ children: React.ReactNode }>[];
}

// Performance testing utilities
export class PerformanceTestUtils {
  private static measurements: Map<string, number[]> = new Map();

  static startMeasurement(name: string): () => number {
    const startTime = performance.now();
    
    return () => {
      const endTime = performance.now();
      const duration = endTime - startTime;
      
      if (!this.measurements.has(name)) {
        this.measurements.set(name, []);
      }
      this.measurements.get(name)!.push(duration);
      
      return duration;
    };
  }

  static getAverageDuration(name: string): number {
    const measurements = this.measurements.get(name) || [];
    if (measurements.length === 0) return 0;
    
    return measurements.reduce((sum, duration) => sum + duration, 0) / measurements.length;
  }

  static expectPerformance(name: string, maxDuration: number): void {
    const avgDuration = this.getAverageDuration(name);
    if (avgDuration > maxDuration) {
      throw new Error(
        `Performance test failed: ${name} took ${avgDuration.toFixed(2)}ms (max: ${maxDuration}ms)`
      );
    }
  }

  static clearMeasurements(): void {
    this.measurements.clear();
  }
}

// Accessibility testing utilities
export class AccessibilityTestUtils {
  static async expectNoAccessibilityViolations(container: HTMLElement): Promise<void> {
    // This would integrate with axe-core in a real implementation
    const violations = await this.runAxeCheck(container);
    
    if (violations.length > 0) {
      const violationMessages = violations.map(v => `${v.id}: ${v.description}`).join('\n');
      throw new Error(`Accessibility violations found:\n${violationMessages}`);
    }
  }

  private static async runAxeCheck(container: HTMLElement): Promise<any[]> {
    // Mock implementation - in real scenario, use axe-core
    return [];
  }

  static expectProperHeadingStructure(container: HTMLElement): void {
    const headings = container.querySelectorAll('h1, h2, h3, h4, h5, h6');
    let previousLevel = 0;
    
    headings.forEach((heading) => {
      const currentLevel = parseInt(heading.tagName.charAt(1));
      
      if (currentLevel > previousLevel + 1) {
        throw new Error(
          `Heading structure violation: Found h${currentLevel} after h${previousLevel}`
        );
      }
      
      previousLevel = currentLevel;
    });
  }

  static expectAriaLabels(container: HTMLElement, selectors: string[]): void {
    selectors.forEach(selector => {
      const elements = container.querySelectorAll(selector);
      elements.forEach(element => {
        const hasAriaLabel = element.hasAttribute('aria-label') || 
                           element.hasAttribute('aria-labelledby') ||
                           element.hasAttribute('aria-describedby');
        
        if (!hasAriaLabel) {
          throw new Error(`Element ${selector} missing aria-label or aria-labelledby`);
        }
      });
    });
  }
}

// Advanced component testing utilities
export class ComponentTestUtils {
  static createTestWrapper(config: TestConfig = {}): React.ComponentType<{ children: React.ReactNode }> {
    const {
      withRouter = true,
      withQuery = true,
      withTheme = true,
      withI18n = true,
      initialRoute = '/',
      queryClient = new QueryClient({
        defaultOptions: {
          queries: { retry: false },
          mutations: { retry: false },
        },
      }),
      customWrappers = [],
    } = config;

    return ({ children }) => {
      let wrappedChildren = children;

      // Apply custom wrappers
      customWrappers.forEach(Wrapper => {
        wrappedChildren = <Wrapper>{wrappedChildren}</Wrapper>;
      });

      // Apply theme
      if (withTheme) {
        wrappedChildren = (
          <ThemeProvider theme={theme}>
            <CssBaseline />
            {wrappedChildren}
          </ThemeProvider>
        );
      }

      // Apply i18n
      if (withI18n) {
        wrappedChildren = (
          <I18nextProvider i18n={i18n}>
            {wrappedChildren}
          </I18nextProvider>
        );
      }

      // Apply query client
      if (withQuery) {
        wrappedChildren = (
          <QueryClientProvider client={queryClient}>
            {wrappedChildren}
          </QueryClientProvider>
        );
      }

      // Apply router
      if (withRouter) {
        window.history.pushState({}, 'Test page', initialRoute);
        wrappedChildren = (
          <BrowserRouter>
            {wrappedChildren}
          </BrowserRouter>
        );
      }

      return <>{wrappedChildren}</>;
    };
  }

  static renderWithProviders(
    component: React.ReactElement,
    config?: TestConfig
  ) {
    const Wrapper = this.createTestWrapper(config);
    return render(component, { wrapper: Wrapper });
  }

  static async waitForLoadingToFinish(): Promise<void> {
    await waitFor(() => {
      expect(screen.queryByTestId('loading-spinner')).not.toBeInTheDocument();
    });
  }

  static async expectAsyncError(asyncFn: () => Promise<any>, expectedError?: string): Promise<void> {
    try {
      await asyncFn();
      throw new Error('Expected function to throw an error');
    } catch (error) {
      if (expectedError && error instanceof Error) {
        expect(error.message).toContain(expectedError);
      }
    }
  }
}

// Mock data generators
export class MockDataGenerator {
  static generateUser(overrides: Partial<any> = {}): any {
    return {
      id: Math.random().toString(36).substr(2, 9),
      email: `user${Math.floor(Math.random() * 1000)}@example.com`,
      name: `Test User ${Math.floor(Math.random() * 1000)}`,
      roles: ['user'],
      permissions: ['read'],
      isActive: true,
      createdAt: new Date().toISOString(),
      ...overrides,
    };
  }

  static generateIndicator(overrides: Partial<any> = {}): any {
    return {
      id: Math.random().toString(36).substr(2, 9),
      name: `Test Indicator ${Math.floor(Math.random() * 1000)}`,
      description: 'Test indicator description',
      query: 'SELECT COUNT(*) FROM test_table',
      isActive: true,
      lastExecuted: new Date().toISOString(),
      ...overrides,
    };
  }

  static generateApiResponse<T>(data: T, overrides: Partial<any> = {}): any {
    return {
      success: true,
      data,
      message: 'Success',
      timestamp: new Date().toISOString(),
      ...overrides,
    };
  }

  static generatePaginatedResponse<T>(
    items: T[],
    page = 1,
    pageSize = 10,
    totalCount?: number
  ): any {
    const total = totalCount ?? items.length;
    const totalPages = Math.ceil(total / pageSize);
    
    return {
      items,
      page,
      pageSize,
      totalCount: total,
      totalPages,
      hasNextPage: page < totalPages,
      hasPreviousPage: page > 1,
    };
  }
}

// Integration test utilities
export class IntegrationTestUtils {
  static async simulateUserFlow(steps: Array<() => Promise<void> | void>): Promise<void> {
    for (const step of steps) {
      await step();
      // Small delay between steps to simulate real user behavior
      await new Promise(resolve => setTimeout(resolve, 100));
    }
  }

  static async fillForm(formData: Record<string, string>): Promise<void> {
    const user = userEvent.setup();
    
    for (const [fieldName, value] of Object.entries(formData)) {
      const field = screen.getByLabelText(new RegExp(fieldName, 'i'));
      await user.clear(field);
      await user.type(field, value);
    }
  }

  static async submitForm(submitButtonText = 'Submit'): Promise<void> {
    const user = userEvent.setup();
    const submitButton = screen.getByRole('button', { name: new RegExp(submitButtonText, 'i') });
    await user.click(submitButton);
  }

  static expectFormValidation(fieldName: string, errorMessage: string): void {
    const errorElement = screen.getByText(new RegExp(errorMessage, 'i'));
    expect(errorElement).toBeInTheDocument();
  }
}

// Test data cleanup utilities
export class TestCleanupUtils {
  private static cleanupFunctions: Array<() => void | Promise<void>> = [];

  static addCleanup(cleanupFn: () => void | Promise<void>): void {
    this.cleanupFunctions.push(cleanupFn);
  }

  static async runCleanup(): Promise<void> {
    for (const cleanupFn of this.cleanupFunctions) {
      await cleanupFn();
    }
    this.cleanupFunctions = [];
  }

  static clearLocalStorage(): void {
    localStorage.clear();
  }

  static clearSessionStorage(): void {
    sessionStorage.clear();
  }

  static resetMocks(): void {
    jest.clearAllMocks();
    jest.resetAllMocks();
  }
}

// Custom matchers for enhanced testing
export const customMatchers = {
  toBeAccessible: async (received: HTMLElement) => {
    try {
      await AccessibilityTestUtils.expectNoAccessibilityViolations(received);
      return {
        message: () => 'Element is accessible',
        pass: true,
      };
    } catch (error) {
      return {
        message: () => `Element is not accessible: ${error}`,
        pass: false,
      };
    }
  },

  toHavePerformantRender: (received: string, maxDuration: number) => {
    try {
      PerformanceTestUtils.expectPerformance(received, maxDuration);
      return {
        message: () => `Render performance is within acceptable limits`,
        pass: true,
      };
    } catch (error) {
      return {
        message: () => `Render performance exceeded limits: ${error}`,
        pass: false,
      };
    }
  },
};

// Export all utilities
export {
  PerformanceTestUtils,
  AccessibilityTestUtils,
  ComponentTestUtils,
  MockDataGenerator,
  IntegrationTestUtils,
  TestCleanupUtils,
};
