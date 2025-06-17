import React, { Suspense, lazy, ComponentType } from 'react';
import { Box, CircularProgress, Typography } from '@mui/material';
import { ErrorBoundary } from '@/components/Common';

/**
 * Modern Lazy Loading Utilities
 * Enhanced lazy loading with better error handling and loading states
 */

// Enhanced loading fallback component
interface LoadingFallbackProps {
  message?: string;
  minHeight?: number;
  showProgress?: boolean;
}

const LoadingFallback: React.FC<LoadingFallbackProps> = ({
  message = 'Loading...',
  minHeight = 200,
  showProgress = true,
}) => (
  <Box
    sx={{
      display: 'flex',
      flexDirection: 'column',
      alignItems: 'center',
      justifyContent: 'center',
      minHeight,
      gap: 2,
      p: 3,
    }}
  >
    {showProgress && <CircularProgress size={40} />}
    <Typography variant="body2" color="text.secondary">
      {message}
    </Typography>
  </Box>
);

// Error fallback component
interface ErrorFallbackProps {
  error: Error;
  resetError: () => void;
  componentName?: string;
}

const ErrorFallback: React.FC<ErrorFallbackProps> = ({
  error,
  resetError,
  componentName = 'Component',
}) => (
  <Box
    sx={{
      display: 'flex',
      flexDirection: 'column',
      alignItems: 'center',
      justifyContent: 'center',
      minHeight: 200,
      gap: 2,
      p: 3,
      border: '1px solid',
      borderColor: 'error.main',
      borderRadius: 1,
      backgroundColor: 'error.light',
      color: 'error.contrastText',
    }}
  >
    <Typography variant="h6" color="error">
      Failed to load {componentName}
    </Typography>
    <Typography variant="body2" color="text.secondary" textAlign="center">
      {error.message}
    </Typography>
    <button
      onClick={resetError}
      style={{
        padding: '8px 16px',
        backgroundColor: 'transparent',
        border: '1px solid',
        borderRadius: '4px',
        cursor: 'pointer',
      }}
    >
      Try Again
    </button>
  </Box>
);

// Enhanced lazy loading options
interface LazyLoadOptions {
  fallback?: React.ComponentType<LoadingFallbackProps>;
  errorFallback?: React.ComponentType<ErrorFallbackProps>;
  loadingMessage?: string;
  minHeight?: number;
  showProgress?: boolean;
  componentName?: string;
  preload?: boolean;
  retryCount?: number;
}

// Enhanced lazy loading wrapper
export function createLazyComponent<T extends ComponentType<any>>(
  importFn: () => Promise<{ default: T }>,
  options: LazyLoadOptions = {}
): React.ComponentType<React.ComponentProps<T>> {
  const {
    fallback: CustomFallback = LoadingFallback,
    errorFallback: CustomErrorFallback = ErrorFallback,
    loadingMessage = 'Loading component...',
    minHeight = 200,
    showProgress = true,
    componentName = 'Component',
    preload = false,
    retryCount = 3,
  } = options;

  // Create lazy component with retry logic
  const LazyComponent = lazy(() => {
    let retries = 0;
    
    const loadWithRetry = async (): Promise<{ default: T }> => {
      try {
        return await importFn();
      } catch (error) {
        if (retries < retryCount) {
          retries++;
          console.warn(`Failed to load ${componentName}, retrying... (${retries}/${retryCount})`);
          // Exponential backoff
          await new Promise(resolve => setTimeout(resolve, Math.pow(2, retries) * 1000));
          return loadWithRetry();
        }
        throw error;
      }
    };

    return loadWithRetry();
  });

  // Preload if requested
  if (preload) {
    importFn().catch(() => {
      // Ignore preload errors
    });
  }

  // Return wrapped component
  return React.forwardRef<any, React.ComponentProps<T>>((props, ref) => (
    <ErrorBoundary
      FallbackComponent={(errorProps) => (
        <CustomErrorFallback {...errorProps} componentName={componentName} />
      )}
    >
      <Suspense
        fallback={
          <CustomFallback
            message={loadingMessage}
            minHeight={minHeight}
            showProgress={showProgress}
          />
        }
      >
        <LazyComponent {...props} ref={ref} />
      </Suspense>
    </ErrorBoundary>
  ));
}

