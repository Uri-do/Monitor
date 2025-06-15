/**
 * Enterprise Monitoring and Analytics System
 * Advanced monitoring, logging, and analytics for enterprise applications
 */

// Monitoring event types
export type MonitoringEventType = 
  | 'page_view'
  | 'user_action'
  | 'error'
  | 'performance'
  | 'security'
  | 'business_metric'
  | 'api_call'
  | 'feature_usage';

// Monitoring event interface
export interface MonitoringEvent {
  id: string;
  type: MonitoringEventType;
  timestamp: number;
  sessionId: string;
  userId?: string;
  data: Record<string, any>;
  metadata: {
    userAgent: string;
    url: string;
    referrer: string;
    viewport: { width: number; height: number };
    connection?: string;
    deviceType: 'mobile' | 'tablet' | 'desktop';
  };
}

// Analytics configuration
export interface AnalyticsConfig {
  enabled: boolean;
  endpoint: string;
  batchSize: number;
  flushInterval: number;
  retryAttempts: number;
  enableRealtime: boolean;
  enableHeatmaps: boolean;
  enableSessionRecording: boolean;
  enablePerformanceTracking: boolean;
  enableErrorTracking: boolean;
  enableBusinessMetrics: boolean;
  samplingRate: number;
}

// Default enterprise analytics configuration
export const ENTERPRISE_ANALYTICS_CONFIG: AnalyticsConfig = {
  enabled: false, // Disabled to prevent API spam
  endpoint: '/api/analytics',
  batchSize: 50,
  flushInterval: 30000, // 30 seconds
  retryAttempts: 3,
  enableRealtime: false,
  enableHeatmaps: false,
  enableSessionRecording: false, // Privacy consideration
  enablePerformanceTracking: false,
  enableErrorTracking: false,
  enableBusinessMetrics: false,
  samplingRate: 0.0, // 0% sampling - disabled
};

// Enterprise monitoring manager
export class EnterpriseMonitoringManager {
  private config: AnalyticsConfig;
  private eventQueue: MonitoringEvent[] = [];
  private sessionId: string;
  private isOnline = navigator.onLine;
  private flushTimer: NodeJS.Timeout | null = null;
  private performanceObserver: PerformanceObserver | null = null;
  private errorCount = 0;
  private lastFlush = Date.now();

  constructor(config: AnalyticsConfig = ENTERPRISE_ANALYTICS_CONFIG) {
    this.config = config;
    this.sessionId = this.generateSessionId();
    this.initialize();
  }

  private initialize(): void {
    this.setupEventListeners();
    this.setupPerformanceMonitoring();
    this.setupErrorTracking();
    this.startFlushTimer();
    this.trackPageView();
  }

  private setupEventListeners(): void {
    // Online/offline status
    window.addEventListener('online', () => {
      this.isOnline = true;
      this.flushEvents();
    });

    window.addEventListener('offline', () => {
      this.isOnline = false;
    });

    // Page visibility changes
    document.addEventListener('visibilitychange', () => {
      if (document.visibilityState === 'hidden') {
        this.flushEvents();
      }
    });

    // Before unload - flush remaining events
    window.addEventListener('beforeunload', () => {
      this.flushEvents(true);
    });

    // User interactions
    if (this.config.enableRealtime) {
      this.setupUserInteractionTracking();
    }
  }

  private setupUserInteractionTracking(): void {
    const events = ['click', 'scroll', 'keydown', 'mousemove'];
    
    events.forEach(eventType => {
      document.addEventListener(eventType, (event) => {
        this.trackUserAction(eventType, {
          target: this.getElementSelector(event.target as Element),
          timestamp: Date.now(),
        });
      }, { passive: true });
    });
  }

  private setupPerformanceMonitoring(): void {
    if (!this.config.enablePerformanceTracking) return;

    // Core Web Vitals
    if ('PerformanceObserver' in window) {
      this.performanceObserver = new PerformanceObserver((list) => {
        const entries = list.getEntries();
        
        entries.forEach((entry) => {
          this.trackPerformance(entry.entryType, {
            name: entry.name,
            duration: entry.duration,
            startTime: entry.startTime,
            ...this.extractPerformanceMetrics(entry),
          });
        });
      });

      try {
        this.performanceObserver.observe({ 
          entryTypes: ['navigation', 'resource', 'paint', 'largest-contentful-paint', 'first-input'] 
        });
      } catch (e) {
        console.warn('Performance observer not fully supported');
      }
    }

    // Memory usage monitoring
    if ('memory' in performance) {
      setInterval(() => {
        const memory = (performance as any).memory;
        this.trackPerformance('memory', {
          usedJSHeapSize: memory.usedJSHeapSize,
          totalJSHeapSize: memory.totalJSHeapSize,
          jsHeapSizeLimit: memory.jsHeapSizeLimit,
        });
      }, 60000); // Every minute
    }
  }

