import { useQuery, UseQueryResult } from '@tanstack/react-query';
import { monitorStatisticsApi } from '../services/api';

// Types for Monitor Statistics
export interface MonitorStatisticsCollector {
  id: number;
  collectorID: number;
  collectorCode?: string;
  collectorDesc?: string;
  frequencyMinutes: number;
  lastMinutes?: number;
  storeProcedure?: string;
  isActive?: boolean;
  updatedDate?: string;
  lastRun?: string;
  lastRunResult?: string;
  displayName: string;
  frequencyDisplay: string;
  lastRunDisplay: string;
  isActiveStatus: boolean;
  statusDisplay: string;
  statisticsCount: number;
  itemNames: string[];
}

export interface MonitorStatistics {
  day: string;
  hour: number;
  collectorID: number;
  itemName?: string;
  total?: number;
  marked?: number;
  markedPercent?: number;
  updatedDate?: string;
  displayTime: string;
  collectorName: string;
}

export interface ApiResult<T> {
  isSuccess: boolean;
  value?: T;
  error?: {
    code: string;
    message: string;
  };
}

// Hook to fetch active collectors
export const useActiveCollectors = (): UseQueryResult<MonitorStatisticsCollector[], Error> => {
  return useQuery({
    queryKey: ['monitor-statistics', 'collectors', 'active'],
    queryFn: () => monitorStatisticsApi.getActiveCollectors(),
    staleTime: 5 * 60 * 1000, // 5 minutes
    retry: 2,
  });
};

// Hook to fetch all collectors
export const useAllCollectors = (): UseQueryResult<MonitorStatisticsCollector[], Error> => {
  return useQuery({
    queryKey: ['monitor-statistics', 'collectors', 'all'],
    queryFn: () => monitorStatisticsApi.getAllCollectors(),
    staleTime: 5 * 60 * 1000, // 5 minutes
    retry: 2,
  });
};

// Hook to fetch specific collector
export const useCollector = (
  collectorId: number
): UseQueryResult<MonitorStatisticsCollector, Error> => {
  return useQuery({
    queryKey: ['monitor-statistics', 'collectors', collectorId],
    queryFn: () => monitorStatisticsApi.getCollector(collectorId),
    enabled: !!collectorId,
    staleTime: 5 * 60 * 1000, // 5 minutes
    retry: 2,
  });
};

// Hook to fetch collector item names
export const useCollectorItemNames = (collectorId: number): UseQueryResult<string[], Error> => {
  return useQuery({
    queryKey: ['monitor-statistics', 'collectors', collectorId, 'items'],
    queryFn: async () => {
      console.log(`Fetching item names for collector ${collectorId}`);
      try {
        const result = await monitorStatisticsApi.getCollectorItemNames(collectorId);
        console.log(
          `Successfully fetched ${result.length} item names for collector ${collectorId}:`,
          result
        );
        return result;
      } catch (error: any) {
        console.error(`Failed to fetch item names for collector ${collectorId}:`, error);

        // If it's an authentication error, provide some mock data for development
        if (error.response?.status === 401) {
          console.warn('Authentication error - providing mock item names for development');
          return ['MockItem1', 'MockItem2', 'MockItem3'];
        }

        throw error;
      }
    },
    enabled: !!collectorId,
    staleTime: 2 * 60 * 1000, // 2 minutes (reduced for better responsiveness)
    retry: 2,
  });
};

// Hook to fetch statistics
export const useCollectorStatistics = (
  collectorId: number,
  options?: {
    fromDate?: string;
    toDate?: string;
    hours?: number;
  }
): UseQueryResult<MonitorStatistics[], Error> => {
  const { fromDate, toDate, hours = 24 } = options || {};

  return useQuery({
    queryKey: [
      'monitor-statistics',
      'collectors',
      collectorId,
      'statistics',
      { fromDate, toDate, hours },
    ],
    queryFn: () =>
      monitorStatisticsApi.getCollectorStatistics(collectorId, { fromDate, toDate, hours }),
    enabled: !!collectorId,
    staleTime: 2 * 60 * 1000, // 2 minutes (statistics are more dynamic)
    retry: 2,
  });
};
