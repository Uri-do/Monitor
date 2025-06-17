import { useQuery } from '@tanstack/react-query';
import { alertApi } from '@/services/api';
import { AlertFilterDto } from '@/types/api';
// PaginatedAlertsDto available for future use
import { queryKeys } from '@/utils/queryKeys';

/**
 * useAlerts hook using TanStack Query
 * Provides automatic caching, background refetching, and error handling
 */
export const useAlerts = (filters: AlertFilterDto) => {
  return useQuery({
    queryKey: queryKeys.alerts.list(filters),
    queryFn: () => alertApi.getAlerts(filters),
    placeholderData: previousData => previousData, // Prevents UI flickering during refetch
    staleTime: 30 * 1000, // Consider data fresh for 30 seconds
    refetchInterval: 60 * 1000, // Auto-refetch every minute for real-time updates
  });
};

/**
 * useAlertStatistics hook for analytics
 */
export const useAlertStatistics = (timeRangeDays: number) => {
  return useQuery({
    queryKey: queryKeys.alerts.statistics(timeRangeDays),
    queryFn: () => alertApi.getStatistics(timeRangeDays),
    placeholderData: previousData => previousData,
    staleTime: 5 * 60 * 1000, // Consider data fresh for 5 minutes
    refetchInterval: 10 * 60 * 1000, // Auto-refetch every 10 minutes
  });
};

/**
 * useAlertDashboard hook for dashboard data with optimized caching
 */
export const useAlertDashboard = () => {
  return useQuery({
    queryKey: queryKeys.alerts.dashboard(),
    queryFn: alertApi.getDashboard,
    placeholderData: previousData => previousData,
    staleTime: 25 * 1000, // Consider data fresh for 25 seconds (slightly less than backend cache)
    refetchInterval: 30 * 1000, // Auto-refetch every 30 seconds to match backend cache expiration
    retry: 2,
    retryDelay: 1000,
  });
};

/**
 * Hook to fetch a single alert by ID
 */
export const useAlert = (id: number) => {
  return useQuery({
    queryKey: queryKeys.alerts.detail(id),
    queryFn: () => alertApi.getAlert(id),
    enabled: !!id && id > 0,
    staleTime: 30 * 1000,
  });
};

/**
 * Hook to fetch basic alert statistics (without time range)
 */
export const useBasicAlertStatistics = () => {
  return useQuery({
    queryKey: queryKeys.alerts.statistics(),
    queryFn: () => alertApi.getStatistics(),
    staleTime: 2 * 60 * 1000, // Statistics can be stale for 2 minutes
    refetchInterval: 5 * 60 * 1000, // Refetch every 5 minutes
  });
};
