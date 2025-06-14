import { useMutation, useQueryClient } from '@tanstack/react-query';
import {
  useGenericQuery,
  useStableQuery,
  useDynamicQuery,
  useOneTimeQuery,
  useOptionalQuery,
  useConditionalQuery,
} from './useGenericQuery';
import {
  indicatorService,
  Indicator,
  CreateIndicatorRequest,
  UpdateIndicatorRequest,
} from '../services/indicatorService';
import { ErrorHandlers } from '../utils/errorHandling';

// Query keys for indicators
export const indicatorQueryKeys = {
  all: ['indicators'] as const,
  lists: () => [...indicatorQueryKeys.all, 'list'] as const,
  list: (filters: Record<string, any>) => [...indicatorQueryKeys.lists(), { filters }] as const,
  details: () => [...indicatorQueryKeys.all, 'detail'] as const,
  detail: (id: number) => [...indicatorQueryKeys.details(), id] as const,
  executions: (id: number) => [...indicatorQueryKeys.detail(id), 'executions'] as const,
  statistics: (id: number) => [...indicatorQueryKeys.detail(id), 'statistics'] as const,
  search: (query: string) => [...indicatorQueryKeys.all, 'search', query] as const,
  byCollector: (collectorId: number) =>
    [...indicatorQueryKeys.all, 'by-collector', collectorId] as const,
  byOwner: (ownerId: number) => [...indicatorQueryKeys.all, 'by-owner', ownerId] as const,
};

// Hook to fetch all indicators
export const useIndicators = (filters?: Record<string, any>) => {
  return useDynamicQuery(
    indicatorQueryKeys.list(filters || {}),
    async () => {
      try {
        const result = await indicatorService.getAll();
        // Ensure we always return an array
        if (!Array.isArray(result)) {
          console.warn('Indicators API returned non-array data:', result);
          return [];
        }
        return result;
      } catch (error: any) {
        // Handle authentication errors gracefully
        if (error?.status === 401) {
          console.warn('Authentication required for indicators endpoint');
          return [];
        }
        throw error;
      }
    },
    {
      errorContext: 'Loading indicators',
      graceful404: true,
      fallbackValue: [],
    }
  );
};

// Hook to fetch a specific indicator
export const useIndicator = (id: number, enabled: boolean = true) => {
  return useConditionalQuery(
    indicatorQueryKeys.detail(id),
    () => indicatorService.getById(id),
    [id, enabled],
    {
      errorContext: `Loading indicator ${id}`,
      preset: 'oneTime',
    }
  );
};

// Hook to fetch indicator execution history
export const useIndicatorExecutions = (
  id: number,
  options?: {
    limit?: number;
    offset?: number;
    status?: string;
    fromDate?: string;
    toDate?: string;
  }
) => {
  return useConditionalQuery(
    indicatorQueryKeys.executions(id),
    () => indicatorService.getExecutionHistory(id, options),
    [id],
    {
      errorContext: `Loading execution history for indicator ${id}`,
      preset: 'dynamic',
      graceful404: true,
      fallbackValue: [],
    }
  );
};

// Hook to fetch indicator statistics
export const useIndicatorStatistics = (id: number) => {
  return useConditionalQuery(
    indicatorQueryKeys.statistics(id),
    () => indicatorService.getStatistics(id),
    [id],
    {
      errorContext: `Loading statistics for indicator ${id}`,
      preset: 'dynamic',
    }
  );
};

// Hook to search indicators
export const useIndicatorSearch = (
  query: string,
  filters?: {
    isActive?: boolean;
    priority?: number;
    collectorID?: number;
    ownerContactId?: number;
  }
) => {
  return useConditionalQuery(
    indicatorQueryKeys.search(query),
    () => indicatorService.search(query, filters),
    [query],
    {
      errorContext: 'Searching indicators',
      preset: 'oneTime',
      graceful404: true,
      fallbackValue: [],
    }
  );
};

// Hook to fetch indicators by collector
export const useIndicatorsByCollector = (collectorId: number) => {
  return useConditionalQuery(
    indicatorQueryKeys.byCollector(collectorId),
    () => indicatorService.getByCollector(collectorId),
    [collectorId],
    {
      errorContext: `Loading indicators for collector ${collectorId}`,
      preset: 'stable',
      graceful404: true,
      fallbackValue: [],
    }
  );
};

// Hook to fetch indicators by owner
export const useIndicatorsByOwner = (ownerId: number) => {
  return useConditionalQuery(
    indicatorQueryKeys.byOwner(ownerId),
    () => indicatorService.getByOwner(ownerId),
    [ownerId],
    {
      errorContext: `Loading indicators for owner ${ownerId}`,
      preset: 'stable',
      graceful404: true,
      fallbackValue: [],
    }
  );
};

// Mutation hooks with error handling
export const useCreateIndicator = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateIndicatorRequest) => indicatorService.create(data),
    onSuccess: newIndicator => {
      // Invalidate and refetch indicators list
      queryClient.invalidateQueries({ queryKey: indicatorQueryKeys.lists() });

      // Add the new indicator to the cache
      queryClient.setQueryData(indicatorQueryKeys.detail(newIndicator.indicatorID), newIndicator);

      // If the indicator belongs to a collector, invalidate that list too
      if (newIndicator.collectorID) {
        queryClient.invalidateQueries({
          queryKey: indicatorQueryKeys.byCollector(newIndicator.collectorID),
        });
      }

      // If the indicator has an owner, invalidate that list too
      queryClient.invalidateQueries({
        queryKey: indicatorQueryKeys.byOwner(newIndicator.ownerContactId),
      });
    },
    onError: error => {
      ErrorHandlers.mutation(error, 'Failed to create indicator');
    },
  });
};

