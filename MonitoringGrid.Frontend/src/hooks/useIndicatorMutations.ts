import { useMutation, useQueryClient } from '@tanstack/react-query';
import { indicatorApi } from '@/services/api';
import { queryKeys } from '@/utils/queryKeys';
import { CreateIndicatorRequest, UpdateIndicatorRequest, TestIndicatorRequest } from '@/types/api';
import toast from 'react-hot-toast';

/**
 * Hook for creating a new indicator
 */
export const useCreateIndicator = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateIndicatorRequest) => indicatorApi.createIndicator(data),
    onSuccess: (newIndicator) => {
      // Invalidate and refetch indicators list
      queryClient.invalidateQueries({ queryKey: queryKeys.indicators.lists() });
      queryClient.invalidateQueries({ queryKey: queryKeys.indicators.dashboard() });
      
      // Add the new indicator to the cache
      queryClient.setQueryData(
        queryKeys.indicators.detail(newIndicator.indicatorId),
        newIndicator
      );

      toast.success(`Indicator "${newIndicator.indicatorName}" created successfully`);
    },
    onError: (error: any) => {
      const message = error.response?.data?.message || error.message || 'Failed to create indicator';
      toast.error(message);
    },
  });
};

/**
 * Hook for updating an existing indicator
 */
export const useUpdateIndicator = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: UpdateIndicatorRequest) => indicatorApi.updateIndicator(data),
    onSuccess: (updatedIndicator) => {
      // Update the specific indicator in cache
      queryClient.setQueryData(
        queryKeys.indicators.detail(updatedIndicator.indicatorId),
        updatedIndicator
      );

      // Invalidate lists to ensure consistency
      queryClient.invalidateQueries({ queryKey: queryKeys.indicators.lists() });
      queryClient.invalidateQueries({ queryKey: queryKeys.indicators.dashboard() });

      toast.success(`Indicator "${updatedIndicator.indicatorName}" updated successfully`);
    },
    onError: (error: any) => {
      const message = error.response?.data?.message || error.message || 'Failed to update indicator';
      toast.error(message);
    },
  });
};

/**
 * Hook for deleting an indicator
 */
export const useDeleteIndicator = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: number) => indicatorApi.deleteIndicator(id),
    onSuccess: (_, deletedId) => {
      // Remove from cache
      queryClient.removeQueries({ queryKey: queryKeys.indicators.detail(deletedId) });
      
      // Invalidate lists
      queryClient.invalidateQueries({ queryKey: queryKeys.indicators.lists() });
      queryClient.invalidateQueries({ queryKey: queryKeys.indicators.dashboard() });

      toast.success('Indicator deleted successfully');
    },
    onError: (error: any) => {
      const message = error.response?.data?.message || error.message || 'Failed to delete indicator';
      toast.error(message);
    },
  });
};

/**
 * Hook for executing an indicator manually
 */
export const useExecuteIndicator = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: TestIndicatorRequest) => indicatorApi.executeIndicator(request),
    onSuccess: (result, request) => {
      // Invalidate the specific indicator to refresh its status
      queryClient.invalidateQueries({ 
        queryKey: queryKeys.indicators.detail(request.indicatorId) 
      });
      
      // Invalidate dashboard for updated counts
      queryClient.invalidateQueries({ queryKey: queryKeys.indicators.dashboard() });

      if (result.isSuccess) {
        toast.success(`Indicator "${result.indicatorName}" executed successfully`);
      } else {
        toast.error(`Indicator execution failed: ${result.errorMessage || 'Unknown error'}`);
      }
    },
    onError: (error: any) => {
      const message = error.response?.data?.message || error.message || 'Failed to execute indicator';
      toast.error(message);
    },
  });
};
