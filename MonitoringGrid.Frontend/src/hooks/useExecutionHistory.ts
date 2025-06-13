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
    queryFn: () => executionHistoryApi.getExecutionHistory(filters || {}),
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
 * Enhanced useIndicatorExecutionHistory hook for Indicator-specific execution history
 */
export const useIndicatorExecutionHistory = (indicatorId: number, limit?: number) => {
  return useQuery({
    queryKey: queryKeys.executionHistory.byIndicator(indicatorId, limit),
    queryFn: () => executionHistoryApi.getExecutionHistory({ kpiId: indicatorId, pageSize: limit }),
    enabled: !!indicatorId && indicatorId > 0,
    staleTime: 30 * 1000,
    refetchInterval: 60 * 1000, // Auto-refetch every minute for recent executions
  });
};

// Legacy export for backward compatibility
export const useKpiExecutionHistory = useIndicatorExecutionHistory;

/**
 * Enhanced useExecutionStatistics hook for execution analytics
 */
export const useExecutionStatistics = (timeRangeDays: number = 30) => {
  return useQuery({
    queryKey: queryKeys.executionHistory.statistics(timeRangeDays),
    queryFn: () =>
      executionHistoryApi.getExecutionStats({ days: timeRangeDays }) || Promise.resolve(null),
    placeholderData: previousData => previousData,
    staleTime: 5 * 60 * 1000, // Consider data fresh for 5 minutes
    refetchInterval: 10 * 60 * 1000, // Auto-refetch every 10 minutes
    enabled: !!executionHistoryApi.getExecutionStats, // Only run if endpoint exists
  });
};

/**
 * Enhanced useRecentExecutions hook for dashboard recent executions
 */
export const useRecentExecutions = (limit: number = 10) => {
  return useQuery({
    queryKey: queryKeys.executionHistory.recent(limit),
    queryFn: () =>
      executionHistoryApi
        .getExecutionHistory({ pageSize: limit })
        .then(data => data.executions || []),
    placeholderData: previousData => previousData,
    staleTime: 30 * 1000, // Consider data fresh for 30 seconds
    refetchInterval: 60 * 1000, // Auto-refetch every minute for real-time updates
    enabled: true, // Always enabled since we're using the main getExecutionHistory method
  });
};
