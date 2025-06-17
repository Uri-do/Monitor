import React, { memo, useMemo, useCallback, useRef, useEffect } from 'react';

/**
 * Component Optimization Utilities
 * Advanced React optimization patterns and utilities
 */

// Enhanced memo with custom comparison
export function createOptimizedComponent<P extends object>(
  Component: React.ComponentType<P>,
  customCompare?: (prevProps: P, nextProps: P) => boolean
): React.ComponentType<P> {
  const OptimizedComponent = memo(Component, customCompare);
  OptimizedComponent.displayName = `Optimized(${Component.displayName || Component.name})`;
  return OptimizedComponent;
}

// Shallow comparison utility
export function shallowEqual<T extends Record<string, any>>(obj1: T, obj2: T): boolean {
  const keys1 = Object.keys(obj1);
  const keys2 = Object.keys(obj2);

  if (keys1.length !== keys2.length) {
    return false;
  }

  for (const key of keys1) {
    if (obj1[key] !== obj2[key]) {
      return false;
    }
  }

  return true;
}

// Deep comparison for complex objects
export function deepEqual(obj1: any, obj2: any): boolean {
  if (obj1 === obj2) {
    return true;
  }

  if (obj1 == null || obj2 == null) {
    return obj1 === obj2;
  }

  if (typeof obj1 !== typeof obj2) {
    return false;
  }

  if (typeof obj1 !== 'object') {
    return obj1 === obj2;
  }

  if (Array.isArray(obj1) !== Array.isArray(obj2)) {
    return false;
  }

  const keys1 = Object.keys(obj1);
  const keys2 = Object.keys(obj2);

  if (keys1.length !== keys2.length) {
    return false;
  }

  for (const key of keys1) {
    if (!keys2.includes(key) || !deepEqual(obj1[key], obj2[key])) {
      return false;
    }
  }

  return true;
}

// Optimized callback hook with dependency tracking
export function useOptimizedCallback<T extends (...args: any[]) => any>(
  callback: T,
  deps: React.DependencyList
): T {
  const callbackRef = useRef(callback);
  const depsRef = useRef(deps);

  // Update callback if dependencies changed
  if (!shallowEqual(depsRef.current, deps)) {
    callbackRef.current = callback;
    depsRef.current = deps;
  }

  return useCallback(callbackRef.current, deps);
}

// Optimized memo hook with custom comparison
export function useOptimizedMemo<T>(
  factory: () => T,
  deps: React.DependencyList,
  compare: (prev: React.DependencyList, next: React.DependencyList) => boolean = shallowEqual
): T {
  const valueRef = useRef<T>();
  const depsRef = useRef<React.DependencyList>();

  if (!depsRef.current || !compare(depsRef.current, deps)) {
    valueRef.current = factory();
    depsRef.current = deps;
  }

  return valueRef.current!;
}

// Debounced value hook
export function useDebouncedValue<T>(value: T, delay: number): T {
  const [debouncedValue, setDebouncedValue] = React.useState(value);

  useEffect(() => {
    const handler = setTimeout(() => {
      setDebouncedValue(value);
    }, delay);

    return () => {
      clearTimeout(handler);
    };
  }, [value, delay]);

  return debouncedValue;
}

// Throttled callback hook
export function useThrottledCallback<T extends (...args: any[]) => any>(
  callback: T,
  delay: number
): T {
  const lastRun = useRef(Date.now());

  return useCallback(
    ((...args: any[]) => {
      if (Date.now() - lastRun.current >= delay) {
        callback(...args);
        lastRun.current = Date.now();
      }
    }) as T,
    [callback, delay]
  );
}

// Virtual scrolling hook
export function useVirtualScrolling<T>(
  items: T[],
  itemHeight: number,
  containerHeight: number
) {
  const [scrollTop, setScrollTop] = React.useState(0);

  const visibleStart = Math.floor(scrollTop / itemHeight);
  const visibleEnd = Math.min(
    visibleStart + Math.ceil(containerHeight / itemHeight) + 1,
    items.length
  );

  const visibleItems = items.slice(visibleStart, visibleEnd);
  const totalHeight = items.length * itemHeight;
  const offsetY = visibleStart * itemHeight;

  return {
    visibleItems,
    totalHeight,
    offsetY,
    setScrollTop,
    visibleStart,
    visibleEnd,
  };
}

