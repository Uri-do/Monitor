import React, { createContext, useContext, useEffect, useState, ReactNode } from 'react';
import {
  initializePerformanceMonitoring,
  getPerformanceMetrics,
  cleanupPerformanceMonitoring,
  checkPerformanceBudget,
  detectMemoryLeaks,
  PerformanceMetrics,
} from '@/utils/performance';

interface PerformanceContextType {
  metrics: Partial<PerformanceMetrics>;
  isMonitoring: boolean;
  budgetViolations: string[];
  startMonitoring: () => void;
  stopMonitoring: () => void;
  refreshMetrics: () => void;
}

const PerformanceContext = createContext<PerformanceContextType | undefined>(undefined);

interface PerformanceProviderProps {
  children: ReactNode;
  enableAutoMonitoring?: boolean;
  monitoringInterval?: number;
}

export const PerformanceProvider: React.FC<PerformanceProviderProps> = ({
  children,
  enableAutoMonitoring = true,
  monitoringInterval = 30000, // 30 seconds
}) => {
  const [metrics, setMetrics] = useState<Partial<PerformanceMetrics>>({});
  const [isMonitoring, setIsMonitoring] = useState(false);
  const [budgetViolations, setBudgetViolations] = useState<string[]>([]);

  const startMonitoring = () => {
    if (!isMonitoring) {
      initializePerformanceMonitoring();
      setIsMonitoring(true);
    }
  };

  const stopMonitoring = () => {
    if (isMonitoring) {
      cleanupPerformanceMonitoring();
      setIsMonitoring(false);
    }
  };

  const refreshMetrics = () => {
    const currentMetrics = getPerformanceMetrics();
    setMetrics(currentMetrics);
    
    // Check performance budget
    const violations = checkPerformanceBudget(currentMetrics);
    setBudgetViolations(violations);
    
    // Check for memory leaks
    detectMemoryLeaks();
  };

  useEffect(() => {
    if (enableAutoMonitoring) {
      startMonitoring();
    }

    return () => {
      stopMonitoring();
    };
  }, [enableAutoMonitoring]);

  useEffect(() => {
    if (isMonitoring) {
      // Initial metrics collection
      refreshMetrics();

      // Set up periodic metrics collection
      const interval = setInterval(refreshMetrics, monitoringInterval);

      return () => {
        clearInterval(interval);
      };
    }
  }, [isMonitoring, monitoringInterval]);

  // Report critical performance issues
  useEffect(() => {
    if (budgetViolations.length > 0) {
      console.warn('Performance budget violations detected:', budgetViolations);
      
      // In production, send to monitoring service
      if (process.env.NODE_ENV === 'production') {
        // Send to analytics/monitoring service
        // Example: analytics.track('performance_budget_violation', { violations: budgetViolations });
      }
    }
  }, [budgetViolations]);

  const value: PerformanceContextType = {
    metrics,
    isMonitoring,
    budgetViolations,
    startMonitoring,
    stopMonitoring,
    refreshMetrics,
  };

  return (
    <PerformanceContext.Provider value={value}>
      {children}
    </PerformanceContext.Provider>
  );
};

export const usePerformance = (): PerformanceContextType => {
  const context = useContext(PerformanceContext);
  if (context === undefined) {
    throw new Error('usePerformance must be used within a PerformanceProvider');
  }
  return context;
};

// Performance monitoring hook for components
export const useComponentPerformance = (componentName: string) => {
  const [renderTime, setRenderTime] = useState<number>(0);
  const [renderCount, setRenderCount] = useState<number>(0);

  useEffect(() => {
    const startTime = performance.now();
    setRenderCount(prev => prev + 1);

    return () => {
      const endTime = performance.now();
      const duration = endTime - startTime;
      setRenderTime(duration);

      if (duration > 16) { // More than one frame (60fps)
        console.warn(`Slow render detected in ${componentName}: ${duration.toFixed(2)}ms`);
      }
    };
  });

  return { renderTime, renderCount };
};

// Hook for measuring async operations
export const useAsyncPerformance = () => {
  const measureAsync = async <T,>(
    operation: () => Promise<T>,
    operationName: string
  ): Promise<T> => {
    const startTime = performance.now();
    
    try {
      const result = await operation();
      const endTime = performance.now();
      const duration = endTime - startTime;
      
      console.log(`${operationName} completed in ${duration.toFixed(2)}ms`);
      
      // Report slow operations
      if (duration > 1000) { // More than 1 second
        console.warn(`Slow operation detected: ${operationName} took ${duration.toFixed(2)}ms`);
      }
      
      return result;
    } catch (error) {
      const endTime = performance.now();
      const duration = endTime - startTime;
      console.error(`${operationName} failed after ${duration.toFixed(2)}ms:`, error);
      throw error;
    }
  };

  return { measureAsync };
};

// Performance debugging component
export const PerformanceDebugger: React.FC = () => {
  const { metrics, budgetViolations, isMonitoring } = usePerformance();

  if (process.env.NODE_ENV !== 'development') {
    return null;
  }

  return (
    <div
      style={{
        position: 'fixed',
        top: 10,
        right: 10,
        background: 'rgba(0, 0, 0, 0.8)',
        color: 'white',
        padding: '10px',
        borderRadius: '5px',
        fontSize: '12px',
        fontFamily: 'monospace',
        zIndex: 9999,
        maxWidth: '300px',
      }}
    >
      <div>Performance Monitor: {isMonitoring ? '🟢' : '🔴'}</div>
      {metrics.fcp && <div>FCP: {metrics.fcp.toFixed(0)}ms</div>}
      {metrics.lcp && <div>LCP: {metrics.lcp.toFixed(0)}ms</div>}
      {metrics.fid && <div>FID: {metrics.fid.toFixed(0)}ms</div>}
      {metrics.cls && <div>CLS: {metrics.cls.toFixed(3)}</div>}
      {metrics.ttfb && <div>TTFB: {metrics.ttfb.toFixed(0)}ms</div>}
      {budgetViolations.length > 0 && (
        <div style={{ color: 'red', marginTop: '5px' }}>
          Budget Violations: {budgetViolations.length}
        </div>
      )}
      {metrics.memoryUsage && (
        <div>
          Memory: {((metrics.memoryUsage.usedJSHeapSize || 0) / 1024 / 1024).toFixed(1)}MB
        </div>
      )}
    </div>
  );
};