  private setupErrorTracking(): void {
    if (!this.config.enableErrorTracking) return;

    // JavaScript errors
    window.addEventListener('error', (event) => {
      this.trackError('javascript', {
        message: event.message,
        filename: event.filename,
        lineno: event.lineno,
        colno: event.colno,
        stack: event.error?.stack,
      });
    });

    // Unhandled promise rejections
    window.addEventListener('unhandledrejection', (event) => {
      this.trackError('promise_rejection', {
        reason: event.reason,
        promise: event.promise,
      });
    });

    // Resource loading errors
    document.addEventListener('error', (event) => {
      if (event.target !== window) {
        this.trackError('resource', {
          element: this.getElementSelector(event.target as Element),
          source: (event.target as any).src || (event.target as any).href,
        });
      }
    }, true);
  }

  private extractPerformanceMetrics(entry: PerformanceEntry): Record<string, any> {
    const metrics: Record<string, any> = {};

    if (entry.entryType === 'navigation') {
      const navEntry = entry as PerformanceNavigationTiming;
      metrics.domContentLoaded = navEntry.domContentLoadedEventEnd - navEntry.domContentLoadedEventStart;
      metrics.loadComplete = navEntry.loadEventEnd - navEntry.loadEventStart;
      metrics.ttfb = navEntry.responseStart - navEntry.requestStart;
    }

    if (entry.entryType === 'largest-contentful-paint') {
      metrics.lcp = entry.startTime;
    }

    if (entry.entryType === 'first-input') {
      const fidEntry = entry as any;
      metrics.fid = fidEntry.processingStart - fidEntry.startTime;
    }

    return metrics;
  }

  private getElementSelector(element: Element): string {
    if (!element) return 'unknown';
    
    let selector = element.tagName.toLowerCase();
    
    if (element.id) {
      selector += `#${element.id}`;
    }
    
    if (element.className) {
      selector += `.${element.className.split(' ').join('.')}`;
    }
    
    return selector;
  }

