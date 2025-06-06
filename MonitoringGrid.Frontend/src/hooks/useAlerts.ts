import { useState, useEffect, useCallback } from 'react';
import { alertApi } from '@/services/api';
import { 
  AlertLogDto, 
  AlertFilterDto, 
  PaginatedAlertsDto, 
  ResolveAlertRequest, 
  BulkResolveAlertsRequest 
} from '@/types/api';

export const useAlerts = (filters: AlertFilterDto) => {
  const [data, setData] = useState<PaginatedAlertsDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchData = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const alerts = await alertApi.getAlerts(filters);
      setData(alerts);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to fetch alerts');
    } finally {
      setLoading(false);
    }
  }, [filters]);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  const resolveAlert = useCallback(async (alertId: number, request: ResolveAlertRequest) => {
    try {
      await alertApi.resolveAlert(request);
      // Update the local state to mark alert as resolved
      setData(prev => {
        if (!prev) return prev;
        return {
          ...prev,
          alerts: prev.alerts.map(alert => 
            alert.alertId === alertId 
              ? { ...alert, isResolved: true, resolvedTime: new Date().toISOString(), resolvedBy: request.resolvedBy }
              : alert
          )
        };
      });
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to resolve alert';
      setError(errorMessage);
      throw new Error(errorMessage);
    }
  }, []);

  const bulkResolveAlerts = useCallback(async (request: BulkResolveAlertsRequest) => {
    try {
      await alertApi.bulkResolveAlerts(request);
      // Update the local state to mark alerts as resolved
      setData(prev => {
        if (!prev) return prev;
        return {
          ...prev,
          alerts: prev.alerts.map(alert => 
            request.alertIds.includes(alert.alertId)
              ? { ...alert, isResolved: true, resolvedTime: new Date().toISOString(), resolvedBy: request.resolvedBy }
              : alert
          )
        };
      });
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to bulk resolve alerts';
      setError(errorMessage);
      throw new Error(errorMessage);
    }
  }, []);

  const getAlert = useCallback(async (id: number): Promise<AlertLogDto> => {
    try {
      const alert = await alertApi.getAlert(id);
      return alert;
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to fetch alert';
      setError(errorMessage);
      throw new Error(errorMessage);
    }
  }, []);

  return {
    data,
    loading,
    error,
    refetch: fetchData,
    resolveAlert,
    bulkResolveAlerts,
    getAlert
  };
};
