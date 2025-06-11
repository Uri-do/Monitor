import { useEffect, useCallback, useRef } from 'react';
import { useAppStore } from '@/stores/appStore';

interface MetricData {
  name: string;
  value: number;
  timestamp: Date;
  tags?: Record<string, string>;
  unit?: string;
}

interface TraceSpan {
  traceId: string;
  spanId: string;
  parentSpanId?: string;
  operationName: string;
  startTime: Date;
  endTime?: Date;
  duration?: number;
  tags: Record<string, any>;
  logs: Array<{
    timestamp: Date;
    level: 'debug' | 'info' | 'warn' | 'error' | 'fatal';
    message: string;
    fields?: Record<string, any>;
  }>;
  status: 'ok' | 'error' | 'timeout';
}

interface LogEntry {
  timestamp: Date;
  level: 'debug' | 'info' | 'warn' | 'error' | 'fatal';
  message: string;
  component: string;
  userId?: string;
  sessionId?: string;
  traceId?: string;
  spanId?: string;
  metadata?: Record<string, any>;
  error?: {
    name: string;
    message: string;
    stack?: string;
  };
}

interface PerformanceMetrics {
  // Core Web Vitals
  fcp: number; // First Contentful Paint
  lcp: number; // Largest Contentful Paint
  fid: number; // First Input Delay
  cls: number; // Cumulative Layout Shift
  ttfb: number; // Time to First Byte

  // Custom metrics
  renderTime: number;
  apiResponseTime: number;
  memoryUsage: number;
  bundleSize: number;
  cacheHitRate: number;
}

interface UserInteraction {
  type: 'click' | 'scroll' | 'input' | 'navigation' | 'error';
  element?: string;
  page: string;
  timestamp: Date;
  userId?: string;
  sessionId: string;
  metadata?: Record<string, any>;
}

interface BusinessMetric {
  name: string;
  value: number;
  dimension?: Record<string, string>;
  timestamp: Date;
}

/**
 * Enterprise observability hook for comprehensive monitoring
 */
