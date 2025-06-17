/**
 * Performance Optimization Utilities
 * Advanced performance monitoring and optimization tools
 */

// Performance metrics interface
export interface PerformanceMetrics {
  renderTime: number;
  componentCount: number;
  memoryUsage: number;
  bundleSize: number;
  loadTime: number;
  interactionTime: number;
}

// Component performance tracker
export class ComponentPerformanceTracker {
  private static instance: ComponentPerformanceTracker;
  private metrics: Map<string, PerformanceMetrics> = new Map();
  private observers: PerformanceObserver[] = [];

  static getInstance(): ComponentPerformanceTracker {
    if (!ComponentPerformanceTracker.instance) {
      ComponentPerformanceTracker.instance = new ComponentPerformanceTracker();
    }
    return ComponentPerformanceTracker.instance;
  }

  // Track component render performance
  trackComponentRender(componentName: string, renderStart: number): void {
    const renderEnd = performance.now();
    const renderTime = renderEnd - renderStart;

    const existingMetrics = this.metrics.get(componentName) || {
      renderTime: 0,
      componentCount: 0,
      memoryUsage: 0,
      bundleSize: 0,
      loadTime: 0,
      interactionTime: 0,
    };

    this.metrics.set(componentName, {
      ...existingMetrics,
      renderTime: (existingMetrics.renderTime + renderTime) / 2, // Average
      componentCount: existingMetrics.componentCount + 1,
    });

    // Log slow renders in development
    if (process.env.NODE_ENV === 'development' && renderTime > 16) {
      console.warn(`Slow render detected: ${componentName} took ${renderTime.toFixed(2)}ms`);
    }
  }

  // Track memory usage
  trackMemoryUsage(componentName: string): void {
    if ('memory' in performance) {
      const memoryInfo = (performance as any).memory;
      const existingMetrics = this.metrics.get(componentName);
      
      if (existingMetrics) {
        this.metrics.set(componentName, {
          ...existingMetrics,
          memoryUsage: memoryInfo.usedJSHeapSize,
        });
      }
    }
  }

  // Get performance report
  getPerformanceReport(): Record<string, PerformanceMetrics> {
    const report: Record<string, PerformanceMetrics> = {};
    this.metrics.forEach((metrics, componentName) => {
      report[componentName] = metrics;
    });
    return report;
  }

  // Initialize performance monitoring
  initializeMonitoring(): void {
    // Monitor long tasks
    if ('PerformanceObserver' in window) {
      const longTaskObserver = new PerformanceObserver((list) => {
        list.getEntries().forEach((entry) => {
          if (entry.duration > 50) {
            console.warn(`Long task detected: ${entry.duration.toFixed(2)}ms`);
          }
        });
      });

      try {
        longTaskObserver.observe({ entryTypes: ['longtask'] });
        this.observers.push(longTaskObserver);
      } catch (e) {
        // Long task API not supported
      }

      // Monitor layout shifts
      const layoutShiftObserver = new PerformanceObserver((list) => {
        let cumulativeScore = 0;
        list.getEntries().forEach((entry: any) => {
          if (!entry.hadRecentInput) {
            cumulativeScore += entry.value;
          }
        });

        if (cumulativeScore > 0.1) {
          console.warn(`Layout shift detected: ${cumulativeScore.toFixed(4)}`);
        }
      });

      try {
        layoutShiftObserver.observe({ entryTypes: ['layout-shift'] });
        this.observers.push(layoutShiftObserver);
      } catch (e) {
        // Layout shift API not supported
      }
    }
  }

  // Cleanup observers
  cleanup(): void {
    this.observers.forEach(observer => observer.disconnect());
    this.observers = [];
  }
}

// React component performance HOC
export function withPerformanceTracking<P extends object>(
  WrappedComponent: React.ComponentType<P>,
  componentName?: string
) {
  const displayName = componentName || WrappedComponent.displayName || WrappedComponent.name || 'Component';
  
  return React.memo((props: P) => {
    const renderStart = performance.now();
    const tracker = ComponentPerformanceTracker.getInstance();

    React.useEffect(() => {
      tracker.trackComponentRender(displayName, renderStart);
      tracker.trackMemoryUsage(displayName);
    });

    return React.createElement(WrappedComponent, props);
  });
}

// Performance monitoring hook
export function usePerformanceMonitoring(componentName: string) {
  const tracker = ComponentPerformanceTracker.getInstance();

  React.useEffect(() => {
    const renderStart = performance.now();
    
    return () => {
      tracker.trackComponentRender(componentName, renderStart);
      tracker.trackMemoryUsage(componentName);
    };
  }, [componentName, tracker]);

  return {
    getMetrics: () => tracker.getPerformanceReport()[componentName],
    getAllMetrics: () => tracker.getPerformanceReport(),
  };
}

