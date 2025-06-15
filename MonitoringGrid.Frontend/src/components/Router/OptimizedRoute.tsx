import React, { Suspense } from 'react';
import { Box, LinearProgress, Typography } from '@mui/material';
import { ErrorBoundary } from 'react-error-boundary';
import { withPerformanceMonitoring } from '@/utils/advancedPerformance';

interface OptimizedRouteProps {
  component: React.ComponentType<any>;
  fallback?: React.ReactNode;
  errorFallback?: React.ComponentType<{ error: Error; resetErrorBoundary: () => void }>;
  preload?: boolean;
  routeName?: string;
}

const DefaultLoadingFallback: React.FC = () => (
  <Box sx={{ width: '100%', mt: 2 }}>
    <LinearProgress />
    <Box sx={{ display: 'flex', justifyContent: 'center', mt: 2 }}>
      <Typography variant="body2" color="text.secondary">
        Loading page...
      </Typography>
    </Box>
  </Box>
);

const DefaultErrorFallback: React.FC<{ error: Error; resetErrorBoundary: () => void }> = ({
  error,
  resetErrorBoundary,
}) => (
  <Box
    sx={{
      display: 'flex',
      flexDirection: 'column',
      alignItems: 'center',
      justifyContent: 'center',
      minHeight: '50vh',
      gap: 2,
      p: 3,
    }}
  >
    <Typography variant="h5" color="error">
      Something went wrong
    </Typography>
    <Typography variant="body1" color="text.secondary" sx={{ textAlign: 'center' }}>
      {error.message}
    </Typography>
    <button onClick={resetErrorBoundary}>Try again</button>
  </Box>
);

/**
 * Optimized route wrapper with performance monitoring, error boundaries, and loading states
 */
export const OptimizedRoute: React.FC<OptimizedRouteProps> = ({
  component: Component,
  fallback,
  errorFallback: ErrorFallbackComponent = DefaultErrorFallback,
  preload = false,
  routeName,
}) => {
  // Wrap component with performance monitoring
  const MonitoredComponent = React.useMemo(() => {
    return withPerformanceMonitoring(Component, routeName || Component.displayName || Component.name);
  }, [Component, routeName]);

  // Preload component if requested
  React.useEffect(() => {
    if (preload && Component) {
      // Trigger component preload
      const preloadPromise = import(/* webpackMode: "lazy" */ '@/pages');
      preloadPromise.catch(() => {
        // Silently handle preload failures
      });
    }
  }, [preload, Component]);

  return (
    <ErrorBoundary FallbackComponent={ErrorFallbackComponent}>
      <Suspense fallback={fallback || <DefaultLoadingFallback />}>
        <MonitoredComponent />
      </Suspense>
    </ErrorBoundary>
  );
};

export default OptimizedRoute;
