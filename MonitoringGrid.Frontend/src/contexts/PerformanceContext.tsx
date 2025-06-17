import React, { createContext, useContext, useEffect, useState, useRef, ReactNode } from 'react';
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

// Performance monitoring hook for components - optimized to avoid unnecessary re-renders
export const useComponentPerformance = (componentName: string) => {
  const renderCountRef = useRef<number>(0);
  const lastRenderTimeRef = useRef<number>(0);

  useEffect(() => {
    const startTime = performance.now();
    renderCountRef.current += 1;

    return () => {
      const endTime = performance.now();
      const duration = endTime - startTime;
      lastRenderTimeRef.current = duration;

      // Only warn in development and for significant delays
      if (process.env.NODE_ENV === 'development' && duration > 16) {
        console.warn(`Slow render detected in ${componentName}: ${duration.toFixed(2)}ms (render #${renderCountRef.current})`);
      }
    };
  });

  return {
    get renderTime() { return lastRenderTimeRef.current; },
    get renderCount() { return renderCountRef.current; }
  };
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

// Performance debugging component - REMOVED FOR SECURITY
// This component has been removed to prevent information disclosure in production
// Performance monitoring is still available through internal metrics but not exposed in UI
export const PerformanceDebugger: React.FC = () => {
  // Always return null - performance panels removed for security hardening
  return null;
};
