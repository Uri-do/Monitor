# Testing Guide

This document provides comprehensive information about the testing setup and practices for the MonitoringGrid Frontend application.

## Testing Stack

- **Test Runner**: [Vitest](https://vitest.dev/) - Fast unit test framework
- **Testing Library**: [@testing-library/react](https://testing-library.com/docs/react-testing-library/intro/) - Simple and complete testing utilities
- **Mocking**: [Vitest Mock Functions](https://vitest.dev/api/vi.html) - Built-in mocking capabilities
- **Coverage**: [V8 Coverage](https://vitest.dev/guide/coverage.html) - Native code coverage

## Test Structure

```
src/
├── test/                           # Test utilities and setup
│   ├── setup.ts                   # Global test setup
│   ├── utils.tsx                  # Custom render functions and utilities
│   ├── i18n-mock.ts              # Mock i18n configuration
│   ├── mocks/                     # Mock implementations
│   │   └── api.ts                 # API service mocks
│   ├── integration/               # Integration tests
│   └── e2e/                       # End-to-end test placeholders
├── components/                     # Component tests
│   └── **/__tests__/              # Component-specific tests
├── hooks/                          # Hook tests
│   └── **/__tests__/              # Hook-specific tests
├── pages/                          # Page component tests
│   └── **/__tests__/              # Page-specific tests
├── services/                       # Service tests
│   └── **/__tests__/              # Service-specific tests
└── utils/                          # Utility tests
    └── **/__tests__/              # Utility-specific tests
```

## Running Tests

### Basic Commands

```bash
# Run all tests once
npm run test

# Run tests in watch mode
npm run test:watch

# Run tests with UI
npm run test:ui

# Run tests with coverage
npm run test:coverage

# Run tests with coverage UI
npm run test:coverage:ui
```

### Specific Test Categories

```bash
# Run only unit tests
npm run test:unit

# Run only integration tests
npm run test:integration

# Run only component tests
npm run test:components

# Run only hook tests
npm run test:hooks

# Run only service tests
npm run test:services

# Run only utility tests
npm run test:utils

# Run only page tests
npm run test:pages

# Run tests for CI/CD
npm run test:ci
```

## Writing Tests

### Component Tests

```typescript
import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@/test/utils';
import { MyComponent } from '../MyComponent';

describe('MyComponent', () => {
  it('renders correctly', () => {
    render(<MyComponent />);
    expect(screen.getByText('Expected Text')).toBeInTheDocument();
  });

  it('handles user interactions', () => {
    const handleClick = vi.fn();
    render(<MyComponent onClick={handleClick} />);
    
    fireEvent.click(screen.getByRole('button'));
    expect(handleClick).toHaveBeenCalled();
  });
});
```

### Hook Tests

```typescript
import { describe, it, expect } from 'vitest';
import { renderHook, waitFor } from '@testing-library/react';
import { AllTheProviders } from '@/test/utils';
import { useMyHook } from '../useMyHook';

describe('useMyHook', () => {
  it('returns expected data', async () => {
    const wrapper = ({ children }: { children: React.ReactNode }) => (
      <AllTheProviders>{children}</AllTheProviders>
    );

    const { result } = renderHook(() => useMyHook(), { wrapper });

    await waitFor(() => {
      expect(result.current.data).toBeDefined();
    });
  });
});
```

### Service Tests

```typescript
import { describe, it, expect, vi } from 'vitest';
import { myService } from '../myService';

// Mock dependencies
vi.mock('axios');

describe('myService', () => {
  it('fetches data correctly', async () => {
    const mockData = { id: 1, name: 'Test' };
    vi.mocked(axios.get).mockResolvedValue({ data: mockData });

    const result = await myService.getData();
    expect(result).toEqual(mockData);
  });
});
```

## Test Utilities

### Custom Render Function

The `render` function from `@/test/utils` provides all necessary providers:

- QueryClient for TanStack Query
- Router for React Router
- Theme Provider for Material-UI
- i18n Provider for internationalization
- Mock Auth Provider

### Mock Data Factories

Use the provided factory functions to create consistent test data:

```typescript
import { createMockIndicator, createMockUser } from '@/test/utils';

const indicator = createMockIndicator({ name: 'Custom Name' });
const user = createMockUser({ username: 'testuser' });
```

### API Mocking

Use the centralized API mocks:

```typescript
import { mockApiService, resetApiMocks, makeApiCallFail } from '@/test/mocks/api';

beforeEach(() => {
  resetApiMocks();
});

it('handles API errors', async () => {
  makeApiCallFail('getIndicators', new Error('Network Error'));
  // Test error handling
});
```

## Best Practices

### 1. Test Structure
- Use descriptive test names
- Group related tests with `describe` blocks
- Use `beforeEach` and `afterEach` for setup/cleanup

### 2. Assertions
- Use specific matchers (`toBeInTheDocument`, `toHaveClass`, etc.)
- Test behavior, not implementation details
- Verify both positive and negative cases

### 3. Mocking
- Mock external dependencies
- Use factory functions for consistent test data
- Reset mocks between tests

### 4. Async Testing
- Use `waitFor` for async operations
- Test loading states
- Test error states

### 5. Coverage Goals
- Aim for 80%+ code coverage
- Focus on critical business logic
- Don't sacrifice quality for coverage numbers

## Coverage Reports

Coverage reports are generated in the `coverage/` directory:

- `coverage/index.html` - Interactive HTML report
- `coverage/lcov.info` - LCOV format for CI/CD
- `coverage/coverage-final.json` - JSON format

## Continuous Integration

The test suite is designed to run in CI/CD environments:

```bash
# CI command with coverage and verbose output
npm run test:ci
```

## Debugging Tests

### VS Code Integration
- Install the Vitest extension
- Use breakpoints in test files
- Run individual tests from the editor

### Browser Debugging
```bash
# Run tests with UI for visual debugging
npm run test:ui
```

### Console Debugging
```typescript
import { screen } from '@testing-library/react';

// Debug rendered output
screen.debug();

// Debug specific element
screen.debug(screen.getByRole('button'));
```

## Common Patterns

### Testing Forms
```typescript
it('submits form with valid data', async () => {
  const handleSubmit = vi.fn();
  render(<MyForm onSubmit={handleSubmit} />);

  fireEvent.change(screen.getByLabelText(/name/i), {
    target: { value: 'Test Name' }
  });

  fireEvent.click(screen.getByRole('button', { name: /submit/i }));

  await waitFor(() => {
    expect(handleSubmit).toHaveBeenCalledWith({
      name: 'Test Name'
    });
  });
});
```

### Testing Navigation
```typescript
it('navigates to correct page', () => {
  const mockNavigate = vi.fn();
  vi.mocked(useNavigate).mockReturnValue(mockNavigate);

  render(<MyComponent />);
  fireEvent.click(screen.getByRole('link'));

  expect(mockNavigate).toHaveBeenCalledWith('/expected-path');
});
```

### Testing Error Boundaries
```typescript
it('handles errors gracefully', () => {
  const ThrowError = () => {
    throw new Error('Test error');
  };

  render(
    <ErrorBoundary>
      <ThrowError />
    </ErrorBoundary>
  );

  expect(screen.getByText(/something went wrong/i)).toBeInTheDocument();
});
```

## Troubleshooting

### Common Issues

1. **Tests timing out**: Increase timeout or check for unresolved promises
2. **Mock not working**: Ensure mock is set up before component render
3. **DOM not updating**: Use `waitFor` for async updates
4. **Coverage not accurate**: Check for untested branches or files

### Getting Help

- Check the [Vitest documentation](https://vitest.dev/)
- Review [Testing Library best practices](https://testing-library.com/docs/guiding-principles)
- Look at existing tests for patterns and examples
