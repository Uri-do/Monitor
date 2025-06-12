import { useQuery } from '@tanstack/react-query';
import { indicatorApi, collectorApi, monitorStatisticsApi } from '@/services/api';
import { queryKeys } from '@/utils/queryKeys';

/**
 * Enhanced useIndicators hook using TanStack Query
 * Provides automatic caching, background refetching, and error handling
 */
export const useIndicators = (filters?: { 
  isActive?: boolean; 
  search?: string; 
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortDirection?: 'asc' | 'desc';
}) => {
  return useQuery({
    queryKey: queryKeys.indicators.list(filters || {}),
    queryFn: () => indicatorApi.getIndicators(filters),
    placeholderData: previousData => previousData, // Prevents UI flickering during refetch
    staleTime: 30 * 1000, // Consider data fresh for 30 seconds
    refetchInterval: 2 * 60 * 1000, // Auto-refetch every 2 minutes for updated lastExecuted times
  });
};

/**
 * Hook to fetch a single Indicator by ID
 */
export const useIndicator = (id: number) => {
  return useQuery({
    queryKey: queryKeys.indicators.detail(id),
    queryFn: () => indicatorApi.getIndicator(id),
    enabled: !!id && id > 0,
    staleTime: 30 * 1000,
  });
};

/**
 * Enhanced useIndicatorDashboard hook for dashboard data with real-time optimization
 */
export const useIndicatorDashboard = () => {
  return useQuery({
    queryKey: queryKeys.indicators.dashboard(),
    queryFn: indicatorApi.getDashboard,
    placeholderData: previousData => previousData,
    staleTime: 5 * 1000, // Consider data fresh for 5 seconds for real-time dashboard
    refetchInterval: 5 * 1000, // Auto-refetch every 5 seconds for real-time updates
    retry: 2,
    retryDelay: 1000,
  });
};

/**
 * Hook to fetch all collectors for dropdown selection
 * Updated to use new monitor statistics API
 */
export const useCollectors = () => {
  return useQuery({
    queryKey: queryKeys.collectors.list(),
    queryFn: monitorStatisticsApi.getActiveCollectors,
    staleTime: 5 * 60 * 1000, // Collectors don't change often, cache for 5 minutes
    refetchInterval: 10 * 60 * 1000, // Refetch every 10 minutes
  });
};

/**
 * Hook to fetch collector item names for a specific collector
 * Updated to use new monitor statistics API
 */
export const useCollectorItemNames = (collectorId: number) => {
  return useQuery({
    queryKey: queryKeys.collectors.items(collectorId),
    queryFn: () => monitorStatisticsApi.getCollectorItemNames(collectorId),
    enabled: !!collectorId && collectorId > 0,
    staleTime: 5 * 60 * 1000, // Item names don't change often
    refetchInterval: 10 * 60 * 1000,
  });
};