export const useObservability = (componentName?: string) => {
  const sessionId = useRef(crypto.randomUUID());
  const traceId = useRef<string | null>(null);
  const activeSpans = useRef<Map<string, TraceSpan>>(new Map());

  const incrementApiCallCount = useAppStore(state => state.incrementApiCallCount);
  const incrementErrorCount = useAppStore(state => state.incrementErrorCount);
  const addError = useAppStore(state => state.addError);

  // Initialize observability
  useEffect(() => {
    initializeObservability();
    setupGlobalErrorHandling();
    setupPerformanceObserver();
    setupUserInteractionTracking();

    return () => {
      cleanup();
    };
  }, []);

  const initializeObservability = useCallback(() => {
    // Initialize session
    const session = {
      sessionId: sessionId.current,
      startTime: new Date(),
      userAgent: navigator.userAgent,
      url: window.location.href,
      referrer: document.referrer,
      viewport: {
        width: window.innerWidth,
        height: window.innerHeight,
      },
    };

    logEvent('session_start', 'info', 'User session started', { session });
  }, []);

  // Metrics collection
  const recordMetric = useCallback(
    (metric: MetricData) => {
      const enrichedMetric = {
        ...metric,
        sessionId: sessionId.current,
        component: componentName,
        url: window.location.pathname,
      };

      // Send to monitoring service
      sendToMonitoringService('metric', enrichedMetric);
    },
    [componentName]
  );

  const recordBusinessMetric = useCallback((metric: BusinessMetric) => {
    const enrichedMetric = {
      ...metric,
      sessionId: sessionId.current,
      userId: getCurrentUserId(),
    };

    sendToMonitoringService('business_metric', enrichedMetric);
  }, []);

  // Distributed tracing
  const startTrace = useCallback((operationName: string, tags?: Record<string, any>) => {
    const newTraceId = crypto.randomUUID();
    traceId.current = newTraceId;

    const span = startSpan(operationName, tags);
    return { traceId: newTraceId, spanId: span.spanId };
  }, []);

  const startSpan = useCallback(
    (operationName: string, tags?: Record<string, any>, parentSpanId?: string) => {
      const spanId = crypto.randomUUID();
      const span: TraceSpan = {
        traceId: traceId.current || crypto.randomUUID(),
        spanId,
        parentSpanId,
        operationName,
        startTime: new Date(),
        tags: {
          component: componentName,
          sessionId: sessionId.current,
          ...tags,
        },
        logs: [],
        status: 'ok',
      };

      activeSpans.current.set(spanId, span);
      return span;
    },
    [componentName]
  );

  const finishSpan = useCallback(
    (spanId: string, status: 'ok' | 'error' | 'timeout' = 'ok', error?: Error) => {
      const span = activeSpans.current.get(spanId);
      if (!span) return;

      span.endTime = new Date();
      span.duration = span.endTime.getTime() - span.startTime.getTime();
      span.status = status;

      if (error) {
        span.logs.push({
          timestamp: new Date(),
          level: 'error',
          message: error.message,
          fields: {
            errorName: error.name,
            errorStack: error.stack,
          },
        });
      }

      // Send completed span to monitoring service
      sendToMonitoringService('span', span);
      activeSpans.current.delete(spanId);
    },
    []
  );

  const addSpanLog = useCallback(
    (spanId: string, level: LogEntry['level'], message: string, fields?: Record<string, any>) => {
      const span = activeSpans.current.get(spanId);
      if (!span) return;

      span.logs.push({
        timestamp: new Date(),
        level,
        message,
        fields,
      });
    },
    []
  );

  // Logging
  const logEvent = useCallback(
    (
      message: string,
      level: LogEntry['level'] = 'info',
      component?: string,
      metadata?: Record<string, any>,
      error?: Error
    ) => {
      const logEntry: LogEntry = {
        timestamp: new Date(),
        level,
        message,
        component: component || componentName || 'unknown',
        userId: getCurrentUserId(),
        sessionId: sessionId.current,
        traceId: traceId.current || undefined,
        metadata,
        error: error
          ? {
              name: error.name,
              message: error.message,
              stack: error.stack,
            }
          : undefined,
      };

      // Console logging for development
      if (process.env.NODE_ENV === 'development') {
        const logMethod =
          level === 'error' || level === 'fatal' ? 'error' : level === 'warn' ? 'warn' : 'log';
        console[logMethod](
          `[${level.toUpperCase()}] ${component || componentName}: ${message}`,
          metadata
        );
      }

      // Send to monitoring service
      sendToMonitoringService('log', logEntry);

      // Update app store for error tracking
      if (level === 'error' || level === 'fatal') {
        incrementErrorCount();
        addError(message, metadata);
      }
    },
    [componentName, incrementErrorCount, addError]
  );

  // Performance monitoring
  const recordPerformanceMetrics = useCallback(() => {
    if ('performance' in window) {
      const navigation = performance.getEntriesByType(
        'navigation'
      )[0] as PerformanceNavigationTiming;
      const paint = performance.getEntriesByType('paint');

      const metrics: Partial<PerformanceMetrics> = {
        ttfb: navigation.responseStart - navigation.requestStart,
        renderTime: navigation.loadEventEnd - navigation.fetchStart,
      };

      // First Contentful Paint
      const fcp = paint.find(entry => entry.name === 'first-contentful-paint');
      if (fcp) metrics.fcp = fcp.startTime;

      // Memory usage (if available)
      if ('memory' in performance) {
        const memory = (performance as any).memory;
        metrics.memoryUsage = memory.usedJSHeapSize / (1024 * 1024); // MB
      }

      Object.entries(metrics).forEach(([name, value]) => {
        if (value !== undefined) {
          recordMetric({
            name: `performance.${name}`,
            value,
            timestamp: new Date(),
            unit: name.includes('Time') ? 'ms' : name === 'memoryUsage' ? 'MB' : 'count',
          });
        }
      });
    }
  }, [recordMetric]);

  // User interaction tracking
  const trackUserInteraction = useCallback(
    (interaction: Omit<UserInteraction, 'sessionId' | 'timestamp'>) => {
      const enrichedInteraction: UserInteraction = {
        ...interaction,
        sessionId: sessionId.current,
        timestamp: new Date(),
        userId: getCurrentUserId(),
      };

      sendToMonitoringService('interaction', enrichedInteraction);
    },
    []
  );

  // Error tracking
  const trackError = useCallback(
    (error: Error, context?: Record<string, any>) => {
      logEvent(
        error.message,
        'error',
        componentName,
        {
          errorName: error.name,
          errorStack: error.stack,
          context,
        },
        error
      );

      // Create error span if we have an active trace
      if (traceId.current) {
        const span = startSpan('error_handling', {
          'error.name': error.name,
          'error.message': error.message,
        });
        finishSpan(span.spanId, 'error', error);
      }
    },
    [componentName, logEvent, startSpan, finishSpan]
  );

  // API call tracking
  const trackApiCall = useCallback(
    (method: string, url: string, duration: number, status: number, error?: Error) => {
      incrementApiCallCount();

      const span = startSpan('api_call', {
        'http.method': method,
        'http.url': url,
        'http.status_code': status,
      });

      recordMetric({
        name: 'api.response_time',
        value: duration,
        timestamp: new Date(),
        tags: {
          method,
          endpoint: new URL(url).pathname,
          status: status.toString(),
        },
        unit: 'ms',
      });

      if (error) {
        addSpanLog(span.spanId, 'error', `API call failed: ${error.message}`);
        finishSpan(span.spanId, 'error', error);
      } else {
        finishSpan(span.spanId, status >= 400 ? 'error' : 'ok');
      }
    },
    [incrementApiCallCount, recordMetric, startSpan, finishSpan, addSpanLog]
  );

  // Setup global error handling
  const setupGlobalErrorHandling = useCallback(() => {
    const handleError = (event: ErrorEvent) => {
      trackError(new Error(event.message), {
        filename: event.filename,
        lineno: event.lineno,
        colno: event.colno,
      });
    };

    const handleUnhandledRejection = (event: PromiseRejectionEvent) => {
      trackError(new Error(`Unhandled Promise Rejection: ${event.reason}`), {
        reason: event.reason,
      });
    };

    window.addEventListener('error', handleError);
    window.addEventListener('unhandledrejection', handleUnhandledRejection);

    return () => {
      window.removeEventListener('error', handleError);
      window.removeEventListener('unhandledrejection', handleUnhandledRejection);
    };
  }, [trackError]);

  // Setup performance observer
  const setupPerformanceObserver = useCallback(() => {
    if ('PerformanceObserver' in window) {
      const observer = new PerformanceObserver(list => {
        list.getEntries().forEach(entry => {
          if (entry.entryType === 'largest-contentful-paint') {
            recordMetric({
              name: 'performance.lcp',
              value: entry.startTime,
              timestamp: new Date(),
              unit: 'ms',
            });
          }
        });
      });

      try {
        observer.observe({
          entryTypes: ['largest-contentful-paint', 'first-input', 'layout-shift'],
        });
      } catch (e) {
        // Observer not supported
      }

      return () => observer.disconnect();
    }
  }, [recordMetric]);

  // Setup user interaction tracking
  const setupUserInteractionTracking = useCallback(() => {
    const handleClick = (event: MouseEvent) => {
      const target = event.target as HTMLElement;
      trackUserInteraction({
        type: 'click',
        element:
          target.tagName +
          (target.id ? `#${target.id}` : '') +
          (target.className ? `.${target.className}` : ''),
        page: window.location.pathname,
      });
    };

    document.addEventListener('click', handleClick);
    return () => document.removeEventListener('click', handleClick);
  }, [trackUserInteraction]);

  // Cleanup
  const cleanup = useCallback(() => {
    // Finish any active spans
    activeSpans.current.forEach((span, spanId) => {
      finishSpan(spanId, 'timeout');
    });

    // Log session end
    logEvent('session_end', 'info', 'User session ended');
  }, [finishSpan, logEvent]);

  // Send data to monitoring service
  const sendToMonitoringService = useCallback((type: string, data: any) => {
    // In a real implementation, this would send to your monitoring service
    // For now, we'll just log to console in development
    if (process.env.NODE_ENV === 'development') {
      console.log(`[OBSERVABILITY] ${type}:`, data);
    }

    // You could send to services like:
    // - DataDog
    // - New Relic
    // - Elastic APM
    // - Jaeger
    // - Custom monitoring endpoint
  }, []);

  // Utility functions
  const getCurrentUserId = () => {
    // Get from auth context or store
    return 'user-123'; // Placeholder
  };

  return {
    // Metrics
    recordMetric,
    recordBusinessMetric,
    recordPerformanceMetrics,

    // Tracing
    startTrace,
    startSpan,
    finishSpan,
    addSpanLog,

    // Logging
    logEvent,
    trackError,
    trackApiCall,
    trackUserInteraction,

    // State
    sessionId: sessionId.current,
    traceId: traceId.current,
    activeSpansCount: activeSpans.current.size,
  };
};