// Bundle size analyzer
export class BundleSizeAnalyzer {
  static analyzeChunks(): Promise<{ [key: string]: number }> {
    return new Promise((resolve) => {
      const chunks: { [key: string]: number } = {};
      
      // Analyze loaded scripts
      const scripts = document.querySelectorAll('script[src]');
      let loadedCount = 0;
      
      scripts.forEach((script: HTMLScriptElement) => {
        if (script.src) {
          fetch(script.src, { method: 'HEAD' })
            .then(response => {
              const contentLength = response.headers.get('content-length');
              if (contentLength) {
                const fileName = script.src.split('/').pop() || 'unknown';
                chunks[fileName] = parseInt(contentLength, 10);
              }
            })
            .catch(() => {
              // Ignore errors for cross-origin scripts
            })
            .finally(() => {
              loadedCount++;
              if (loadedCount === scripts.length) {
                resolve(chunks);
              }
            });
        }
      });

      // Fallback if no scripts found
      if (scripts.length === 0) {
        resolve(chunks);
      }
    });
  }

  static async generateReport(): Promise<{
    totalSize: number;
    chunks: { [key: string]: number };
    recommendations: string[];
  }> {
    const chunks = await this.analyzeChunks();
    const totalSize = Object.values(chunks).reduce((sum, size) => sum + size, 0);
    const recommendations: string[] = [];

    // Generate recommendations
    if (totalSize > 500 * 1024) { // 500KB
      recommendations.push('Consider implementing code splitting to reduce bundle size');
    }

    const largeChunks = Object.entries(chunks).filter(([, size]) => size > 100 * 1024);
    if (largeChunks.length > 0) {
      recommendations.push(`Large chunks detected: ${largeChunks.map(([name]) => name).join(', ')}`);
    }

    return {
      totalSize,
      chunks,
      recommendations,
    };
  }
}

// Web Vitals monitoring
export class WebVitalsMonitor {
  private static metrics: { [key: string]: number } = {};

  static async initializeWebVitals(): Promise<void> {
    try {
      const { getCLS, getFID, getFCP, getLCP, getTTFB } = await import('web-vitals');

      getCLS((metric) => {
        this.metrics.CLS = metric.value;
        if (metric.value > 0.1) {
          console.warn(`Poor CLS score: ${metric.value.toFixed(4)}`);
        }
      });

      getFID((metric) => {
        this.metrics.FID = metric.value;
        if (metric.value > 100) {
          console.warn(`Poor FID score: ${metric.value.toFixed(2)}ms`);
        }
      });

      getFCP((metric) => {
        this.metrics.FCP = metric.value;
        if (metric.value > 1800) {
          console.warn(`Poor FCP score: ${metric.value.toFixed(2)}ms`);
        }
      });

      getLCP((metric) => {
        this.metrics.LCP = metric.value;
        if (metric.value > 2500) {
          console.warn(`Poor LCP score: ${metric.value.toFixed(2)}ms`);
        }
      });

      getTTFB((metric) => {
        this.metrics.TTFB = metric.value;
        if (metric.value > 800) {
          console.warn(`Poor TTFB score: ${metric.value.toFixed(2)}ms`);
        }
      });
    } catch (error) {
      console.warn('Web Vitals library not available:', error);
    }
  }

  static getMetrics(): { [key: string]: number } {
    return { ...this.metrics };
  }

  static generateReport(): {
    metrics: { [key: string]: number };
    score: number;
    recommendations: string[];
  } {
    const recommendations: string[] = [];
    let score = 100;

    // Analyze metrics and generate recommendations
    if (this.metrics.CLS > 0.1) {
      score -= 20;
      recommendations.push('Improve Cumulative Layout Shift (CLS) by reserving space for dynamic content');
    }

    if (this.metrics.FID > 100) {
      score -= 15;
      recommendations.push('Improve First Input Delay (FID) by optimizing JavaScript execution');
    }

    if (this.metrics.LCP > 2500) {
      score -= 25;
      recommendations.push('Improve Largest Contentful Paint (LCP) by optimizing critical resources');
    }

    if (this.metrics.FCP > 1800) {
      score -= 20;
      recommendations.push('Improve First Contentful Paint (FCP) by optimizing critical rendering path');
    }

    if (this.metrics.TTFB > 800) {
      score -= 20;
      recommendations.push('Improve Time to First Byte (TTFB) by optimizing server response time');
    }

    return {
      metrics: this.getMetrics(),
      score: Math.max(0, score),
      recommendations,
    };
  }
}

// Initialize performance monitoring
export const initializePerformanceMonitoring = (): void => {
  const tracker = ComponentPerformanceTracker.getInstance();
  tracker.initializeMonitoring();
  WebVitalsMonitor.initializeWebVitals();
};

// Cleanup performance monitoring
export const cleanupPerformanceMonitoring = (): void => {
  const tracker = ComponentPerformanceTracker.getInstance();
  tracker.cleanup();
};
