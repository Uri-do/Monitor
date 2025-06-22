/**
 * Performance monitoring and optimization utilities
 */
import React from 'react';

// Performance metrics interface
export interface PerformanceMetrics {
  fcp: number; // First Contentful Paint
  lcp: number; // Largest Contentful Paint
  fid: number; // First Input Delay
  cls: number; // Cumulative Layout Shift
  ttfb: number; // Time to First Byte
  navigationTiming: PerformanceNavigationTiming | null;
  resourceTiming: PerformanceResourceTiming[];
  memoryUsage: any;
}

// Performance observer for Core Web Vitals
class PerformanceMonitor {
  private metrics: Partial<PerformanceMetrics> = {};
  private observers: PerformanceObserver[] = [];

  constructor() {
    this.initializeObservers();
  }

  private initializeObservers() {
    // Largest Contentful Paint (LCP)
    if ('PerformanceObserver' in window) {
      try {
        const lcpObserver = new PerformanceObserver((list) => {
          const entries = list.getEntries();
          const lastEntry = entries[entries.length - 1] as any;
          this.metrics.lcp = lastEntry.startTime;
          this.reportMetric('LCP', lastEntry.startTime);
        });
        lcpObserver.observe({ entryTypes: ['largest-contentful-paint'] });
        this.observers.push(lcpObserver);
      } catch (e) {
        console.warn('LCP observer not supported');
      }

      // First Input Delay (FID)
      try {
        const fidObserver = new PerformanceObserver((list) => {
          const entries = list.getEntries();
          entries.forEach((entry: any) => {
            this.metrics.fid = entry.processingStart - entry.startTime;
            this.reportMetric('FID', entry.processingStart - entry.startTime);
          });
        });
        fidObserver.observe({ entryTypes: ['first-input'] });
        this.observers.push(fidObserver);
      } catch (e) {
        console.warn('FID observer not supported');
      }

      // Cumulative Layout Shift (CLS)
      try {
        let clsValue = 0;
        const clsObserver = new PerformanceObserver((list) => {
          const entries = list.getEntries();
          entries.forEach((entry: any) => {
            if (!entry.hadRecentInput) {
              clsValue += entry.value;
            }
          });
          this.metrics.cls = clsValue;
          this.reportMetric('CLS', clsValue);
        });
        clsObserver.observe({ entryTypes: ['layout-shift'] });
        this.observers.push(clsObserver);
      } catch (e) {
        console.warn('CLS observer not supported');
      }

      // Navigation Timing
      try {
        const navigationObserver = new PerformanceObserver((list) => {
          const entries = list.getEntries();
          entries.forEach((entry: any) => {
            this.metrics.navigationTiming = entry;
            this.metrics.ttfb = entry.responseStart - entry.requestStart;
            this.reportMetric('TTFB', this.metrics.ttfb);
          });
        });
        navigationObserver.observe({ entryTypes: ['navigation'] });
        this.observers.push(navigationObserver);
      } catch (e) {
        console.warn('Navigation observer not supported');
      }

      // Resource Timing
      try {
        const resourceObserver = new PerformanceObserver((list) => {
          const entries = list.getEntries() as PerformanceResourceTiming[];
          this.metrics.resourceTiming = entries;
          this.analyzeResourceTiming(entries);
        });
        resourceObserver.observe({ entryTypes: ['resource'] });
        this.observers.push(resourceObserver);
      } catch (e) {
        console.warn('Resource observer not supported');
      }
    }

    // First Contentful Paint (FCP)
    this.measureFCP();

    // Memory usage (if available)
    this.measureMemoryUsage();
  }

  private measureFCP() {
    if ('performance' in window && 'getEntriesByType' in performance) {
      const paintEntries = performance.getEntriesByType('paint');
      const fcpEntry = paintEntries.find(entry => entry.name === 'first-contentful-paint');
      if (fcpEntry) {
        this.metrics.fcp = fcpEntry.startTime;
        this.reportMetric('FCP', fcpEntry.startTime);
      }
    }
  }

  private measureMemoryUsage() {
    if ('memory' in performance) {
      this.metrics.memoryUsage = (performance as any).memory;
    }
  }

  private analyzeResourceTiming(entries: PerformanceResourceTiming[]) {
    const slowResources = entries.filter(entry => entry.duration > 1000);
    if (slowResources.length > 0) {
      console.warn('Slow resources detected:', slowResources);
    }

    const largeResources = entries.filter(entry => entry.transferSize > 1024 * 1024); // > 1MB
    if (largeResources.length > 0) {
      console.warn('Large resources detected:', largeResources);
    }
  }

