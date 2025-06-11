import { useQuery } from '@tanstack/react-query';
import { kpiApi } from '@/services/api';
import { KpiDto, KpiMetricsDto } from '@/types/api';
import { queryKeys } from '@/utils/queryKeys';

/**
 * Enhanced useKpis hook using TanStack Query
 * Provides automatic caching, background refetching, and error handling
 */
export const useKpis = (filters?: { isActive?: boolean; owner?: string; priority?: number }) => {
  return useQuery({
    queryKey: queryKeys.kpis.list(filters || {}),
    queryFn: () => kpiApi.getKpis(filters),
    placeholderData: (previousData) => previousData, // Prevents UI flickering during refetch
    staleTime: 30 * 1000, // Consider data fresh for 30 seconds
    refetchInterval: 2 * 60 * 1000, // Auto-refetch every 2 minutes for updated lastRun times
  });
};

/**
 * Hook to fetch a single KPI by ID
 */
export const useKpi = (id: number) => {
  return useQuery({
    queryKey: queryKeys.kpis.detail(id),
    queryFn: () => kpiApi.getKpi(id),
    enabled: !!id && id > 0,
    staleTime: 30 * 1000,
  });
};

/**
 * Hook to fetch KPI metrics/analytics
 */
export const useKpiMetrics = (id: number, days: number = 30) => {
  return useQuery({
    queryKey: queryKeys.kpis.analytics(id),
    queryFn: () => kpiApi.getKpiMetrics(id, days),
    enabled: !!id && id > 0,
    staleTime: 5 * 60 * 1000, // Metrics can be stale for 5 minutes
    refetchInterval: 10 * 60 * 1000, // Refetch every 10 minutes
  });
};

/**
 * Hook to fetch KPI execution history
 */
export const useKpiExecutions = (id: number) => {
  return useQuery({
    queryKey: queryKeys.kpis.executions(id),
    queryFn: () => kpiApi.getKpiExecutions?.(id) || Promise.resolve([]),
    enabled: !!id && id > 0,
    staleTime: 30 * 1000,
    refetchInterval: 60 * 1000, // Refetch every minute for recent executions
  });
};

/**
 * Enhanced useKpiDashboard hook for dashboard data with real-time optimization
 */
export const useKpiDashboard = () => {
  return useQuery({
    queryKey: queryKeys.kpis.dashboard(),
    queryFn: kpiApi.getDashboard,
    placeholderData: (previousData) => previousData,
    staleTime: 5 * 1000, // Consider data fresh for 5 seconds for real-time dashboard
    refetchInterval: 5 * 1000, // Auto-refetch every 5 seconds for real-time updates
    retry: 2,
    retryDelay: 1000,
  });
};
