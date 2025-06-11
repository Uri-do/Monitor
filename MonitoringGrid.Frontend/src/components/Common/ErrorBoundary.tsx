import React, { Component, ErrorInfo, ReactNode } from 'react';
import {
  Box,
  Card,
  CardContent,
  Typography,
  Button,
  Alert,
  Accordion,
  AccordionSummary,
  AccordionDetails,
  Chip,
  Stack,
} from '@mui/material';
import {
  Error as ErrorIcon,
  Refresh as RefreshIcon,
  ExpandMore as ExpandMoreIcon,
  BugReport as BugIcon,
  Home as HomeIcon,
} from '@mui/icons-material';

interface ErrorBoundaryState {
  hasError: boolean;
  error: Error | null;
  errorInfo: ErrorInfo | null;
  errorId: string;
  retryCount: number;
}

interface ErrorBoundaryProps {
  children: ReactNode;
  fallback?: ReactNode;
  onError?: (error: Error, errorInfo: ErrorInfo) => void;
  showDetails?: boolean;
  maxRetries?: number;
  resetOnPropsChange?: boolean;
  resetKeys?: Array<string | number>;
}

/**
 * Enhanced Error Boundary with detailed error reporting and recovery options
 */
export class ErrorBoundary extends Component<ErrorBoundaryProps, ErrorBoundaryState> {
  private resetTimeoutId: number | null = null;

  constructor(props: ErrorBoundaryProps) {
    super(props);

    this.state = {
      hasError: false,
      error: null,
      errorInfo: null,
      errorId: '',
      retryCount: 0,
    };
  }

