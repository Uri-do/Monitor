import { useQuery, UseQueryResult } from '@tanstack/react-query';
import { api } from '../services/api';

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
    queryFn: async () => {
      const response = await api.get<ApiResult<MonitorStatisticsCollector[]>>(
        '/monitorstatistics/collectors?activeOnly=true'
      );
      
      if (response.data.isSuccess && response.data.value) {
        return response.data.value;
      } else {
        throw new Error(response.data.error?.message || 'Failed to fetch collectors');
      }
    },
    staleTime: 5 * 60 * 1000, // 5 minutes
    retry: 2,
  });
};

// Hook to fetch all collectors
export const useAllCollectors = (): UseQueryResult<MonitorStatisticsCollector[], Error> => {
  return useQuery({
    queryKey: ['monitor-statistics', 'collectors', 'all'],
    queryFn: async () => {
      const response = await api.get<ApiResult<MonitorStatisticsCollector[]>>(
        '/monitorstatistics/collectors?activeOnly=false'
      );
      
      if (response.data.isSuccess && response.data.value) {
        return response.data.value;
      } else {
        throw new Error(response.data.error?.message || 'Failed to fetch collectors');
      }
    },
    staleTime: 5 * 60 * 1000, // 5 minutes
    retry: 2,
  });
};

// Hook to fetch specific collector
export const useCollector = (collectorId: number): UseQueryResult<MonitorStatisticsCollector, Error> => {
  return useQuery({
    queryKey: ['monitor-statistics', 'collectors', collectorId],
    queryFn: async () => {
      const response = await api.get<ApiResult<MonitorStatisticsCollector>>(
        `/monitorstatistics/collectors/${collectorId}`
      );
      
      if (response.data.isSuccess && response.data.value) {
        return response.data.value;
      } else {
        throw new Error(response.data.error?.message || 'Failed to fetch collector');
      }
    },
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
      const response = await api.get<ApiResult<string[]>>(
        `/monitorstatistics/collectors/${collectorId}/items`
      );
      
      if (response.data.isSuccess && response.data.value) {
        return response.data.value;
      } else {
        throw new Error(response.data.error?.message || 'Failed to fetch item names');
      }
    },
    enabled: !!collectorId,
    staleTime: 10 * 60 * 1000, // 10 minutes (item names change less frequently)
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
    queryKey: ['monitor-statistics', 'collectors', collectorId, 'statistics', { fromDate, toDate, hours }],
    queryFn: async () => {
      let url = `/monitorstatistics/collectors/${collectorId}/statistics`;
      const params = new URLSearchParams();
      
      if (fromDate && toDate) {
        params.append('fromDate', fromDate);
        params.append('toDate', toDate);
      } else {
        params.append('hours', hours.toString());
      }
      
      if (params.toString()) {
        url += `?${params.toString()}`;
      }
      
      const response = await api.get<ApiResult<MonitorStatistics[]>>(url);
      
      if (response.data.isSuccess && response.data.value) {
        return response.data.value;
      } else {
        throw new Error(response.data.error?.message || 'Failed to fetch statistics');
      }
    },
    enabled: !!collectorId,
    staleTime: 2 * 60 * 1000, // 2 minutes (statistics are more dynamic)
    retry: 2,
  });
};
