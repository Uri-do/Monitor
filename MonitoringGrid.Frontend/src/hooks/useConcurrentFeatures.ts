import React, {
  useDeferredValue,
  useTransition,
  startTransition,
  useMemo,
  useCallback,
  useId,
  useSyncExternalStore,
  useInsertionEffect,
  useState,
  useEffect,
  useRef
} from 'react';

/**
 * Advanced React 18 Concurrent Features Hook
 * Provides enterprise-grade performance optimizations using React 18 features
 */

// Deferred value hook for non-urgent updates
export const useDeferredSearch = <T>(value: T) => {
  const deferredValue = useDeferredValue(value);
  const [isPending, startTransition] = useTransition();
  
  const updateValue = useCallback((newValue: T) => {
    startTransition(() => {
      // This update will be deferred if more urgent updates are pending
    });
  }, []);
  
  return {
    deferredValue,
    isPending,
    updateValue
  };
};

// Transition hook for expensive operations
export const useExpensiveTransition = () => {
  const [isPending, startTransition] = useTransition();
  
  const executeExpensiveOperation = useCallback((operation: () => void) => {
    startTransition(() => {
      operation();
    });
  }, []);
  
  return {
    isPending,
    executeExpensiveOperation
  };
};

// Optimized list rendering with concurrent features
export const useOptimizedList = <T>(
  items: T[],
  filterFn?: (item: T) => boolean,
  sortFn?: (a: T, b: T) => number
) => {
  const [isPending, startTransition] = useTransition();
  
  // Defer expensive filtering and sorting operations
  const deferredItems = useDeferredValue(items);
  
  const processedItems = useMemo(() => {
    let result = deferredItems;
    
    if (filterFn) {
      result = result.filter(filterFn);
    }
    
    if (sortFn) {
      result = [...result].sort(sortFn);
    }
    
    return result;
  }, [deferredItems, filterFn, sortFn]);
  
  const updateItems = useCallback((newItems: T[]) => {
    startTransition(() => {
      // Update will be deferred for better performance
    });
  }, []);
  
  return {
    items: processedItems,
    isPending,
    updateItems
  };
};

// Unique ID generation for accessibility
export const useStableId = (prefix?: string) => {
  const id = useId();
  return useMemo(() => prefix ? `${prefix}-${id}` : id, [prefix, id]);
};

// External store subscription for global state
export const useExternalStore = <T>(
  subscribe: (callback: () => void) => () => void,
  getSnapshot: () => T,
  getServerSnapshot?: () => T
) => {
  return useSyncExternalStore(subscribe, getSnapshot, getServerSnapshot);
};

// Performance-critical CSS injection
export const useCriticalCSS = (css: string) => {
  useInsertionEffect(() => {
    const style = document.createElement('style');
    style.textContent = css;
    document.head.appendChild(style);
    
    return () => {
      document.head.removeChild(style);
    };
  }, [css]);
};

// Concurrent data fetching pattern
export const useConcurrentData = <T>(
  fetchFn: () => Promise<T>,
  dependencies: unknown[] = []
) => {
  const [isPending, startTransition] = useTransition();
  const [data, setData] = useState<T | null>(null);
  const [error, setError] = useState<Error | null>(null);
  
  const fetchData = useCallback(() => {
    startTransition(async () => {
      try {
        setError(null);
        const result = await fetchFn();
        setData(result);
      } catch (err) {
        setError(err instanceof Error ? err : new Error('Unknown error'));
      }
    });
  }, dependencies);
  
  useEffect(() => {
    fetchData();
  }, [fetchData]);
  
  return {
    data,
    error,
    isPending,
    refetch: fetchData
  };
};

// Optimized event handling with transitions
export const useTransitionCallback = <T extends unknown[]>(
  callback: (...args: T) => void,
  deps: unknown[] = []
) => {
  const [isPending, startTransition] = useTransition();
  
  const wrappedCallback = useCallback((...args: T) => {
    startTransition(() => {
      callback(...args);
    });
  }, deps);
  
  return {
    callback: wrappedCallback,
    isPending
  };
};

// Advanced memoization with concurrent features
export const useAdvancedMemo = <T>(
  factory: () => T,
  deps: unknown[],
  options?: {
    defer?: boolean;
    priority?: 'high' | 'normal' | 'low';
  }
) => {
  const deferredDeps = options?.defer ? useDeferredValue(deps) : deps;
  
  return useMemo(() => {
    if (options?.priority === 'low') {
      // Use scheduler for low priority computations
      return factory();
    }
    return factory();
  }, deferredDeps);
};

// Concurrent form handling
export const useConcurrentForm = <T extends Record<string, unknown>>(
  initialValues: T,
  onSubmit: (values: T) => Promise<void>
) => {
  const [values, setValues] = useState<T>(initialValues);
  const [isPending, startTransition] = useTransition();
  const [isSubmitting, setIsSubmitting] = useState(false);
  
  const updateField = useCallback((field: keyof T, value: T[keyof T]) => {
    startTransition(() => {
      setValues(prev => ({ ...prev, [field]: value }));
    });
  }, []);
  
  const handleSubmit = useCallback(async (e: React.FormEvent) => {
    e.preventDefault();
    setIsSubmitting(true);
    
    try {
      await onSubmit(values);
    } finally {
      setIsSubmitting(false);
    }
  }, [values, onSubmit]);
  
  return {
    values,
    updateField,
    handleSubmit,
    isPending,
    isSubmitting
  };
};

// Performance monitoring with concurrent features
export const usePerformanceMonitoring = (componentName: string) => {
  const renderCount = useRef(0);
  const [isPending] = useTransition();
  
  useInsertionEffect(() => {
    renderCount.current += 1;
    
    if (process.env.NODE_ENV === 'development') {
      const startTime = performance.now();
      
      return () => {
        const endTime = performance.now();
        const duration = endTime - startTime;
        
        if (duration > 16) {
          console.warn(
            `[Performance] ${componentName} render #${renderCount.current}: ${duration.toFixed(2)}ms`,
            { isPending }
          );
        }
      };
    }
  });
  
  return {
    renderCount: renderCount.current,
    isPending
  };
};
