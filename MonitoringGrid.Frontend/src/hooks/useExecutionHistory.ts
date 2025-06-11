import { useQuery } from '@tanstack/react-query';
import { executionHistoryApi } from '@/services/api';
import { queryKeys } from '@/utils/queryKeys';

/**
 * Enhanced useExecutionHistory hook using TanStack Query
 * Provides automatic caching, background refetching, and error handling
 */
export const useExecutionHistory = (filters?: {
  kpiId?: number;
  startDate?: string;
  endDate?: string;
  isSuccessful?: boolean;
  pageSize?: number;
  pageNumber?: number;
}) => {
  return useQuery({
    queryKey: queryKeys.executionHistory.list(filters || {}),
    queryFn: () => executionHistoryApi.getExecutionHistory(filters),
    placeholderData: previousData => previousData, // Prevents UI flickering during refetch
    staleTime: 30 * 1000, // Consider data fresh for 30 seconds
    refetchInterval: 60 * 1000, // Auto-refetch every minute for recent executions
  });
};

/**
 * Enhanced useExecutionDetail hook for single execution details
 */
export const useExecutionDetail = (executionId: number) => {
  return useQuery({
    queryKey: queryKeys.executionHistory.detail(executionId),
    queryFn: () => executionHistoryApi.getExecutionDetail(executionId),
    enabled: !!executionId && executionId > 0,
    staleTime: 5 * 60 * 1000, // Execution details don't change, cache for 5 minutes
    refetchInterval: false, // Don't auto-refetch execution details
  });
};

/**
 * Enhanced useKpiExecutionHistory hook for KPI-specific execution history
 */
export const useKpiExecutionHistory = (kpiId: number, limit?: number) => {
  return useQuery({
    queryKey: queryKeys.executionHistory.byKpi(kpiId, limit),
    queryFn: () => executionHistoryApi.getKpiExecutionHistory(kpiId, limit),
    enabled: !!kpiId && kpiId > 0,
    staleTime: 30 * 1000,
    refetchInterval: 60 * 1000, // Auto-refetch every minute for recent executions
  });
};

/**
 * Enhanced useExecutionStatistics hook for execution analytics
 */
export const useExecutionStatistics = (timeRangeDays: number = 30) => {
  return useQuery({
    queryKey: queryKeys.executionHistory.statistics(timeRangeDays),
    queryFn: () =>
      executionHistoryApi.getExecutionStatistics?.(timeRangeDays) || Promise.resolve(null),
    placeholderData: previousData => previousData,
    staleTime: 5 * 60 * 1000, // Consider data fresh for 5 minutes
    refetchInterval: 10 * 60 * 1000, // Auto-refetch every 10 minutes
    enabled: !!executionHistoryApi.getExecutionStatistics, // Only run if endpoint exists
  });
};

/**
 * Enhanced useRecentExecutions hook for dashboard recent executions
 */
export const useRecentExecutions = (limit: number = 10) => {
  return useQuery({
    queryKey: queryKeys.executionHistory.recent(limit),
    queryFn: () => executionHistoryApi.getRecentExecutions?.(limit) || Promise.resolve([]),
    placeholderData: previousData => previousData,
    staleTime: 30 * 1000, // Consider data fresh for 30 seconds
    refetchInterval: 60 * 1000, // Auto-refetch every minute for real-time updates
    enabled: !!executionHistoryApi.getRecentExecutions, // Only run if endpoint exists
  });
};
