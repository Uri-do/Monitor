import { useEffect, useRef, useState } from 'react';
import { useQueryClient } from '@tanstack/react-query';

interface PerformanceMetrics {
  renderTime: number;
  queryCount: number;
  cacheHitRate: number;
  memoryUsage: number;
  networkRequests: number;
  errorCount: number;
  lastUpdate: Date;
}

interface PerformanceAlert {
  type: 'warning' | 'error';
  message: string;
  metric: keyof PerformanceMetrics;
  value: number;
  threshold: number;
  timestamp: Date;
}

interface PerformanceThresholds {
  renderTime: number; // ms
  queryCount: number;
  cacheHitRate: number; // percentage
  memoryUsage: number; // MB
  networkRequests: number;
  errorCount: number;
}

const DEFAULT_THRESHOLDS: PerformanceThresholds = {
  renderTime: 100, // 100ms
  queryCount: 50,
  cacheHitRate: 80, // 80%
  memoryUsage: 100, // 100MB
  networkRequests: 20,
  errorCount: 5,
};

/**
 * Enhanced performance monitoring hook for React Query and component performance
 */
export const usePerformanceMonitor = (
  componentName?: string,
  thresholds: Partial<PerformanceThresholds> = {}
) => {
  const queryClient = useQueryClient();
  const [metrics, setMetrics] = useState<PerformanceMetrics>({
    renderTime: 0,
    queryCount: 0,
    cacheHitRate: 0,
    memoryUsage: 0,
    networkRequests: 0,
    errorCount: 0,
    lastUpdate: new Date(),
  });
  const [alerts, setAlerts] = useState<PerformanceAlert[]>([]);
  const renderStartTime = useRef<number>(0);
  const networkRequestCount = useRef<number>(0);
  const errorCount = useRef<number>(0);

  const finalThresholds = { ...DEFAULT_THRESHOLDS, ...thresholds };

  // Monitor render performance
  useEffect(() => {
    renderStartTime.current = performance.now();
    
    return () => {
      const renderTime = performance.now() - renderStartTime.current;
      setMetrics(prev => ({ ...prev, renderTime, lastUpdate: new Date() }));
      
      // Check render time threshold
      if (renderTime > finalThresholds.renderTime) {
        addAlert('warning', 'Slow render detected', 'renderTime', renderTime, finalThresholds.renderTime);
      }
    };
  });

  // Monitor React Query cache performance
  useEffect(() => {
    const updateQueryMetrics = () => {
      const cache = queryClient.getQueryCache();
      const queries = cache.getAll();
      
      const queryCount = queries.length;
      const staleQueries = queries.filter(query => query.isStale()).length;
      const cacheHitRate = queryCount > 0 ? ((queryCount - staleQueries) / queryCount) * 100 : 100;
      
      setMetrics(prev => ({
        ...prev,
        queryCount,
        cacheHitRate,
        lastUpdate: new Date(),
      }));

      // Check thresholds
      if (queryCount > finalThresholds.queryCount) {
        addAlert('warning', 'High query count detected', 'queryCount', queryCount, finalThresholds.queryCount);
      }
      
      if (cacheHitRate < finalThresholds.cacheHitRate) {
        addAlert('warning', 'Low cache hit rate', 'cacheHitRate', cacheHitRate, finalThresholds.cacheHitRate);
      }
    };

    updateQueryMetrics();
    const interval = setInterval(updateQueryMetrics, 5000); // Update every 5 seconds
    
    return () => clearInterval(interval);
  }, [queryClient, finalThresholds]);

  // Monitor memory usage
  useEffect(() => {
    const updateMemoryMetrics = () => {
      if ('memory' in performance) {
        const memory = (performance as any).memory;
        const memoryUsage = memory.usedJSHeapSize / (1024 * 1024); // Convert to MB
        
        setMetrics(prev => ({
          ...prev,
          memoryUsage,
          lastUpdate: new Date(),
        }));

        if (memoryUsage > finalThresholds.memoryUsage) {
          addAlert('error', 'High memory usage detected', 'memoryUsage', memoryUsage, finalThresholds.memoryUsage);
        }
      }
    };

    updateMemoryMetrics();
    const interval = setInterval(updateMemoryMetrics, 10000); // Update every 10 seconds
    
    return () => clearInterval(interval);
  }, [finalThresholds]);

  // Monitor network requests
  const trackNetworkRequest = () => {
    networkRequestCount.current++;
    setMetrics(prev => ({
      ...prev,
      networkRequests: networkRequestCount.current,
      lastUpdate: new Date(),
    }));

    if (networkRequestCount.current > finalThresholds.networkRequests) {
      addAlert('warning', 'High network request count', 'networkRequests', networkRequestCount.current, finalThresholds.networkRequests);
    }
  };

  // Monitor errors
  const trackError = (error: Error) => {
    errorCount.current++;
    setMetrics(prev => ({
      ...prev,
      errorCount: errorCount.current,
      lastUpdate: new Date(),
    }));

    if (errorCount.current > finalThresholds.errorCount) {
      addAlert('error', 'High error count detected', 'errorCount', errorCount.current, finalThresholds.errorCount);
    }

    // Log error for debugging
    console.error(`[${componentName || 'Component'}] Performance Monitor Error:`, error);
  };

  const addAlert = (
    type: 'warning' | 'error',
    message: string,
    metric: keyof PerformanceMetrics,
    value: number,
    threshold: number
  ) => {
    const alert: PerformanceAlert = {
      type,
      message,
      metric,
      value,
      threshold,
      timestamp: new Date(),
    };

    setAlerts(prev => {
      // Prevent duplicate alerts for the same metric within 30 seconds
      const recentAlert = prev.find(
        a => a.metric === metric && 
        Date.now() - a.timestamp.getTime() < 30000
      );
      
      if (recentAlert) return prev;
      
      // Keep only last 10 alerts
      return [alert, ...prev.slice(0, 9)];
    });

    // Log alert for debugging
    console.warn(`[${componentName || 'Component'}] Performance Alert:`, alert);
  };

  const clearAlerts = () => {
    setAlerts([]);
  };

  const resetMetrics = () => {
    networkRequestCount.current = 0;
    errorCount.current = 0;
    setMetrics({
      renderTime: 0,
      queryCount: 0,
      cacheHitRate: 0,
      memoryUsage: 0,
      networkRequests: 0,
      errorCount: 0,
      lastUpdate: new Date(),
    });
    clearAlerts();
  };

  // Get performance score (0-100)
  const getPerformanceScore = (): number => {
    const scores = [
      Math.max(0, 100 - (metrics.renderTime / finalThresholds.renderTime) * 100),
      Math.max(0, 100 - (metrics.queryCount / finalThresholds.queryCount) * 100),
      Math.min(100, metrics.cacheHitRate),
      Math.max(0, 100 - (metrics.memoryUsage / finalThresholds.memoryUsage) * 100),
      Math.max(0, 100 - (metrics.networkRequests / finalThresholds.networkRequests) * 100),
      Math.max(0, 100 - (metrics.errorCount / finalThresholds.errorCount) * 100),
    ];

    return Math.round(scores.reduce((sum, score) => sum + score, 0) / scores.length);
  };

  // Get performance status
  const getPerformanceStatus = (): 'excellent' | 'good' | 'fair' | 'poor' => {
    const score = getPerformanceScore();
    if (score >= 90) return 'excellent';
    if (score >= 75) return 'good';
    if (score >= 60) return 'fair';
    return 'poor';
  };

  return {
    metrics,
    alerts,
    performanceScore: getPerformanceScore(),
    performanceStatus: getPerformanceStatus(),
    trackNetworkRequest,
    trackError,
    clearAlerts,
    resetMetrics,
    thresholds: finalThresholds,
  };
};
