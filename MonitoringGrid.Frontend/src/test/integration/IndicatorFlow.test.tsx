import { describe, it, expect, beforeEach, vi } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@/test/utils';
import { BrowserRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { createMockIndicator } from '@/test/utils';
import App from '@/App';

// Mock the API service
vi.mock('@/services/api', () => ({
  indicatorService: {
    getIndicators: vi.fn(),
    getIndicator: vi.fn(),
    createIndicator: vi.fn(),
    updateIndicator: vi.fn(),
    deleteIndicator: vi.fn(),
  },
}));

// Mock auth hook
vi.mock('@/hooks/useAuth', () => ({
  AuthProvider: ({ children }: { children: React.ReactNode }) => <div>{children}</div>,
  useAuth: () => ({
    user: { id: 1, username: 'testuser', roles: ['Admin'] },
    isAuthenticated: true,
    login: vi.fn(),
    logout: vi.fn(),
  }),
}));

// Mock other providers
vi.mock('@/contexts/RealtimeContext', () => ({
  RealtimeProvider: ({ children }: { children: React.ReactNode }) => <div>{children}</div>,
}));

vi.mock('@/hooks/useTheme', () => ({
  CustomThemeProvider: ({ children }: { children: React.ReactNode }) => <div>{children}</div>,
}));

describe('Indicator Management Flow Integration Tests', () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false },
        mutations: { retry: false },
      },
    });
    vi.clearAllMocks();
  });

  it('completes full indicator CRUD flow', async () => {
    const mockIndicators = [
      createMockIndicator({ id: 1, name: 'Existing Indicator' }),
    ];

    const { indicatorService } = await import('@/services/api');
    
    // Mock API responses
    vi.mocked(indicatorService.getIndicators).mockResolvedValue(mockIndicators);
    vi.mocked(indicatorService.getIndicator).mockResolvedValue(mockIndicators[0]);
    vi.mocked(indicatorService.createIndicator).mockResolvedValue(
      createMockIndicator({ id: 2, name: 'New Indicator' })
    );
    vi.mocked(indicatorService.updateIndicator).mockResolvedValue(
      createMockIndicator({ id: 1, name: 'Updated Indicator' })
    );
    vi.mocked(indicatorService.deleteIndicator).mockResolvedValue(undefined);

    // Start with the app
    render(
      <QueryClientProvider client={queryClient}>
        <BrowserRouter>
          <App />
        </BrowserRouter>
      </QueryClientProvider>
    );

    // Should redirect to dashboard initially
    await waitFor(() => {
      expect(window.location.pathname).toBe('/dashboard');
    });

    // Navigate to indicators
    const indicatorsLink = screen.getByText(/indicators/i);
    fireEvent.click(indicatorsLink);

    await waitFor(() => {
      expect(window.location.pathname).toBe('/indicators');
    });

    // Should see existing indicator
    await waitFor(() => {
      expect(screen.getByText('Existing Indicator')).toBeInTheDocument();
    });

    // Create new indicator
    const createButton = screen.getByRole('button', { name: /create/i });
    fireEvent.click(createButton);

    await waitFor(() => {
      expect(window.location.pathname).toBe('/indicators/create');
    });

    // Fill out form
    const nameInput = screen.getByLabelText(/name/i);
    const descriptionInput = screen.getByLabelText(/description/i);
    
    fireEvent.change(nameInput, { target: { value: 'New Indicator' } });
    fireEvent.change(descriptionInput, { target: { value: 'Test description' } });

    // Submit form
    const submitButton = screen.getByRole('button', { name: /save/i });
    fireEvent.click(submitButton);

    await waitFor(() => {
      expect(indicatorService.createIndicator).toHaveBeenCalledWith({
        name: 'New Indicator',
        description: 'Test description',
        isActive: true,
      });
    });

    // Should redirect back to list
    await waitFor(() => {
      expect(window.location.pathname).toBe('/indicators');
    });

    // View indicator details
    const indicatorLink = screen.getByText('Existing Indicator');
    fireEvent.click(indicatorLink);

    await waitFor(() => {
      expect(window.location.pathname).toBe('/indicators/1');
    });

    await waitFor(() => {
      expect(indicatorService.getIndicator).toHaveBeenCalledWith(1);
    });

    // Edit indicator
    const editButton = screen.getByRole('button', { name: /edit/i });
    fireEvent.click(editButton);

    await waitFor(() => {
      expect(window.location.pathname).toBe('/indicators/1/edit');
    });

    // Update name
    const editNameInput = screen.getByDisplayValue('Existing Indicator');
    fireEvent.change(editNameInput, { target: { value: 'Updated Indicator' } });

    // Submit update
    const updateButton = screen.getByRole('button', { name: /save/i });
    fireEvent.click(updateButton);

    await waitFor(() => {
      expect(indicatorService.updateIndicator).toHaveBeenCalledWith({
        id: 1,
        name: 'Updated Indicator',
        description: expect.any(String),
        isActive: expect.any(Boolean),
      });
    });

    // Should redirect back to detail view
    await waitFor(() => {
      expect(window.location.pathname).toBe('/indicators/1');
    });

    // Go back to list
    const backButton = screen.getByRole('button', { name: /back/i });
    fireEvent.click(backButton);

    await waitFor(() => {
      expect(window.location.pathname).toBe('/indicators');
    });

    // Delete indicator
    const deleteButton = screen.getByRole('button', { name: /delete/i });
    fireEvent.click(deleteButton);

    // Confirm deletion
    const confirmButton = screen.getByRole('button', { name: /confirm/i });
    fireEvent.click(confirmButton);

    await waitFor(() => {
      expect(indicatorService.deleteIndicator).toHaveBeenCalledWith(1);
    });
  });

  it('handles API errors gracefully', async () => {
    const { indicatorService } = await import('@/services/api');
    
    // Mock API to fail
    vi.mocked(indicatorService.getIndicators).mockRejectedValue(new Error('API Error'));

    render(
      <QueryClientProvider client={queryClient}>
        <BrowserRouter>
          <App />
        </BrowserRouter>
      </QueryClientProvider>
    );

    // Navigate to indicators
    const indicatorsLink = screen.getByText(/indicators/i);
    fireEvent.click(indicatorsLink);

    // Should show error message
    await waitFor(() => {
      expect(screen.getByText(/error/i)).toBeInTheDocument();
    });
  });

  it('shows loading states during operations', async () => {
    const { indicatorService } = await import('@/services/api');
    
    // Mock API with delay
    vi.mocked(indicatorService.getIndicators).mockImplementation(
      () => new Promise(resolve => setTimeout(() => resolve([]), 100))
    );

    render(
      <QueryClientProvider client={queryClient}>
        <BrowserRouter>
          <App />
        </BrowserRouter>
      </QueryClientProvider>
    );

    // Navigate to indicators
    const indicatorsLink = screen.getByText(/indicators/i);
    fireEvent.click(indicatorsLink);

    // Should show loading state
    expect(screen.getByText(/loading/i)).toBeInTheDocument();

    // Wait for loading to complete
    await waitFor(() => {
      expect(screen.queryByText(/loading/i)).not.toBeInTheDocument();
    });
  });
});
