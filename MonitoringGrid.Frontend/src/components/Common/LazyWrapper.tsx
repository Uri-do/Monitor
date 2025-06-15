import React, { Suspense } from 'react';
import { Box, CircularProgress, Typography } from '@mui/material';
import { ErrorBoundary } from 'react-error-boundary';

interface LazyWrapperProps {
  children: React.ReactNode;
  fallback?: React.ReactNode;
  errorFallback?: React.ComponentType<{ error: Error; resetErrorBoundary: () => void }>;
  minHeight?: number | string;
}

const DefaultLoadingFallback: React.FC<{ minHeight?: number | string }> = ({ minHeight = 200 }) => (
  <Box
    sx={{
      display: 'flex',
      flexDirection: 'column',
      alignItems: 'center',
      justifyContent: 'center',
      minHeight,
      gap: 2,
    }}
  >
    <CircularProgress />
    <Typography variant="body2" color="text.secondary">
      Loading component...
    </Typography>
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
      minHeight: 200,
      gap: 2,
      p: 3,
      border: '1px solid',
      borderColor: 'error.main',
      borderRadius: 1,
      bgcolor: 'error.light',
      color: 'error.contrastText',
    }}
  >
    <Typography variant="h6">Something went wrong</Typography>
    <Typography variant="body2" sx={{ textAlign: 'center' }}>
      {error.message}
    </Typography>
    <button onClick={resetErrorBoundary}>Try again</button>
  </Box>
);

/**
 * Wrapper component for lazy-loaded components with error boundary and loading states
 */
export const LazyWrapper: React.FC<LazyWrapperProps> = ({
  children,
  fallback,
  errorFallback: ErrorFallbackComponent = DefaultErrorFallback,
  minHeight,
}) => {
  return (
    <ErrorBoundary FallbackComponent={ErrorFallbackComponent}>
      <Suspense fallback={fallback || <DefaultLoadingFallback minHeight={minHeight} />}>
        {children}
      </Suspense>
    </ErrorBoundary>
  );
};

export default LazyWrapper;
