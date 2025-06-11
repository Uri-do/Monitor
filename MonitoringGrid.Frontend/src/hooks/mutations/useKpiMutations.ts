import { useMutation, useQueryClient } from '@tanstack/react-query';
import { kpiApi } from '@/services/api';
import { queryKeys } from '@/utils/queryKeys';
import { CreateKpiRequest, UpdateKpiRequest, TestKpiRequest } from '@/types/api';
import toast from 'react-hot-toast';

/**
 * Hook for creating a new KPI
 */
export const useCreateKpi = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateKpiRequest) => kpiApi.createKpi(data),
    onSuccess: newKpi => {
      toast.success('KPI created successfully');

      // Invalidate KPI lists to show the new KPI
      queryClient.invalidateQueries({ queryKey: queryKeys.kpis.lists() });

      // Optionally set the new KPI in cache
      queryClient.setQueryData(queryKeys.kpis.detail(newKpi.kpiId), newKpi);

      // Update dashboard data
      queryClient.invalidateQueries({ queryKey: queryKeys.dashboard.all });
    },
    onError: (error: any) => {
      toast.error(error.message || 'Failed to create KPI');
    },
  });
};

/**
 * Hook for updating an existing KPI
 */
export const useUpdateKpi = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: UpdateKpiRequest) => kpiApi.updateKpi(data),
    onSuccess: (updatedKpi, variables) => {
      toast.success('KPI updated successfully');

      // Update the specific KPI in cache
      queryClient.setQueryData(queryKeys.kpis.detail(variables.kpiId), updatedKpi);

      // Invalidate KPI lists to reflect changes
      queryClient.invalidateQueries({ queryKey: queryKeys.kpis.lists() });

      // Update dashboard data
      queryClient.invalidateQueries({ queryKey: queryKeys.dashboard.all });
    },
    onError: (error: any) => {
      toast.error(error.message || 'Failed to update KPI');
    },
  });
};

/**
 * Hook for deleting a KPI
 */
export const useDeleteKpi = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (kpiId: number) => kpiApi.deleteKpi(kpiId),
    onSuccess: (_, kpiId) => {
      toast.success('KPI deleted successfully');

      // Remove the KPI from cache
      queryClient.removeQueries({ queryKey: queryKeys.kpis.detail(kpiId) });

      // Invalidate KPI lists
      queryClient.invalidateQueries({ queryKey: queryKeys.kpis.lists() });

      // Update dashboard data
      queryClient.invalidateQueries({ queryKey: queryKeys.dashboard.all });
    },
    onError: (error: any) => {
      toast.error(error.message || 'Failed to delete KPI');
    },
  });
};

/**
 * Hook for executing/testing a KPI
 */
export const useExecuteKpi = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: TestKpiRequest) => kpiApi.executeKpi(request),
    onSuccess: (result, variables) => {
      if (result.isSuccessful) {
        toast.success('KPI executed successfully');
      } else {
        toast.error(`KPI execution failed: ${result.errorMessage}`);
      }

      // Invalidate related queries to refresh execution data
      queryClient.invalidateQueries({ queryKey: queryKeys.kpis.detail(variables.kpiId) });
      queryClient.invalidateQueries({ queryKey: queryKeys.kpis.executions(variables.kpiId) });
      queryClient.invalidateQueries({ queryKey: queryKeys.kpis.analytics(variables.kpiId) });

      // Update KPI lists to reflect updated lastRun time
      queryClient.invalidateQueries({ queryKey: queryKeys.kpis.lists() });

      // Update dashboard data
      queryClient.invalidateQueries({ queryKey: queryKeys.dashboard.all });
    },
    onError: (error: any) => {
      toast.error(error.message || 'Failed to execute KPI');
    },
  });
};

/**
 * Hook for bulk KPI operations
 */
export const useBulkKpiOperation = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: kpiApi.bulkOperation,
    onSuccess: (_, variables) => {
      const operationType = variables.operation;
      const count = variables.kpiIds.length;

      toast.success(`${count} KPI${count > 1 ? 's' : ''} ${operationType} successfully`);

      // Invalidate all KPI-related queries
      queryClient.invalidateQueries({ queryKey: queryKeys.kpis.all });
      queryClient.invalidateQueries({ queryKey: queryKeys.dashboard.all });
    },
    onError: (error: any) => {
      toast.error(error.message || 'Failed to perform bulk operation');
    },
  });
};
