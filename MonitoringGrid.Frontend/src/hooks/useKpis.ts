import { useState, useEffect, useCallback } from 'react';
import { kpiApi } from '@/services/api';
import {
  KpiDto,
  CreateKpiRequest,
  UpdateKpiRequest,
  TestKpiRequest,
  KpiExecutionResultDto,
} from '@/types/api';

export const useKpis = (filters?: { isActive?: boolean; owner?: string; priority?: number }) => {
  const [data, setData] = useState<KpiDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchData = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const kpis = await kpiApi.getKpis(filters);
      setData(kpis);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to fetch KPIs');
    } finally {
      setLoading(false);
    }
  }, [filters]);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  const createKpi = useCallback(async (kpi: CreateKpiRequest) => {
    try {
      const newKpi = await kpiApi.createKpi(kpi);
      setData(prev => [newKpi, ...prev]);
      return newKpi;
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to create KPI';
      setError(errorMessage);
      throw new Error(errorMessage);
    }
  }, []);

  const updateKpi = useCallback(async (kpi: UpdateKpiRequest) => {
    try {
      const updatedKpi = await kpiApi.updateKpi(kpi);
      setData(prev => prev.map(k => (k.kpiId === kpi.kpiId ? updatedKpi : k)));
      return updatedKpi;
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to update KPI';
      setError(errorMessage);
      throw new Error(errorMessage);
    }
  }, []);

  const deleteKpi = useCallback(async (id: number) => {
    try {
      await kpiApi.deleteKpi(id);
      setData(prev => prev.filter(k => k.kpiId !== id));
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to delete KPI';
      setError(errorMessage);
      throw new Error(errorMessage);
    }
  }, []);

  const executeKpi = useCallback(
    async (id: number, customFrequency?: number): Promise<KpiExecutionResultDto> => {
      try {
        const request: TestKpiRequest = { kpiId: id, customFrequency };
        const result = await kpiApi.executeKpi(request);
        // Optionally refresh the KPI list to get updated lastRun time
        fetchData();
        return result;
      } catch (err) {
        const errorMessage = err instanceof Error ? err.message : 'Failed to execute KPI';
        setError(errorMessage);
        throw new Error(errorMessage);
      }
    },
    [fetchData]
  );

  return {
    data,
    loading,
    error,
    refetch: fetchData,
    createKpi,
    updateKpi,
    deleteKpi,
    executeKpi,
  };
};
