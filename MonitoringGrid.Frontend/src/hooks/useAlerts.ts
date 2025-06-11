import { useQuery } from '@tanstack/react-query';
import { alertApi } from '@/services/api';
import { AlertFilterDto } from '@/types/api';
// PaginatedAlertsDto available for future use
import { queryKeys } from '@/utils/queryKeys';

/**
 * Enhanced useAlerts hook using TanStack Query
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
 * Enhanced useAlertStatistics hook for analytics
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
 * Enhanced useAlertDashboard hook for dashboard data with real-time optimization
 */
export const useAlertDashboard = () => {
  return useQuery({
    queryKey: queryKeys.alerts.dashboard(),
    queryFn: alertApi.getDashboard,
    placeholderData: previousData => previousData,
    staleTime: 10 * 1000, // Consider data fresh for 10 seconds for real-time dashboard
    refetchInterval: 10 * 1000, // Auto-refetch every 10 seconds for real-time updates
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
