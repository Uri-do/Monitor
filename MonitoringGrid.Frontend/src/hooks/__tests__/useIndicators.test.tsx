import { describe, it, expect, beforeEach, vi } from 'vitest';
import { renderHook, waitFor } from '@testing-library/react';
import { AllTheProviders, createTestQueryClient, resetApiMocks, makeApiCallFail } from '@/test/utils';
import { useIndicators, useIndicator, useCreateIndicator, useUpdateIndicator, useDeleteIndicator } from '../useIndicators';

// Mock the API service
vi.mock('@/services/api', () => ({
  indicatorService: {
    getIndicators: vi.fn().mockResolvedValue([
      { id: 1, name: 'Indicator 1', isActive: true },
      { id: 2, name: 'Indicator 2', isActive: false },
    ]),
    getIndicator: vi.fn().mockResolvedValue({ id: 1, name: 'Indicator 1', isActive: true }),
    createIndicator: vi.fn().mockResolvedValue({ id: 3, name: 'New Indicator', isActive: true }),
    updateIndicator: vi.fn().mockResolvedValue({ id: 1, name: 'Updated Indicator', isActive: true }),
    deleteIndicator: vi.fn().mockResolvedValue(undefined),
  },
}));

describe('useIndicators Hook', () => {
  beforeEach(() => {
    resetApiMocks();
  });

  describe('useIndicators', () => {
    it('fetches indicators successfully', async () => {
      const queryClient = createTestQueryClient();
      const wrapper = ({ children }: { children: React.ReactNode }) => (
        <AllTheProviders queryClient={queryClient}>{children}</AllTheProviders>
      );

      const { result } = renderHook(() => useIndicators(), { wrapper });

      expect(result.current.isLoading).toBe(true);

      await waitFor(() => {
        expect(result.current.isSuccess).toBe(true);
      });

      expect(result.current.data).toHaveLength(2);
      expect(result.current.data?.[0]).toEqual({
        id: 1,
        name: 'Indicator 1',
        isActive: true,
      });
    });

    it('handles error when fetching indicators fails', async () => {
      const queryClient = createTestQueryClient();
      const wrapper = ({ children }: { children: React.ReactNode }) => (
        <AllTheProviders queryClient={queryClient}>{children}</AllTheProviders>
      );

      // Mock API to fail
      const { indicatorService } = await import('@/services/api');
      vi.mocked(indicatorService.getIndicators).mockRejectedValueOnce(new Error('API Error'));

      const { result } = renderHook(() => useIndicators(), { wrapper });

      await waitFor(() => {
        expect(result.current.isError).toBe(true);
      });

      expect(result.current.error).toBeInstanceOf(Error);
    });

    it('filters active indicators when specified', async () => {
      const queryClient = createTestQueryClient();
      const wrapper = ({ children }: { children: React.ReactNode }) => (
        <AllTheProviders queryClient={queryClient}>{children}</AllTheProviders>
      );

      const { result } = renderHook(() => useIndicators({ activeOnly: true }), { wrapper });

      await waitFor(() => {
        expect(result.current.isSuccess).toBe(true);
      });

      // Should filter to only active indicators
      expect(result.current.data).toHaveLength(1);
      expect(result.current.data?.[0].isActive).toBe(true);
    });
  });

  describe('useIndicator', () => {
    it('fetches single indicator successfully', async () => {
      const queryClient = createTestQueryClient();
      const wrapper = ({ children }: { children: React.ReactNode }) => (
        <AllTheProviders queryClient={queryClient}>{children}</AllTheProviders>
      );

      const { result } = renderHook(() => useIndicator(1), { wrapper });

      await waitFor(() => {
        expect(result.current.isSuccess).toBe(true);
      });

      expect(result.current.data).toEqual({
        id: 1,
        name: 'Indicator 1',
        isActive: true,
      });
    });

    it('does not fetch when id is 0 or undefined', () => {
      const queryClient = createTestQueryClient();
      const wrapper = ({ children }: { children: React.ReactNode }) => (
        <AllTheProviders queryClient={queryClient}>{children}</AllTheProviders>
      );

      const { result } = renderHook(() => useIndicator(0), { wrapper });

      expect(result.current.isIdle).toBe(true);
      expect(result.current.data).toBeUndefined();
    });
  });

  describe('useCreateIndicator', () => {
    it('creates indicator successfully', async () => {
      const queryClient = createTestQueryClient();
      const wrapper = ({ children }: { children: React.ReactNode }) => (
        <AllTheProviders queryClient={queryClient}>{children}</AllTheProviders>
      );

      const { result } = renderHook(() => useCreateIndicator(), { wrapper });

      const newIndicator = { name: 'New Indicator', description: 'Test', isActive: true };

      await result.current.mutateAsync(newIndicator);

      expect(result.current.isSuccess).toBe(true);
      expect(result.current.data).toEqual({
        id: 3,
        name: 'New Indicator',
        isActive: true,
      });
    });

    it('handles creation error', async () => {
      const queryClient = createTestQueryClient();
      const wrapper = ({ children }: { children: React.ReactNode }) => (
        <AllTheProviders queryClient={queryClient}>{children}</AllTheProviders>
      );

      // Mock API to fail
      const { indicatorService } = await import('@/services/api');
      vi.mocked(indicatorService.createIndicator).mockRejectedValueOnce(new Error('Creation failed'));

      const { result } = renderHook(() => useCreateIndicator(), { wrapper });

      const newIndicator = { name: 'New Indicator', description: 'Test', isActive: true };

      try {
        await result.current.mutateAsync(newIndicator);
      } catch (error) {
        expect(error).toBeInstanceOf(Error);
        expect(result.current.isError).toBe(true);
      }
    });
  });

  describe('useUpdateIndicator', () => {
    it('updates indicator successfully', async () => {
      const queryClient = createTestQueryClient();
      const wrapper = ({ children }: { children: React.ReactNode }) => (
        <AllTheProviders queryClient={queryClient}>{children}</AllTheProviders>
      );

      const { result } = renderHook(() => useUpdateIndicator(), { wrapper });

      const updateData = { id: 1, name: 'Updated Indicator', isActive: true };

      await result.current.mutateAsync(updateData);

      expect(result.current.isSuccess).toBe(true);
      expect(result.current.data).toEqual({
        id: 1,
        name: 'Updated Indicator',
        isActive: true,
      });
    });
  });

  describe('useDeleteIndicator', () => {
    it('deletes indicator successfully', async () => {
      const queryClient = createTestQueryClient();
      const wrapper = ({ children }: { children: React.ReactNode }) => (
        <AllTheProviders queryClient={queryClient}>{children}</AllTheProviders>
      );

      const { result } = renderHook(() => useDeleteIndicator(), { wrapper });

      await result.current.mutateAsync(1);

      expect(result.current.isSuccess).toBe(true);
    });

    it('handles deletion error', async () => {
      const queryClient = createTestQueryClient();
      const wrapper = ({ children }: { children: React.ReactNode }) => (
        <AllTheProviders queryClient={queryClient}>{children}</AllTheProviders>
      );

      // Mock API to fail
      const { indicatorService } = await import('@/services/api');
      vi.mocked(indicatorService.deleteIndicator).mockRejectedValueOnce(new Error('Deletion failed'));

      const { result } = renderHook(() => useDeleteIndicator(), { wrapper });

      try {
        await result.current.mutateAsync(1);
      } catch (error) {
        expect(error).toBeInstanceOf(Error);
        expect(result.current.isError).toBe(true);
      }
    });
  });
});