// Intersection observer hook for lazy rendering
export function useIntersectionObserver(
  options: IntersectionObserverInit = {}
): [React.RefObject<HTMLElement>, boolean] {
  const [isIntersecting, setIsIntersecting] = React.useState(false);
  const ref = useRef<HTMLElement>(null);

  useEffect(() => {
    const observer = new IntersectionObserver(([entry]) => {
      setIsIntersecting(entry.isIntersecting);
    }, options);

    if (ref.current) {
      observer.observe(ref.current);
    }

    return () => {
      if (ref.current) {
        observer.unobserve(ref.current);
      }
    };
  }, [options]);

  return [ref, isIntersecting];
}

// Component render tracking
export function useRenderTracking(componentName: string) {
  const renderCount = useRef(0);
  const lastRenderTime = useRef(Date.now());

  renderCount.current += 1;
  const currentTime = Date.now();
  const timeSinceLastRender = currentTime - lastRenderTime.current;
  lastRenderTime.current = currentTime;

  useEffect(() => {
    if (process.env.NODE_ENV === 'development') {
      console.log(`${componentName} rendered ${renderCount.current} times. Time since last render: ${timeSinceLastRender}ms`);
    }
  });

  return {
    renderCount: renderCount.current,
    timeSinceLastRender,
  };
}

// Optimized list component
interface OptimizedListProps<T> {
  items: T[];
  renderItem: (item: T, index: number) => React.ReactNode;
  keyExtractor: (item: T, index: number) => string | number;
  itemHeight?: number;
  containerHeight?: number;
  enableVirtualization?: boolean;
}

export function OptimizedList<T>({
  items,
  renderItem,
  keyExtractor,
  itemHeight = 50,
  containerHeight = 400,
  enableVirtualization = true,
}: OptimizedListProps<T>) {
  const virtualScrolling = useVirtualScrolling(
    items,
    itemHeight,
    containerHeight
  );

  const handleScroll = useCallback((e: React.UIEvent<HTMLDivElement>) => {
    virtualScrolling.setScrollTop(e.currentTarget.scrollTop);
  }, [virtualScrolling]);

  if (!enableVirtualization || items.length < 50) {
    // Render all items for small lists
    return (
      <div style={{ height: containerHeight, overflowY: 'auto' }}>
        {items.map((item, index) => (
          <div key={keyExtractor(item, index)}>
            {renderItem(item, index)}
          </div>
        ))}
      </div>
    );
  }

  // Virtual scrolling for large lists
  return (
    <div
      style={{ height: containerHeight, overflowY: 'auto' }}
      onScroll={handleScroll}
    >
      <div style={{ height: virtualScrolling.totalHeight, position: 'relative' }}>
        <div
          style={{
            transform: `translateY(${virtualScrolling.offsetY}px)`,
            position: 'absolute',
            top: 0,
            left: 0,
            right: 0,
          }}
        >
          {virtualScrolling.visibleItems.map((item, index) => (
            <div
              key={keyExtractor(item, virtualScrolling.visibleStart + index)}
              style={{ height: itemHeight }}
            >
              {renderItem(item, virtualScrolling.visibleStart + index)}
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}

// Performance monitoring component
interface PerformanceMonitorProps {
  children: React.ReactNode;
  componentName: string;
  enableLogging?: boolean;
}

export const PerformanceMonitor: React.FC<PerformanceMonitorProps> = ({
  children,
  componentName,
  enableLogging = process.env.NODE_ENV === 'development',
}) => {
  const renderTracking = useRenderTracking(componentName);

  if (enableLogging && renderTracking.renderCount > 10) {
    console.warn(`${componentName} has rendered ${renderTracking.renderCount} times. Consider optimization.`);
  }

  return <>{children}</>;
};

// HOC for automatic performance monitoring
export function withPerformanceMonitoring<P extends object>(
  Component: React.ComponentType<P>,
  componentName?: string
): React.ComponentType<P> {
  const displayName = componentName || Component.displayName || Component.name || 'Component';
  
  return (props: P) => (
    <PerformanceMonitor componentName={displayName}>
      <Component {...props} />
    </PerformanceMonitor>
  );
}
