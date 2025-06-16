import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useGenericQuery, useDynamicQuery } from './useGenericQuery';
import { workerApi } from '@/services/api';
import { ErrorHandlers } from '@/utils/errorHandling';

// Worker types
export interface WorkerService {
  name: string;
  status: string;
  lastActivity?: string;
  errorMessage?: string;
  currentActivity?: string;
  processedCount?: number;
  lastProcessedItem?: string;
  nextScheduledRun?: string;
  description?: string;
}

export interface WorkerStatus {
  isRunning: boolean;
  mode: string;
  processId?: number;
  startTime?: string;
  uptime?: string;
  services: WorkerService[];
  timestamp: string;
}

export interface WorkerActionResult {
  success: boolean;
  message: string;
  processId?: number;
  timestamp: string;
}

// Query keys for worker
export const workerQueryKeys = {
  all: ['worker'] as const,
  status: () => [...workerQueryKeys.all, 'status'] as const,
  logs: () => [...workerQueryKeys.all, 'logs'] as const,
};

// Hook to get worker status
export const useWorkerStatus = () => {
  return useDynamicQuery(
    workerQueryKeys.status(),
    async () => {
      try {
        const result = await workerApi.getStatus();
        return result as WorkerStatus;
      } catch (error: any) {
        // Handle authentication errors gracefully
        if (error?.status === 401) {
          console.warn('Authentication required for worker status endpoint');
          return {
            isRunning: false,
            mode: 'Unknown',
            services: [],
            timestamp: new Date().toISOString(),
          } as WorkerStatus;
        }
        throw error;
      }
    },
    {
      errorContext: 'Loading worker status',
      graceful404: true,
      fallbackValue: {
        isRunning: false,
        mode: 'Unknown',
        services: [],
        timestamp: new Date().toISOString(),
      } as WorkerStatus,
      // Cache worker status for 5 seconds since it changes frequently
      staleTime: 5 * 1000,
      // Keep in cache for 10 seconds
      cacheTime: 10 * 1000,
      // Refetch every 10 seconds when component is focused
      refetchInterval: 10 * 1000,
    }
  );
};

// Hook to start worker
export const useStartWorker = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: () => workerApi.start(),
    onSuccess: () => {
      // Invalidate worker status to refresh
      queryClient.invalidateQueries({ queryKey: workerQueryKeys.status() });
    },
    onError: (error) => {
      ErrorHandlers.mutation(error, 'Failed to start worker');
    },
  });
};

// Hook to stop worker
export const useStopWorker = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: () => workerApi.stop(),
    onSuccess: () => {
      // Invalidate worker status to refresh
      queryClient.invalidateQueries({ queryKey: workerQueryKeys.status() });
    },
    onError: (error) => {
      ErrorHandlers.mutation(error, 'Failed to stop worker');
    },
  });
};

// Hook to restart worker
export const useRestartWorker = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: () => workerApi.restart(),
    onSuccess: () => {
      // Invalidate worker status to refresh
      queryClient.invalidateQueries({ queryKey: workerQueryKeys.status() });
    },
    onError: (error) => {
      ErrorHandlers.mutation(error, 'Failed to restart worker');
    },
  });
};

export default {
  useWorkerStatus,
  useStartWorker,
  useStopWorker,
  useRestartWorker,
};
