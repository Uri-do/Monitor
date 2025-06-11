import { useMutation, useQueryClient } from '@tanstack/react-query';
import { alertApi } from '@/services/api';
import { queryKeys } from '@/utils/queryKeys';
import { ResolveAlertRequest, BulkResolveAlertsRequest } from '@/types/api';
import toast from 'react-hot-toast';

/**
 * Hook for resolving a single alert
 */
export const useResolveAlert = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: ResolveAlertRequest) => alertApi.resolveAlert(request),
    onSuccess: (_, variables) => {
      toast.success('Alert resolved successfully');

      // Invalidate and refetch alert lists
      queryClient.invalidateQueries({ queryKey: queryKeys.alerts.lists() });

      // Update the specific alert in cache if we have the alertId
      if (variables.alertId) {
        queryClient.invalidateQueries({ queryKey: queryKeys.alerts.detail(variables.alertId) });
      }

      // Update dashboard data
      queryClient.invalidateQueries({ queryKey: queryKeys.dashboard.all });
      queryClient.invalidateQueries({ queryKey: queryKeys.alerts.statistics() });
    },
    onError: (error: any) => {
      toast.error(error.message || 'Failed to resolve alert');
    },
  });
};

/**
 * Hook for bulk resolving multiple alerts
 */
export const useBulkResolveAlerts = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: BulkResolveAlertsRequest) => alertApi.bulkResolveAlerts(request),
    onSuccess: (_, variables) => {
      const count = variables.alertIds.length;
      toast.success(`${count} alert${count > 1 ? 's' : ''} resolved successfully`);

      // Invalidate all alert-related queries
      queryClient.invalidateQueries({ queryKey: queryKeys.alerts.all });
      queryClient.invalidateQueries({ queryKey: queryKeys.dashboard.all });
      queryClient.invalidateQueries({ queryKey: queryKeys.alerts.statistics() });
    },
    onError: (error: any) => {
      toast.error(error.message || 'Failed to bulk resolve alerts');
    },
  });
};

/**
 * Hook for creating manual alerts
 */
export const useCreateManualAlert = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: alertApi.createManualAlert,
    onSuccess: () => {
      toast.success('Manual alert created successfully');

      // Invalidate alert lists to show the new alert
      queryClient.invalidateQueries({ queryKey: queryKeys.alerts.lists() });
      queryClient.invalidateQueries({ queryKey: queryKeys.dashboard.all });
      queryClient.invalidateQueries({ queryKey: queryKeys.alerts.statistics() });
    },
    onError: (error: any) => {
      toast.error(error.message || 'Failed to create manual alert');
    },
  });
};