  static getDerivedStateFromError(error: Error): Partial<ErrorBoundaryState> {
    // Generate unique error ID for tracking
    const errorId = `error_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;

    return {
      hasError: true,
      error,
      errorId,
    };
  }

  componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    this.setState({
      errorInfo,
    });

    // Call custom error handler
    this.props.onError?.(error, errorInfo);

    // Log error to console for development
    console.error('ErrorBoundary caught an error:', error, errorInfo);

    // In production, you might want to send this to an error reporting service
    if (process.env.NODE_ENV === 'production') {
      this.reportError(error, errorInfo);
    }
  }

  componentDidUpdate(prevProps: ErrorBoundaryProps) {
    const { resetOnPropsChange, resetKeys } = this.props;
    const { hasError } = this.state;

    // Reset error boundary when specified props change
    if (hasError && resetOnPropsChange && resetKeys) {
      const hasResetKeyChanged = resetKeys.some(
        (key, index) => key !== prevProps.resetKeys?.[index]
      );

      if (hasResetKeyChanged) {
        this.resetErrorBoundary();
      }
    }
  }

  componentWillUnmount() {
    if (this.resetTimeoutId) {
      clearTimeout(this.resetTimeoutId);
    }
  }

  private reportError = (error: Error, errorInfo: ErrorInfo) => {
    // Here you would typically send the error to a monitoring service
    // like Sentry, LogRocket, or your own error tracking system
    const errorReport = {
      message: error.message,
      stack: error.stack,
      componentStack: errorInfo.componentStack,
      timestamp: new Date().toISOString(),
      userAgent: navigator.userAgent,
      url: window.location.href,
      errorId: this.state.errorId,
    };

    // Send to monitoring service in production
    if (process.env.NODE_ENV === 'production') {
      // errorTrackingService.captureException(errorReport);
    }
  };

  private resetErrorBoundary = () => {
    this.setState({
      hasError: false,
      error: null,
      errorInfo: null,
      errorId: '',
      retryCount: 0,
    });
  };

  private handleRetry = () => {
    const { maxRetries = 3 } = this.props;
    const { retryCount } = this.state;

    if (retryCount < maxRetries) {
      this.setState(prevState => ({
        hasError: false,
        error: null,
        errorInfo: null,
        retryCount: prevState.retryCount + 1,
      }));

      // Auto-reset after a delay to prevent infinite retry loops
      this.resetTimeoutId = window.setTimeout(() => {
        this.resetErrorBoundary();
      }, 5000);
    }
  };

  private handleGoHome = () => {
    window.location.href = '/';
  };

  private handleReload = () => {
    window.location.reload();
  };

  private getErrorSeverity = (error: Error): 'low' | 'medium' | 'high' => {
    const errorMessage = error.message.toLowerCase();

    if (errorMessage.includes('network') || errorMessage.includes('fetch')) {
      return 'medium';
    }

    if (errorMessage.includes('chunk') || errorMessage.includes('loading')) {
      return 'low';
    }

    return 'high';
  };

  private getSeverityColor = (severity: 'low' | 'medium' | 'high') => {
    switch (severity) {
      case 'low':
        return 'warning';
      case 'medium':
        return 'error';
      case 'high':
        return 'error';
      default:
        return 'error';
    }
  };

  render() {
    const { hasError, error, errorInfo, errorId, retryCount } = this.state;
    const { children, fallback, showDetails = true, maxRetries = 3 } = this.props;

    if (hasError && error) {
      // Use custom fallback if provided
      if (fallback) {
        return fallback;
      }

      const severity = this.getErrorSeverity(error);
      const canRetry = retryCount < maxRetries;

      return (
        <Box sx={{ p: 3, maxWidth: 800, mx: 'auto' }}>
          <Card>
            <CardContent>
              <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                <ErrorIcon color="error" sx={{ mr: 2, fontSize: 40 }} />
                <Box>
                  <Typography variant="h5" component="h1" gutterBottom>
                    Something went wrong
                  </Typography>
                  <Typography variant="body1" color="text.secondary">
                    We're sorry, but an unexpected error occurred. Our team has been notified.
                  </Typography>
                </Box>
              </Box>

              <Alert severity={this.getSeverityColor(severity)} sx={{ mb: 2 }}>
                <Typography variant="body2">
                  <strong>Error:</strong> {error.message}
                </Typography>
                <Typography variant="caption" display="block" sx={{ mt: 1 }}>
                  Error ID: {errorId}
                </Typography>
              </Alert>

              <Stack direction="row" spacing={1} sx={{ mb: 2 }}>
                <Chip
                  label={`Severity: ${severity.toUpperCase()}`}
                  color={this.getSeverityColor(severity)}
                  size="small"
                />
                <Chip label={`Retry: ${retryCount}/${maxRetries}`} color="default" size="small" />
              </Stack>

              <Stack direction="row" spacing={2} sx={{ mb: 2 }}>
                {canRetry && (
                  <Button
                    variant="contained"
                    startIcon={<RefreshIcon />}
                    onClick={this.handleRetry}
                  >
                    Try Again
                  </Button>
                )}
                <Button variant="outlined" startIcon={<HomeIcon />} onClick={this.handleGoHome}>
                  Go Home
                </Button>
                <Button variant="outlined" startIcon={<RefreshIcon />} onClick={this.handleReload}>
                  Reload Page
                </Button>
              </Stack>

              {showDetails && errorInfo && (
                <Accordion>
                  <AccordionSummary expandIcon={<ExpandMoreIcon />}>
                    <Typography variant="subtitle2">
                      <BugIcon sx={{ mr: 1, verticalAlign: 'middle' }} />
                      Technical Details
                    </Typography>
                  </AccordionSummary>
                  <AccordionDetails>
                    <Box sx={{ mb: 2 }}>
                      <Typography variant="subtitle2" gutterBottom>
                        Error Stack:
                      </Typography>
                      <Box
                        component="pre"
                        sx={{
                          backgroundColor: 'grey.100',
                          p: 2,
                          borderRadius: 1,
                          overflow: 'auto',
                          fontSize: '0.75rem',
                          fontFamily: 'monospace',
                          maxHeight: 200,
                        }}
                      >
                        {error.stack}
                      </Box>
                    </Box>

                    <Box>
                      <Typography variant="subtitle2" gutterBottom>
                        Component Stack:
                      </Typography>
                      <Box
                        component="pre"
                        sx={{
                          backgroundColor: 'grey.100',
                          p: 2,
                          borderRadius: 1,
                          overflow: 'auto',
                          fontSize: '0.75rem',
                          fontFamily: 'monospace',
                          maxHeight: 200,
                        }}
                      >
                        {errorInfo.componentStack}
                      </Box>
                    </Box>
                  </AccordionDetails>
                </Accordion>
              )}
            </CardContent>
          </Card>
        </Box>
      );
    }

    return children;
  }
}

// HOC for easier usage
export const withErrorBoundary = <P extends object>(
  Component: React.ComponentType<P>,
  errorBoundaryProps?: Omit<ErrorBoundaryProps, 'children'>
) => {
  const WrappedComponent = (props: P) => (
    <ErrorBoundary {...errorBoundaryProps}>
      <Component {...props} />
    </ErrorBoundary>
  );

  WrappedComponent.displayName = `withErrorBoundary(${Component.displayName || Component.name})`;

  return WrappedComponent;
};
