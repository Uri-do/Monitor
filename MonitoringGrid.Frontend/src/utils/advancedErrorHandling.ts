/**
 * Advanced Error Handling and Monitoring System
 * Enterprise-grade error management with reporting and recovery
 */

// Enhanced error types
export class ApplicationError extends Error {
  public readonly code: string;
  public readonly severity: 'low' | 'medium' | 'high' | 'critical';
  public readonly context: Record<string, any>;
  public readonly timestamp: Date;
  public readonly userAgent: string;
  public readonly url: string;
  public readonly userId?: string;

  constructor(
    message: string,
    code: string = 'UNKNOWN_ERROR',
    severity: 'low' | 'medium' | 'high' | 'critical' = 'medium',
    context: Record<string, any> = {}
  ) {
    super(message);
    this.name = 'ApplicationError';
    this.code = code;
    this.severity = severity;
    this.context = context;
    this.timestamp = new Date();
    this.userAgent = navigator.userAgent;
    this.url = window.location.href;
    this.userId = context.userId;

    // Maintain proper stack trace
    if (Error.captureStackTrace) {
      Error.captureStackTrace(this, ApplicationError);
    }
  }

  toJSON(): Record<string, any> {
    return {
      name: this.name,
      message: this.message,
      code: this.code,
      severity: this.severity,
      context: this.context,
      timestamp: this.timestamp.toISOString(),
      userAgent: this.userAgent,
      url: this.url,
      userId: this.userId,
      stack: this.stack,
    };
  }
}

// Error recovery strategies
export interface RecoveryStrategy {
  name: string;
  canRecover: (error: Error) => boolean;
  recover: (error: Error, context?: any) => Promise<any>;
  maxAttempts?: number;
}

export class ErrorRecoveryManager {
  private strategies: RecoveryStrategy[] = [];
  private recoveryAttempts = new Map<string, number>();

  registerStrategy(strategy: RecoveryStrategy): void {
    this.strategies.push(strategy);
  }

  async attemptRecovery(error: Error, context?: any): Promise<any> {
    const errorKey = `${error.name}-${error.message}`;
    const attempts = this.recoveryAttempts.get(errorKey) || 0;

    for (const strategy of this.strategies) {
      if (strategy.canRecover(error)) {
        const maxAttempts = strategy.maxAttempts || 3;
        
        if (attempts < maxAttempts) {
          try {
            this.recoveryAttempts.set(errorKey, attempts + 1);
            const result = await strategy.recover(error, context);
            
            // Reset attempts on successful recovery
            this.recoveryAttempts.delete(errorKey);
            
            return result;
          } catch (recoveryError) {
            console.warn(`Recovery strategy '${strategy.name}' failed:`, recoveryError);
            continue;
          }
        }
      }
    }

    // No recovery possible
    throw error;
  }

  clearAttempts(errorKey?: string): void {
    if (errorKey) {
      this.recoveryAttempts.delete(errorKey);
    } else {
      this.recoveryAttempts.clear();
    }
  }
}

// Error monitoring and reporting
export interface ErrorReport {
  error: ApplicationError;
  breadcrumbs: Breadcrumb[];
  userSession: UserSession;
  performanceMetrics: PerformanceMetrics;
}

export interface Breadcrumb {
  timestamp: Date;
  category: 'navigation' | 'user' | 'http' | 'console' | 'error';
  message: string;
  level: 'info' | 'warning' | 'error';
  data?: Record<string, any>;
}

export interface UserSession {
  sessionId: string;
  userId?: string;
  startTime: Date;
  pageViews: number;
  interactions: number;
  errors: number;
}

export interface PerformanceMetrics {
  memoryUsage?: number;
  loadTime: number;
  renderTime: number;
  networkLatency: number;
  fps: number;
}

export class ErrorMonitor {
  private breadcrumbs: Breadcrumb[] = [];
  private maxBreadcrumbs = 50;
  private userSession: UserSession;
  private errorReporters: Array<(report: ErrorReport) => void> = [];
  private recoveryManager = new ErrorRecoveryManager();

  constructor() {
    this.userSession = {
      sessionId: this.generateSessionId(),
      startTime: new Date(),
      pageViews: 1,
      interactions: 0,
      errors: 0,
    };

    this.setupGlobalErrorHandlers();
    this.setupPerformanceMonitoring();
    this.registerDefaultRecoveryStrategies();
  }

