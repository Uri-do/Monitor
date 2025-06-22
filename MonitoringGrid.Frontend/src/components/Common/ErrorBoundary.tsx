import React, { Component, ErrorInfo, ReactNode } from 'react';
import { Card, Button, Alert, Collapse, Tag, Space } from 'antd';
import {
  ExclamationCircleOutlined,
  ReloadOutlined,
  HomeOutlined,
  BugOutlined,
} from '@ant-design/icons';

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

    // Send to monitoring service in production
    if (process.env.NODE_ENV === 'production') {
      const errorReport = {
        message: error.message,
        stack: error.stack,
        componentStack: errorInfo.componentStack,
        timestamp: new Date().toISOString(),
        userAgent: navigator.userAgent,
        url: window.location.href,
        errorId: this.state.errorId,
      };
      // errorTrackingService.captureException(errorReport);
      console.error('Error Report:', errorReport);
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
        <div style={{ padding: '24px', maxWidth: '800px', margin: '0 auto' }}>
          <Card>
            <div style={{ display: 'flex', alignItems: 'center', marginBottom: '16px' }}>
              <ExclamationCircleOutlined style={{ marginRight: '16px', fontSize: '40px', color: '#ff4d4f' }} />
              <div>
                <h1 style={{ margin: '0 0 8px 0', fontSize: '24px' }}>
                  Something went wrong
                </h1>
                <p style={{ margin: 0, color: '#666' }}>
                  We're sorry, but an unexpected error occurred. Our team has been notified.
                </p>
              </div>
            </div>

            <Alert
              message={<><strong>Error:</strong> {error.message}</>}
              description={<>Error ID: {errorId}</>}
              type="error"
              style={{ marginBottom: '16px' }}
            />

            <Space style={{ marginBottom: '16px' }}>
              <Tag color={severity === 'high' ? 'red' : severity === 'medium' ? 'orange' : 'yellow'}>
                Severity: {severity.toUpperCase()}
              </Tag>
              <Tag>Retry: {retryCount}/{maxRetries}</Tag>
            </Space>

            <Space style={{ marginBottom: '16px' }}>
              {canRetry && (
                <Button
                  type="primary"
                  icon={<ReloadOutlined />}
                  onClick={this.handleRetry}
                >
                  Try Again
                </Button>
              )}
              <Button icon={<HomeOutlined />} onClick={this.handleGoHome}>
                Go Home
              </Button>
              <Button icon={<ReloadOutlined />} onClick={this.handleReload}>
                Reload Page
              </Button>
            </Space>

            {showDetails && errorInfo && (
              <Collapse
                items={[
                  {
                    key: '1',
                    label: (
                      <span>
                        <BugOutlined style={{ marginRight: '8px' }} />
                        Technical Details
                      </span>
                    ),
                    children: (
                      <div>
                        <div style={{ marginBottom: '16px' }}>
                          <h4>Error Stack:</h4>
                          <pre
                            style={{
                              backgroundColor: '#f5f5f5',
                              padding: '16px',
                              borderRadius: '4px',
                              overflow: 'auto',
                              fontSize: '12px',
                              fontFamily: 'monospace',
                              maxHeight: '200px',
                            }}
                          >
                            {error.stack}
                          </pre>
                        </div>

                        <div>
                          <h4>Component Stack:</h4>
                          <pre
                            style={{
                              backgroundColor: '#f5f5f5',
                              padding: '16px',
                              borderRadius: '4px',
                              overflow: 'auto',
                              fontSize: '12px',
                              fontFamily: 'monospace',
                              maxHeight: '200px',
                            }}
                          >
                            {errorInfo.componentStack}
                          </pre>
                        </div>
                      </div>
                    ),
                  },
                ]}
              />
            )}
          </Card>
        </div>
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
