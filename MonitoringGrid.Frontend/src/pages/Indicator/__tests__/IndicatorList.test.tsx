import { describe, it, expect, beforeEach, vi } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@/test/utils';
import { createMockIndicator } from '@/test/utils';
import IndicatorList from '../IndicatorList';

// Mock the hooks
vi.mock('@/hooks/useIndicators', () => ({
  useIndicators: vi.fn(),
  useDeleteIndicator: vi.fn(),
}));

vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom');
  return {
    ...actual,
    useNavigate: vi.fn(),
  };
});

describe('IndicatorList Component', () => {
  const mockNavigate = vi.fn();
  const mockDeleteMutation = {
    mutateAsync: vi.fn(),
    isPending: false,
  };

  beforeEach(() => {
    vi.clearAllMocks();
    
    const { useNavigate } = require('react-router-dom');
    useNavigate.mockReturnValue(mockNavigate);

    const { useDeleteIndicator } = require('@/hooks/useIndicators');
    useDeleteIndicator.mockReturnValue(mockDeleteMutation);
  });

  it('renders loading state', () => {
    const { useIndicators } = require('@/hooks/useIndicators');
    useIndicators.mockReturnValue({
      data: undefined,
      isLoading: true,
      isError: false,
      error: null,
    });

    render(<IndicatorList />);

    expect(screen.getByText(/loading/i)).toBeInTheDocument();
  });

  it('renders error state', () => {
    const { useIndicators } = require('@/hooks/useIndicators');
    useIndicators.mockReturnValue({
      data: undefined,
      isLoading: false,
      isError: true,
      error: new Error('Failed to load indicators'),
    });

    render(<IndicatorList />);

    expect(screen.getByText(/error/i)).toBeInTheDocument();
  });

  it('renders empty state when no indicators', () => {
    const { useIndicators } = require('@/hooks/useIndicators');
    useIndicators.mockReturnValue({
      data: [],
      isLoading: false,
      isError: false,
      error: null,
    });

    render(<IndicatorList />);

    expect(screen.getByText(/no indicators found/i)).toBeInTheDocument();
  });

  it('renders indicators list', () => {
    const mockIndicators = [
      createMockIndicator({ id: 1, name: 'Indicator 1', isActive: true }),
      createMockIndicator({ id: 2, name: 'Indicator 2', isActive: false }),
    ];

    const { useIndicators } = require('@/hooks/useIndicators');
    useIndicators.mockReturnValue({
      data: mockIndicators,
      isLoading: false,
      isError: false,
      error: null,
    });

    render(<IndicatorList />);

    expect(screen.getByText('Indicator 1')).toBeInTheDocument();
    expect(screen.getByText('Indicator 2')).toBeInTheDocument();
  });

  it('navigates to create page when create button is clicked', () => {
    const { useIndicators } = require('@/hooks/useIndicators');
    useIndicators.mockReturnValue({
      data: [],
      isLoading: false,
      isError: false,
      error: null,
    });

    render(<IndicatorList />);

    const createButton = screen.getByRole('button', { name: /create/i });
    fireEvent.click(createButton);

    expect(mockNavigate).toHaveBeenCalledWith('/indicators/create');
  });

  it('navigates to detail page when indicator is clicked', () => {
    const mockIndicators = [
      createMockIndicator({ id: 1, name: 'Indicator 1' }),
    ];

    const { useIndicators } = require('@/hooks/useIndicators');
    useIndicators.mockReturnValue({
      data: mockIndicators,
      isLoading: false,
      isError: false,
      error: null,
    });

    render(<IndicatorList />);

    const indicatorRow = screen.getByText('Indicator 1');
    fireEvent.click(indicatorRow);

    expect(mockNavigate).toHaveBeenCalledWith('/indicators/1');
  });

  it('shows delete confirmation dialog', async () => {
    const mockIndicators = [
      createMockIndicator({ id: 1, name: 'Indicator 1' }),
    ];

    const { useIndicators } = require('@/hooks/useIndicators');
    useIndicators.mockReturnValue({
      data: mockIndicators,
      isLoading: false,
      isError: false,
      error: null,
    });

    render(<IndicatorList />);

    const deleteButton = screen.getByRole('button', { name: /delete/i });
    fireEvent.click(deleteButton);

    expect(screen.getByText(/confirm delete/i)).toBeInTheDocument();
  });

  it('deletes indicator when confirmed', async () => {
    const mockIndicators = [
      createMockIndicator({ id: 1, name: 'Indicator 1' }),
    ];

    const { useIndicators } = require('@/hooks/useIndicators');
    useIndicators.mockReturnValue({
      data: mockIndicators,
      isLoading: false,
      isError: false,
      error: null,
    });

    mockDeleteMutation.mutateAsync.mockResolvedValue(undefined);

    render(<IndicatorList />);

    // Open delete dialog
    const deleteButton = screen.getByRole('button', { name: /delete/i });
    fireEvent.click(deleteButton);

    // Confirm deletion
    const confirmButton = screen.getByRole('button', { name: /confirm/i });
    fireEvent.click(confirmButton);

    await waitFor(() => {
      expect(mockDeleteMutation.mutateAsync).toHaveBeenCalledWith(1);
    });
  });

  it('filters indicators by search term', () => {
    const mockIndicators = [
      createMockIndicator({ id: 1, name: 'Database Indicator' }),
      createMockIndicator({ id: 2, name: 'API Indicator' }),
    ];

    const { useIndicators } = require('@/hooks/useIndicators');
    useIndicators.mockReturnValue({
      data: mockIndicators,
      isLoading: false,
      isError: false,
      error: null,
    });

    render(<IndicatorList />);

    const searchInput = screen.getByPlaceholderText(/search/i);
    fireEvent.change(searchInput, { target: { value: 'Database' } });

    expect(screen.getByText('Database Indicator')).toBeInTheDocument();
    expect(screen.queryByText('API Indicator')).not.toBeInTheDocument();
  });

  it('filters indicators by active status', () => {
    const mockIndicators = [
      createMockIndicator({ id: 1, name: 'Active Indicator', isActive: true }),
      createMockIndicator({ id: 2, name: 'Inactive Indicator', isActive: false }),
    ];

    const { useIndicators } = require('@/hooks/useIndicators');
    useIndicators.mockReturnValue({
      data: mockIndicators,
      isLoading: false,
      isError: false,
      error: null,
    });

    render(<IndicatorList />);

    const activeFilter = screen.getByRole('button', { name: /active only/i });
    fireEvent.click(activeFilter);

    expect(screen.getByText('Active Indicator')).toBeInTheDocument();
    expect(screen.queryByText('Inactive Indicator')).not.toBeInTheDocument();
  });

  it('refreshes data when refresh button is clicked', () => {
    const mockRefetch = vi.fn();
    const { useIndicators } = require('@/hooks/useIndicators');
    useIndicators.mockReturnValue({
      data: [],
      isLoading: false,
      isError: false,
      error: null,
      refetch: mockRefetch,
    });

    render(<IndicatorList />);

    const refreshButton = screen.getByRole('button', { name: /refresh/i });
    fireEvent.click(refreshButton);

    expect(mockRefetch).toHaveBeenCalled();
  });
});