  private generateSessionId(): string {
    return `session_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
  }

  private createEvent(type: MonitoringEventType, data: Record<string, any>): MonitoringEvent {
    return {
      id: `event_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`,
      type,
      timestamp: Date.now(),
      sessionId: this.sessionId,
      userId: this.getCurrentUserId(),
      data,
      metadata: {
        userAgent: navigator.userAgent,
        url: window.location.href,
        referrer: document.referrer,
        viewport: {
          width: window.innerWidth,
          height: window.innerHeight,
        },
        connection: (navigator as any).connection?.effectiveType,
        deviceType: this.getDeviceType(),
      },
    };
  }

  private getCurrentUserId(): string | undefined {
    // This would integrate with your auth system
    return localStorage.getItem('userId') || undefined;
  }

  private getDeviceType(): 'mobile' | 'tablet' | 'desktop' {
    const width = window.innerWidth;
    if (width < 768) return 'mobile';
    if (width < 1024) return 'tablet';
    return 'desktop';
  }

  // Public tracking methods
  public trackPageView(page?: string): void {
    const event = this.createEvent('page_view', {
      page: page || window.location.pathname,
      title: document.title,
      loadTime: performance.now(),
    });
    
    this.queueEvent(event);
  }

  public trackUserAction(action: string, data: Record<string, any> = {}): void {
    const event = this.createEvent('user_action', {
      action,
      ...data,
    });
    
    this.queueEvent(event);
  }

  public trackError(type: string, data: Record<string, any>): void {
    this.errorCount++;
    
    const event = this.createEvent('error', {
      errorType: type,
      errorCount: this.errorCount,
      ...data,
    });
    
    this.queueEvent(event);
    
    // Immediate flush for critical errors
    if (this.errorCount > 5) {
      this.flushEvents();
    }
  }

  public trackPerformance(metric: string, data: Record<string, any>): void {
    const event = this.createEvent('performance', {
      metric,
      ...data,
    });
    
    this.queueEvent(event);
  }

  public trackBusinessMetric(metric: string, value: number, data: Record<string, any> = {}): void {
    if (!this.config.enableBusinessMetrics) return;
    
    const event = this.createEvent('business_metric', {
      metric,
      value,
      ...data,
    });
    
    this.queueEvent(event);
  }

  public trackApiCall(endpoint: string, method: string, duration: number, status: number): void {
    const event = this.createEvent('api_call', {
      endpoint,
      method,
      duration,
      status,
      success: status >= 200 && status < 300,
    });
    
    this.queueEvent(event);
  }

  public trackFeatureUsage(feature: string, data: Record<string, any> = {}): void {
    const event = this.createEvent('feature_usage', {
      feature,
      ...data,
    });
    
    this.queueEvent(event);
  }

  private queueEvent(event: MonitoringEvent): void {
    if (!this.config.enabled) return;
    
    // Apply sampling rate
    if (Math.random() > this.config.samplingRate) return;
    
    this.eventQueue.push(event);
    
    // Auto-flush if queue is full
    if (this.eventQueue.length >= this.config.batchSize) {
      this.flushEvents();
    }
  }

  private startFlushTimer(): void {
    if (this.flushTimer) {
      clearInterval(this.flushTimer);
    }
    
    this.flushTimer = setInterval(() => {
      this.flushEvents();
    }, this.config.flushInterval);
  }

  private async flushEvents(immediate = false): Promise<void> {
    if (this.eventQueue.length === 0) return;
    if (!this.isOnline && !immediate) return;
    
    const events = [...this.eventQueue];
    this.eventQueue = [];
    this.lastFlush = Date.now();
    
    try {
      await this.sendEvents(events);
    } catch (error) {
      console.error('Failed to send analytics events:', error);
      
      // Re-queue events for retry (with limit)
      if (events.length < this.config.batchSize * 2) {
        this.eventQueue.unshift(...events);
      }
    }
  }

  private async sendEvents(events: MonitoringEvent[]): Promise<void> {
    const payload = {
      events,
      sessionId: this.sessionId,
      timestamp: Date.now(),
    };
    
    const response = await fetch(this.config.endpoint, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(payload),
    });
    
    if (!response.ok) {
      throw new Error(`Analytics request failed: ${response.status}`);
    }
  }

  // Analytics reporting
  public generateReport(): AnalyticsReport {
    return {
      sessionId: this.sessionId,
      timestamp: new Date().toISOString(),
      queueSize: this.eventQueue.length,
      errorCount: this.errorCount,
      lastFlush: new Date(this.lastFlush).toISOString(),
      isOnline: this.isOnline,
      config: this.config,
    };
  }

  // Cleanup
  public destroy(): void {
    if (this.flushTimer) {
      clearInterval(this.flushTimer);
    }
    
    if (this.performanceObserver) {
      this.performanceObserver.disconnect();
    }
    
    this.flushEvents(true);
  }
}

// Analytics report interface
export interface AnalyticsReport {
  sessionId: string;
  timestamp: string;
  queueSize: number;
  errorCount: number;
  lastFlush: string;
  isOnline: boolean;
  config: AnalyticsConfig;
}

// Singleton instance
let monitoringManager: EnterpriseMonitoringManager | null = null;

export const initializeEnterpriseMonitoring = (config?: AnalyticsConfig): EnterpriseMonitoringManager => {
  if (!monitoringManager) {
    monitoringManager = new EnterpriseMonitoringManager(config);
  }
  return monitoringManager;
};

export const getMonitoringManager = (): EnterpriseMonitoringManager | null => {
  return monitoringManager;
};

// React hook for monitoring
export const useMonitoring = () => {
  const manager = getMonitoringManager();
  
  return {
    trackPageView: (page?: string) => manager?.trackPageView(page),
    trackUserAction: (action: string, data?: Record<string, any>) => manager?.trackUserAction(action, data),
    trackError: (type: string, data: Record<string, any>) => manager?.trackError(type, data),
    trackPerformance: (metric: string, data: Record<string, any>) => manager?.trackPerformance(metric, data),
    trackBusinessMetric: (metric: string, value: number, data?: Record<string, any>) => 
      manager?.trackBusinessMetric(metric, value, data),
    trackApiCall: (endpoint: string, method: string, duration: number, status: number) =>
      manager?.trackApiCall(endpoint, method, duration, status),
    trackFeatureUsage: (feature: string, data?: Record<string, any>) => 
      manager?.trackFeatureUsage(feature, data),
    generateReport: () => manager?.generateReport(),
  };
};