  private reportMetric(name: string, value: number) {
    // Report to analytics service
    if (process.env.NODE_ENV === 'production') {
      // Send to monitoring service (e.g., Google Analytics, Sentry, etc.)
      console.log(`Performance metric - ${name}: ${value}`);
    }
  }

  public getMetrics(): Partial<PerformanceMetrics> {
    return { ...this.metrics };
  }

  public disconnect() {
    this.observers.forEach(observer => observer.disconnect());
    this.observers = [];
  }
}

// Singleton instance
let performanceMonitor: PerformanceMonitor | null = null;

export const initializePerformanceMonitoring = (): PerformanceMonitor => {
  if (!performanceMonitor) {
    performanceMonitor = new PerformanceMonitor();
  }
  return performanceMonitor;
};

export const getPerformanceMetrics = (): Partial<PerformanceMetrics> => {
  return performanceMonitor?.getMetrics() || {};
};

// Performance optimization utilities
export const preloadResource = (href: string, as: string = 'script') => {
  const link = document.createElement('link');
  link.rel = 'preload';
  link.href = href;
  link.as = as;
  document.head.appendChild(link);
};

export const prefetchResource = (href: string) => {
  const link = document.createElement('link');
  link.rel = 'prefetch';
  link.href = href;
  document.head.appendChild(link);
};

// Lazy loading utility
export const createIntersectionObserver = (
  callback: IntersectionObserverCallback,
  options?: IntersectionObserverInit
): IntersectionObserver => {
  return new IntersectionObserver(callback, {
    rootMargin: '50px',
    threshold: 0.1,
    ...options,
  });
};

// Bundle size analyzer
export const analyzeBundleSize = () => {
  if (process.env.NODE_ENV === 'development') {
    const scripts = Array.from(document.querySelectorAll('script[src]'));
    const styles = Array.from(document.querySelectorAll('link[rel="stylesheet"]'));
    
    console.group('Bundle Analysis');
    console.log('Scripts:', scripts.length);
    console.log('Stylesheets:', styles.length);
    console.groupEnd();
  }
};

// Memory leak detection
export const detectMemoryLeaks = () => {
  if ('memory' in performance && process.env.NODE_ENV === 'development') {
    const memory = (performance as any).memory;
    const threshold = 50 * 1024 * 1024; // 50MB
    
    if (memory.usedJSHeapSize > threshold) {
      console.warn('High memory usage detected:', {
        used: `${(memory.usedJSHeapSize / 1024 / 1024).toFixed(2)}MB`,
        total: `${(memory.totalJSHeapSize / 1024 / 1024).toFixed(2)}MB`,
        limit: `${(memory.jsHeapSizeLimit / 1024 / 1024).toFixed(2)}MB`,
      });
    }
  }
};

// Performance budget checker
export const checkPerformanceBudget = (metrics: Partial<PerformanceMetrics>) => {
  const budgets = {
    fcp: 1800, // 1.8s
    lcp: 2500, // 2.5s
    fid: 100,  // 100ms
    cls: 0.1,  // 0.1
    ttfb: 600, // 600ms
  };

  const violations: string[] = [];

  Object.entries(budgets).forEach(([metric, budget]) => {
    const value = metrics[metric as keyof PerformanceMetrics] as number;
    if (value && value > budget) {
      violations.push(`${metric.toUpperCase()}: ${value} > ${budget}`);
    }
  });

  if (violations.length > 0) {
    console.warn('Performance budget violations:', violations);
  }

  return violations;
};

// Cleanup function
export const cleanupPerformanceMonitoring = () => {
  if (performanceMonitor) {
    performanceMonitor.disconnect();
    performanceMonitor = null;
  }
};

/**
 * Basic performance monitoring HOC for components
 */
export const withPerformanceMonitoring = <P extends object>(
  Component: React.ComponentType<P>,
  componentName?: string
): React.ComponentType<P> => {
  const WrappedComponent: React.FC<P> = (props) => {
    React.useEffect(() => {
      const startTime = performance.now();

      return () => {
        const endTime = performance.now();
        const renderTime = endTime - startTime;

        if (renderTime > 100) { // Log slow renders
          console.warn(`Slow render detected in ${componentName || 'Unknown'}: ${renderTime.toFixed(2)}ms`);
        }
      };
    }, []);

    return React.createElement(Component, props);
  };

  WrappedComponent.displayName = `withPerformanceMonitoring(${componentName || Component.displayName || Component.name})`;

  return WrappedComponent;
};