// Preload utility
export function preloadComponent(importFn: () => Promise<any>): void {
  importFn().catch(() => {
    // Ignore preload errors
  });
}

// Batch preload utility
export function preloadComponents(importFns: Array<() => Promise<any>>): void {
  importFns.forEach(importFn => {
    preloadComponent(importFn);
  });
}

// Route-based lazy loading
export function createLazyRoute<T extends ComponentType<any>>(
  importFn: () => Promise<{ default: T }>,
  routeName: string
): React.ComponentType<React.ComponentProps<T>> {
  return createLazyComponent(importFn, {
    componentName: `${routeName} Page`,
    loadingMessage: `Loading ${routeName}...`,
    minHeight: 400,
    preload: false,
  });
}

// Intersection Observer based lazy loading
interface IntersectionLazyProps {
  children: React.ReactNode;
  fallback?: React.ReactNode;
  rootMargin?: string;
  threshold?: number;
  triggerOnce?: boolean;
}

export const IntersectionLazy: React.FC<IntersectionLazyProps> = ({
  children,
  fallback = <LoadingFallback />,
  rootMargin = '50px',
  threshold = 0.1,
  triggerOnce = true,
}) => {
  const [isVisible, setIsVisible] = React.useState(false);
  const [hasTriggered, setHasTriggered] = React.useState(false);
  const ref = React.useRef<HTMLDivElement>(null);

  React.useEffect(() => {
    const observer = new IntersectionObserver(
      ([entry]) => {
        if (entry.isIntersecting && (!triggerOnce || !hasTriggered)) {
          setIsVisible(true);
          if (triggerOnce) {
            setHasTriggered(true);
          }
        } else if (!triggerOnce && !entry.isIntersecting) {
          setIsVisible(false);
        }
      },
      {
        rootMargin,
        threshold,
      }
    );

    if (ref.current) {
      observer.observe(ref.current);
    }

    return () => {
      if (ref.current) {
        observer.unobserve(ref.current);
      }
    };
  }, [rootMargin, threshold, triggerOnce, hasTriggered]);

  return (
    <div ref={ref}>
      {isVisible ? children : fallback}
    </div>
  );
};

// Hook for lazy loading state
export function useLazyLoading() {
  const [loadedComponents, setLoadedComponents] = React.useState<Set<string>>(new Set());

  const markAsLoaded = React.useCallback((componentName: string) => {
    setLoadedComponents(prev => new Set(prev).add(componentName));
  }, []);

  const isLoaded = React.useCallback((componentName: string) => {
    return loadedComponents.has(componentName);
  }, [loadedComponents]);

  return {
    markAsLoaded,
    isLoaded,
    loadedComponents: Array.from(loadedComponents),
  };
}

// Performance monitoring for lazy loading
export class LazyLoadingMonitor {
  private static loadTimes: Map<string, number> = new Map();
  private static errors: Map<string, Error[]> = new Map();

  static recordLoadTime(componentName: string, loadTime: number): void {
    this.loadTimes.set(componentName, loadTime);
    
    if (loadTime > 2000) {
      console.warn(`Slow lazy load detected: ${componentName} took ${loadTime}ms`);
    }
  }

  static recordError(componentName: string, error: Error): void {
    const errors = this.errors.get(componentName) || [];
    errors.push(error);
    this.errors.set(componentName, errors);
  }

  static getReport(): {
    loadTimes: Record<string, number>;
    errors: Record<string, Error[]>;
    averageLoadTime: number;
    slowestComponent: string | null;
  } {
    const loadTimes: Record<string, number> = {};
    this.loadTimes.forEach((time, component) => {
      loadTimes[component] = time;
    });

    const errors: Record<string, Error[]> = {};
    this.errors.forEach((errorList, component) => {
      errors[component] = errorList;
    });

    const times = Array.from(this.loadTimes.values());
    const averageLoadTime = times.length > 0 ? times.reduce((a, b) => a + b, 0) / times.length : 0;

    let slowestComponent: string | null = null;
    let slowestTime = 0;
    this.loadTimes.forEach((time, component) => {
      if (time > slowestTime) {
        slowestTime = time;
        slowestComponent = component;
      }
    });

    return {
      loadTimes,
      errors,
      averageLoadTime,
      slowestComponent,
    };
  }
}
