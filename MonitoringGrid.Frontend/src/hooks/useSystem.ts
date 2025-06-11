import { useQuery } from '@tanstack/react-query';
import { systemApi } from '@/services/api';
import { queryKeys } from '@/utils/queryKeys';

/**
 * Hook to fetch system health information
 */
export const useSystemHealth = () => {
  return useQuery({
    queryKey: queryKeys.system.health(),
    queryFn: () => systemApi.getHealth(),
    staleTime: 30 * 1000, // Consider data fresh for 30 seconds
    refetchInterval: 60 * 1000, // Auto-refetch every minute for health monitoring
    retry: 3, // Retry failed health checks
  });
};

/**
 * Hook to fetch system analytics
 */
export const useSystemAnalytics = (timeRange: string = '24h') => {
  return useQuery({
    queryKey: queryKeys.system.analytics(),
    queryFn: () => systemApi.getAnalytics(timeRange),
    staleTime: 5 * 60 * 1000, // Consider data fresh for 5 minutes
    refetchInterval: 10 * 60 * 1000, // Auto-refetch every 10 minutes
  });
};

/**
 * Hook to fetch system settings
 */
export const useSystemSettings = () => {
  return useQuery({
    queryKey: queryKeys.system.settings(),
    queryFn: () => systemApi.getSettings(),
    staleTime: 10 * 60 * 1000, // Settings change infrequently
    refetchInterval: 30 * 60 * 1000, // Refetch every 30 minutes
  });
};

/**
 * Hook to fetch dashboard overview data
 */
export const useDashboardOverview = () => {
  return useQuery({
    queryKey: queryKeys.dashboard.overview(),
    queryFn: () => systemApi.getDashboardOverview(),
    staleTime: 30 * 1000, // Consider data fresh for 30 seconds
    refetchInterval: 2 * 60 * 1000, // Auto-refetch every 2 minutes for dashboard
  });
};

/**
 * Hook to fetch dashboard metrics with time range
 */
export const useDashboardMetrics = (timeRange: string = '24h') => {
  return useQuery({
    queryKey: queryKeys.dashboard.metrics(timeRange),
    queryFn: () => systemApi.getDashboardMetrics(timeRange),
    staleTime: 2 * 60 * 1000, // Consider data fresh for 2 minutes
    refetchInterval: 5 * 60 * 1000, // Auto-refetch every 5 minutes
  });
};

/**
 * Hook to fetch real-time dashboard data
 */
export const useRealtimeDashboard = () => {
  return useQuery({
    queryKey: queryKeys.dashboard.realtime(),
    queryFn: () => systemApi.getRealtimeDashboard(),
    staleTime: 10 * 1000, // Consider data fresh for 10 seconds
    refetchInterval: 30 * 1000, // Auto-refetch every 30 seconds for real-time feel
  });
};