  private generateSessionId(): string {
    return `session-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
  }

  private setupGlobalErrorHandlers(): void {
    // Handle unhandled promise rejections
    window.addEventListener('unhandledrejection', (event) => {
      const error = new ApplicationError(
        event.reason?.message || 'Unhandled promise rejection',
        'UNHANDLED_PROMISE_REJECTION',
        'high',
        { reason: event.reason }
      );
      this.reportError(error);
    });

    // Handle global errors
    window.addEventListener('error', (event) => {
      const error = new ApplicationError(
        event.message || 'Global error',
        'GLOBAL_ERROR',
        'high',
        {
          filename: event.filename,
          lineno: event.lineno,
          colno: event.colno,
          error: event.error,
        }
      );
      this.reportError(error);
    });

    // Handle resource loading errors
    window.addEventListener('error', (event) => {
      if (event.target !== window) {
        const error = new ApplicationError(
          'Resource loading failed',
          'RESOURCE_LOAD_ERROR',
          'medium',
          {
            element: event.target,
            source: (event.target as any)?.src || (event.target as any)?.href,
          }
        );
        this.reportError(error);
      }
    }, true);
  }

  private setupPerformanceMonitoring(): void {
    // Monitor FPS
    let lastTime = performance.now();
    let frames = 0;
    
    const measureFPS = () => {
      frames++;
      const currentTime = performance.now();
      
      if (currentTime >= lastTime + 1000) {
        const fps = Math.round((frames * 1000) / (currentTime - lastTime));
        
        if (fps < 30) {
          this.addBreadcrumb({
            category: 'console',
            message: `Low FPS detected: ${fps}`,
            level: 'warning',
            data: { fps },
          });
        }
        
        frames = 0;
        lastTime = currentTime;
      }
      
      requestAnimationFrame(measureFPS);
    };
    
    requestAnimationFrame(measureFPS);
  }

  private registerDefaultRecoveryStrategies(): void {
    // Network error recovery
    this.recoveryManager.registerStrategy({
      name: 'network-retry',
      canRecover: (error) => error.message.includes('fetch') || error.message.includes('network'),
      recover: async (error, context) => {
        await new Promise(resolve => setTimeout(resolve, 1000));
        if (context?.retryFn) {
          return context.retryFn();
        }
        throw error;
      },
      maxAttempts: 3,
    });

    // Component error recovery
    this.recoveryManager.registerStrategy({
      name: 'component-refresh',
      canRecover: (error) => error.message.includes('render') || error.message.includes('component'),
      recover: async (error, context) => {
        if (context?.refreshComponent) {
          return context.refreshComponent();
        }
        throw error;
      },
      maxAttempts: 2,
    });
  }

  addBreadcrumb(breadcrumb: Omit<Breadcrumb, 'timestamp'>): void {
    const fullBreadcrumb: Breadcrumb = {
      ...breadcrumb,
      timestamp: new Date(),
    };

    this.breadcrumbs.push(fullBreadcrumb);

    // Limit breadcrumbs
    if (this.breadcrumbs.length > this.maxBreadcrumbs) {
      this.breadcrumbs.shift();
    }
  }

  addErrorReporter(reporter: (report: ErrorReport) => void): void {
    this.errorReporters.push(reporter);
  }

  async reportError(error: ApplicationError, context?: any): Promise<void> {
    this.userSession.errors++;

    // Add error breadcrumb
    this.addBreadcrumb({
      category: 'error',
      message: error.message,
      level: 'error',
      data: { code: error.code, severity: error.severity },
    });

    // Attempt recovery
    try {
      await this.recoveryManager.attemptRecovery(error, context);
      return; // Recovery successful
    } catch (recoveryError) {
      // Recovery failed, proceed with reporting
    }

    // Gather performance metrics
    const performanceMetrics: PerformanceMetrics = {
      loadTime: performance.timing?.loadEventEnd - performance.timing?.navigationStart || 0,
      renderTime: performance.now(),
      networkLatency: 0, // Would need to be measured separately
      fps: 60, // Would need to be tracked
    };

    if ('memory' in performance) {
      performanceMetrics.memoryUsage = (performance as any).memory.usedJSHeapSize;
    }

    // Create error report
    const report: ErrorReport = {
      error,
      breadcrumbs: [...this.breadcrumbs],
      userSession: { ...this.userSession },
      performanceMetrics,
    };

    // Send to all reporters
    this.errorReporters.forEach(reporter => {
      try {
        reporter(report);
      } catch (reporterError) {
        console.error('Error reporter failed:', reporterError);
      }
    });
  }

  trackUserInteraction(type: string, details?: Record<string, any>): void {
    this.userSession.interactions++;
    
    this.addBreadcrumb({
      category: 'user',
      message: `User ${type}`,
      level: 'info',
      data: details,
    });
  }

  trackNavigation(url: string): void {
    this.userSession.pageViews++;
    
    this.addBreadcrumb({
      category: 'navigation',
      message: `Navigated to ${url}`,
      level: 'info',
      data: { url },
    });
  }

  getSessionSummary(): UserSession & { breadcrumbCount: number } {
    return {
      ...this.userSession,
      breadcrumbCount: this.breadcrumbs.length,
    };
  }

  clearBreadcrumbs(): void {
    this.breadcrumbs = [];
  }

  destroy(): void {
    this.breadcrumbs = [];
    this.errorReporters = [];
  }
}

// Global error monitor instance
export const globalErrorMonitor = new ErrorMonitor();

// Convenience functions
export const reportError = (error: Error | string, code?: string, severity?: 'low' | 'medium' | 'high' | 'critical', context?: Record<string, any>) => {
  const appError = error instanceof ApplicationError 
    ? error 
    : new ApplicationError(
        typeof error === 'string' ? error : error.message,
        code,
        severity,
        context
      );
  
  return globalErrorMonitor.reportError(appError, context);
};

export const trackUserInteraction = globalErrorMonitor.trackUserInteraction.bind(globalErrorMonitor);
export const trackNavigation = globalErrorMonitor.trackNavigation.bind(globalErrorMonitor);
export const addBreadcrumb = globalErrorMonitor.addBreadcrumb.bind(globalErrorMonitor);
