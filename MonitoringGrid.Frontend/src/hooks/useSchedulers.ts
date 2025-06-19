import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { schedulerApi } from '@/services/api';
import { SchedulerDto, CreateSchedulerRequest, UpdateSchedulerRequest } from '@/types/api';
import { useConditionalQuery, useDynamicQuery, useStableQuery } from './useGenericQuery';

// Get all schedulers
export const useSchedulers = (includeDisabled: boolean = false) => {
  return useQuery({
    queryKey: ['schedulers', includeDisabled],
    queryFn: () => schedulerApi.getSchedulers(includeDisabled),
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
};

// Get scheduler by ID
export const useScheduler = (id: number, options?: { enabled?: boolean }) => {
  return useQuery({
    queryKey: ['schedulers', id],
    queryFn: () => schedulerApi.getScheduler(id),
    enabled: options?.enabled ?? !!id,
    staleTime: 5 * 60 * 1000,
  });
};

// Create scheduler mutation
export const useCreateScheduler = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateSchedulerRequest) => schedulerApi.createScheduler(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['schedulers'] });
    },
  });
};

// Update scheduler mutation
export const useUpdateScheduler = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: UpdateSchedulerRequest) => schedulerApi.updateScheduler(data),
    onSuccess: (updatedScheduler) => {
      queryClient.invalidateQueries({ queryKey: ['schedulers'] });
      queryClient.invalidateQueries({ queryKey: ['schedulers', updatedScheduler.schedulerID] });
    },
  });
};

// Delete scheduler mutation
export const useDeleteScheduler = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: number) => schedulerApi.deleteScheduler(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['schedulers'] });
    },
  });
};

// Toggle scheduler enabled/disabled
export const useToggleScheduler = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, enabled }: { id: number; enabled: boolean }) =>
      schedulerApi.toggleScheduler(id, enabled),
    onSuccess: (updatedScheduler) => {
      queryClient.invalidateQueries({ queryKey: ['schedulers'] });
      queryClient.invalidateQueries({ queryKey: ['schedulers', updatedScheduler.schedulerID] });
    },
  });
};

// Hook to get upcoming indicator executions
export const useUpcomingExecutions = (hours: number = 24) => {
  return useConditionalQuery(
    ['schedulers', 'upcoming', hours],
    () => schedulerApi.getUpcomingExecutions(hours),
    [hours],
    {
      errorContext: `Loading upcoming executions for next ${hours} hours`,
      preset: 'dynamic',
      graceful404: true,
      fallbackValue: [],
    }
  );
};

// Hook to get due indicators
export const useDueIndicators = () => {
  return useDynamicQuery(
    ['schedulers', 'due-indicators'],
    () => schedulerApi.getDueIndicators(),
    {
      errorContext: 'Loading due indicators',
      graceful404: true,
      fallbackValue: [],
      // Refresh every 2 minutes since this is time-sensitive but not critical
      staleTime: 120 * 1000,
      cacheTime: 300 * 1000,
      refetchInterval: 120 * 1000,
    }
  );
};

// Hook to get indicators with scheduler information
export const useIndicatorsWithSchedulers = () => {
  return useStableQuery(
    ['schedulers', 'indicators'],
    () => schedulerApi.getIndicatorsWithSchedulers(),
    {
      errorContext: 'Loading indicators with schedulers',
      graceful404: true,
      fallbackValue: [],
    }
  );
};

export default {
  useSchedulers,
  useScheduler,
  useCreateScheduler,
  useUpdateScheduler,
  useDeleteScheduler,
  useToggleScheduler,
  useUpcomingExecutions,
  useDueIndicators,
  useIndicatorsWithSchedulers,
};