export const useUpdateIndicator = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: number; data: UpdateIndicatorRequest }) =>
      indicatorService.update(id, data),
    onSuccess: updatedIndicator => {
      // Update the specific indicator in cache
      queryClient.setQueryData(
        indicatorQueryKeys.detail(updatedIndicator.indicatorID),
        updatedIndicator
      );

      // Invalidate lists to ensure consistency
      queryClient.invalidateQueries({ queryKey: indicatorQueryKeys.lists() });

      // Invalidate related queries
      if (updatedIndicator.collectorID) {
        queryClient.invalidateQueries({
          queryKey: indicatorQueryKeys.byCollector(updatedIndicator.collectorID),
        });
      }

      queryClient.invalidateQueries({
        queryKey: indicatorQueryKeys.byOwner(updatedIndicator.ownerContactId),
      });
    },
    onError: error => {
      ErrorHandlers.mutation(error, 'Failed to update indicator');
    },
  });
};

export const useDeleteIndicator = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: number) => indicatorService.delete(id),
    onSuccess: (_, deletedId) => {
      // Remove from cache
      queryClient.removeQueries({ queryKey: indicatorQueryKeys.detail(deletedId) });

      // Invalidate lists
      queryClient.invalidateQueries({ queryKey: indicatorQueryKeys.lists() });
      queryClient.invalidateQueries({ queryKey: indicatorQueryKeys.all });
    },
    onError: error => {
      ErrorHandlers.mutation(error, 'Failed to delete indicator');
    },
  });
};

export const useExecuteIndicator = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: number) => indicatorService.execute(id),
    onSuccess: (result, id) => {
      // Invalidate execution history and statistics
      queryClient.invalidateQueries({ queryKey: indicatorQueryKeys.executions(id) });
      queryClient.invalidateQueries({ queryKey: indicatorQueryKeys.statistics(id) });
    },
    onError: error => {
      ErrorHandlers.mutation(error, 'Failed to execute indicator');
    },
  });
};

// Bulk operation hooks
export const useBulkDeleteIndicators = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (ids: number[]) => indicatorService.bulkDelete(ids),
    onSuccess: (_, deletedIds) => {
      // Remove all deleted indicators from cache
      deletedIds.forEach(id => {
        queryClient.removeQueries({ queryKey: indicatorQueryKeys.detail(id) });
      });

      // Invalidate all lists
      queryClient.invalidateQueries({ queryKey: indicatorQueryKeys.all });
    },
    onError: error => {
      ErrorHandlers.mutation(error, 'Failed to delete indicators');
    },
  });
};

export const useBulkActivateIndicators = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (ids: number[]) => indicatorService.bulkActivate(ids),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: indicatorQueryKeys.all });
    },
    onError: error => {
      ErrorHandlers.mutation(error, 'Failed to activate indicators');
    },
  });
};

export const useBulkDeactivateIndicators = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (ids: number[]) => indicatorService.bulkDeactivate(ids),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: indicatorQueryKeys.all });
    },
    onError: error => {
      ErrorHandlers.mutation(error, 'Failed to deactivate indicators');
    },
  });
};

// Validation and testing hooks
export const useValidateIndicator = () => {
  return useMutation({
    mutationFn: (data: CreateIndicatorRequest) => indicatorService.validateConfiguration(data),
    onError: error => {
      ErrorHandlers.silent(error, 'Validation failed');
    },
  });
};

export const useTestIndicator = () => {
  return useMutation({
    mutationFn: (data: CreateIndicatorRequest) => indicatorService.testExecution(data),
    onError: error => {
      ErrorHandlers.mutation(error, 'Test execution failed');
    },
  });
};

// Hook for dashboard data
export const useIndicatorDashboard = () => {
  return useDynamicQuery(
    ['indicators', 'dashboard'],
    () => indicatorService.getDashboardData(),
    {
      errorContext: 'Loading dashboard data',
      graceful404: true,
      fallbackValue: {
        totalIndicators: 0,
        activeIndicators: 0,
        recentExecutions: [],
        alerts: [],
        statistics: {}
      },
    }
  );
};

// Hook for collector item names
export const useCollectorItemNames = (collectorId: number) => {
  return useConditionalQuery(
    ['collectors', collectorId, 'items'],
    () => indicatorService.getCollectorItemNames(collectorId),
    [collectorId],
    {
      errorContext: `Loading collector ${collectorId} item names`,
      preset: 'stable',
      graceful404: true,
      fallbackValue: [],
    }
  );
};

export default {
  useIndicators,
  useIndicator,
  useIndicatorExecutions,
  useIndicatorStatistics,
  useIndicatorSearch,
  useIndicatorsByCollector,
  useIndicatorsByOwner,
  useCreateIndicator,
  useUpdateIndicator,
  useDeleteIndicator,
  useExecuteIndicator,
  useBulkDeleteIndicators,
  useBulkActivateIndicators,
  useBulkDeactivateIndicators,
  useValidateIndicator,
  useTestIndicator,
};
