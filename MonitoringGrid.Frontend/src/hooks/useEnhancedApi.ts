import { useState, useEffect, useCallback } from 'react';
import { analyticsApi, realtimeApi, enhancedAlertApi } from '@/services/api';
import {
  SystemAnalyticsDto,
  KpiPerformanceAnalyticsDto,
  OwnerAnalyticsDto,
  SystemHealthDto,
  RealtimeStatusDto,
  LiveDashboardDto,
  EnhancedAlertDto,
  AlertStatisticsDto,
} from '@/types/api';

// Hook for system analytics
export const useSystemAnalytics = (days: number = 30) => {
  const [data, setData] = useState<SystemAnalyticsDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchData = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const analytics = await analyticsApi.getSystemAnalytics(days);
      setData(analytics);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to fetch system analytics');
    } finally {
      setLoading(false);
    }
  }, [days]);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  return { data, loading, error, refetch: fetchData };
};

// Hook for KPI performance analytics
export const useKpiPerformanceAnalytics = (kpiId: number, days: number = 30) => {
  const [data, setData] = useState<KpiPerformanceAnalyticsDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchData = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const analytics = await analyticsApi.getKpiPerformanceAnalytics(kpiId, days);
      setData(analytics);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to fetch KPI performance analytics');
    } finally {
      setLoading(false);
    }
  }, [kpiId, days]);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  return { data, loading, error, refetch: fetchData };
};

// Hook for owner analytics
export const useOwnerAnalytics = (days: number = 30) => {
  const [data, setData] = useState<OwnerAnalyticsDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchData = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const analytics = await analyticsApi.getOwnerAnalytics(days);
      setData(analytics);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to fetch owner analytics');
    } finally {
      setLoading(false);
    }
  }, [days]);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  return { data, loading, error, refetch: fetchData };
};

// Hook for system health
export const useSystemHealth = (autoRefresh: boolean = true, interval: number = 30000) => {
  const [data, setData] = useState<SystemHealthDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchData = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const health = await analyticsApi.getSystemHealth();
      setData(health);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to fetch system health');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchData();

    if (autoRefresh) {
      const intervalId = setInterval(fetchData, interval);
      return () => clearInterval(intervalId);
    }
  }, [fetchData, autoRefresh, interval]);

  return { data, loading, error, refetch: fetchData };
};

// Hook for real-time status
export const useRealtimeStatus = (autoRefresh: boolean = true, interval: number = 5000) => {
  const [data, setData] = useState<RealtimeStatusDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchData = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const status = await realtimeApi.getRealtimeStatus();
      setData(status);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to fetch real-time status');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchData();

    if (autoRefresh) {
      const intervalId = setInterval(fetchData, interval);
      return () => clearInterval(intervalId);
    }
  }, [fetchData, autoRefresh, interval]);

  return { data, loading, error, refetch: fetchData };
};

// Hook for live dashboard
export const useLiveDashboard = (autoRefresh: boolean = true, interval: number = 10000) => {
  const [data, setData] = useState<LiveDashboardDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchData = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const dashboard = await realtimeApi.getLiveDashboard();
      setData(dashboard);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to fetch live dashboard');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchData();

    if (autoRefresh) {
      const intervalId = setInterval(fetchData, interval);
      return () => clearInterval(intervalId);
    }
  }, [fetchData, autoRefresh, interval]);

  return { data, loading, error, refetch: fetchData };
};

// Hook for critical alerts
export const useCriticalAlerts = (autoRefresh: boolean = true, interval: number = 15000) => {
  const [data, setData] = useState<EnhancedAlertDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchData = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const alerts = await enhancedAlertApi.getCriticalAlerts();
      setData(alerts);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to fetch critical alerts');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchData();

    if (autoRefresh) {
      const intervalId = setInterval(fetchData, interval);
      return () => clearInterval(intervalId);
    }
  }, [fetchData, autoRefresh, interval]);

  return { data, loading, error, refetch: fetchData };
};

// Hook for unresolved alerts
export const useUnresolvedAlerts = (autoRefresh: boolean = true, interval: number = 20000) => {
  const [data, setData] = useState<EnhancedAlertDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchData = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const alerts = await enhancedAlertApi.getUnresolvedAlerts();
      setData(alerts);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to fetch unresolved alerts');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchData();

    if (autoRefresh) {
      const intervalId = setInterval(fetchData, interval);
      return () => clearInterval(intervalId);
    }
  }, [fetchData, autoRefresh, interval]);

  return { data, loading, error, refetch: fetchData };
};

// Hook for enhanced alert statistics
export const useEnhancedAlertStatistics = (days: number = 30) => {
  const [data, setData] = useState<AlertStatisticsDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchData = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const statistics = await enhancedAlertApi.getEnhancedStatistics(days);
      setData(statistics);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to fetch enhanced alert statistics');
    } finally {
      setLoading(false);
    }
  }, [days]);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  return { data, loading, error, refetch: fetchData };
};

// Hook for real-time KPI execution
export const useRealtimeKpiExecution = () => {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const executeKpi = useCallback(async (kpiId: number) => {
    try {
      setLoading(true);
      setError(null);
      const result = await realtimeApi.executeKpiRealtime(kpiId);
      return result;
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to execute KPI';
      setError(errorMessage);
      throw new Error(errorMessage);
    } finally {
      setLoading(false);
    }
  }, []);

  return { executeKpi, loading, error };
};

// Hook for manual alert creation
export const useManualAlert = () => {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const sendAlert = useCallback(
    async (kpiId: number, message: string, details?: string, priority: number = 2) => {
      try {
        setLoading(true);
        setError(null);
        const result = await enhancedAlertApi.sendManualAlert({
          kpiId,
          message,
          details,
          priority,
        });
        return result;
      } catch (err) {
        const errorMessage = err instanceof Error ? err.message : 'Failed to send manual alert';
        setError(errorMessage);
        throw new Error(errorMessage);
      } finally {
        setLoading(false);
      }
    },
    []
  );

  return { sendAlert, loading, error };
};
