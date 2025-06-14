/**
 * Advanced Performance Optimization System
 * Enterprise-grade performance monitoring and optimization utilities
 */

// Advanced performance metrics interface
export interface AdvancedPerformanceMetrics {
  // Core Web Vitals
  fcp: number; // First Contentful Paint
  lcp: number; // Largest Contentful Paint
  fid: number; // First Input Delay
  cls: number; // Cumulative Layout Shift
  ttfb: number; // Time to First Byte
  
  // Custom metrics
  renderTime: number;
  memoryUsage: number;
  bundleSize: number;
  cacheHitRate: number;
  errorRate: number;
  
  // React-specific metrics
  componentRenderCount: number;
  suspenseFallbackCount: number;
  transitionCount: number;
  deferredUpdateCount: number;
}

// Performance budget configuration
export interface PerformanceBudget {
  fcp: number;
  lcp: number;
  fid: number;
  cls: number;
  ttfb: number;
  bundleSize: number;
  memoryUsage: number;
  renderTime: number;
}

// Default performance budgets (enterprise-grade)
export const ENTERPRISE_PERFORMANCE_BUDGET: PerformanceBudget = {
  fcp: 1500,    // 1.5s
  lcp: 2000,    // 2.0s
  fid: 50,      // 50ms
  cls: 0.05,    // 0.05
  ttfb: 500,    // 500ms
  bundleSize: 300, // 300KB
  memoryUsage: 50,  // 50MB
  renderTime: 8,    // 8ms (60fps = 16ms, target 50% margin)
};

// Advanced performance monitor class
export class AdvancedPerformanceMonitor {
  private metrics: Partial<AdvancedPerformanceMetrics> = {};
  private observers: PerformanceObserver[] = [];
  private budget: PerformanceBudget;
  private violations: string[] = [];
  private isMonitoring = false;

  constructor(budget: PerformanceBudget = ENTERPRISE_PERFORMANCE_BUDGET) {
    this.budget = budget;
    this.initializeObservers();
  }

  private initializeObservers() {
    // Core Web Vitals observer
    if ('PerformanceObserver' in window) {
      // LCP Observer
      const lcpObserver = new PerformanceObserver((list) => {
        const entries = list.getEntries();
        const lastEntry = entries[entries.length - 1] as PerformanceEntry & { renderTime: number };
        this.metrics.lcp = lastEntry.renderTime || lastEntry.loadTime;
        this.checkBudgetViolation('lcp', this.metrics.lcp);
      });
      
      try {
        lcpObserver.observe({ entryTypes: ['largest-contentful-paint'] });
        this.observers.push(lcpObserver);
      } catch (e) {
        console.warn('LCP observer not supported');
      }

      // FID Observer
      const fidObserver = new PerformanceObserver((list) => {
        const entries = list.getEntries();
        entries.forEach((entry: any) => {
          this.metrics.fid = entry.processingStart - entry.startTime;
          this.checkBudgetViolation('fid', this.metrics.fid);
        });
      });
      
      try {
        fidObserver.observe({ entryTypes: ['first-input'] });
        this.observers.push(fidObserver);
      } catch (e) {
        console.warn('FID observer not supported');
      }

      // CLS Observer
      const clsObserver = new PerformanceObserver((list) => {
        let clsValue = 0;
        const entries = list.getEntries();
        entries.forEach((entry: any) => {
          if (!entry.hadRecentInput) {
            clsValue += entry.value;
          }
        });
        this.metrics.cls = clsValue;
        this.checkBudgetViolation('cls', this.metrics.cls);
      });
      
      try {
        clsObserver.observe({ entryTypes: ['layout-shift'] });
        this.observers.push(clsObserver);
      } catch (e) {
        console.warn('CLS observer not supported');
      }

      // Navigation timing observer
      const navigationObserver = new PerformanceObserver((list) => {
        const entries = list.getEntries();
        entries.forEach((entry: any) => {
          this.metrics.ttfb = entry.responseStart - entry.requestStart;
          this.metrics.fcp = entry.loadEventEnd - entry.navigationStart;
          this.checkBudgetViolation('ttfb', this.metrics.ttfb);
          this.checkBudgetViolation('fcp', this.metrics.fcp);
        });
      });
      
      try {
        navigationObserver.observe({ entryTypes: ['navigation'] });
        this.observers.push(navigationObserver);
      } catch (e) {
        console.warn('Navigation observer not supported');
      }
    }
  }

  private checkBudgetViolation(metric: keyof PerformanceBudget, value: number) {
    const budgetValue = this.budget[metric];
    if (value > budgetValue) {
      const violation = `${metric.toUpperCase()}: ${value} > ${budgetValue}`;
      this.violations.push(violation);
      
      if (process.env.NODE_ENV === 'development') {
        console.warn(`Performance budget violation: ${violation}`);
      }
    }
  }

  public startMonitoring() {
    this.isMonitoring = true;
    this.measureMemoryUsage();
    this.measureBundleSize();
  }

  public stopMonitoring() {
    this.isMonitoring = false;
    this.observers.forEach(observer => observer.disconnect());
    this.observers = [];
  }

  private measureMemoryUsage() {
    if ('memory' in performance) {
      const memory = (performance as any).memory;
      this.metrics.memoryUsage = memory.usedJSHeapSize / (1024 * 1024); // MB
      this.checkBudgetViolation('memoryUsage', this.metrics.memoryUsage);
    }
  }

  private measureBundleSize() {
    // Estimate bundle size from loaded resources
    if ('getEntriesByType' in performance) {
      const resources = performance.getEntriesByType('resource') as PerformanceResourceTiming[];
      let totalSize = 0;
      
      resources.forEach(resource => {
        if (resource.name.includes('.js') || resource.name.includes('.css')) {
          totalSize += resource.transferSize || 0;
        }
      });
      
      this.metrics.bundleSize = totalSize / 1024; // KB
      this.checkBudgetViolation('bundleSize', this.metrics.bundleSize);
    }
  }

  public recordRenderTime(componentName: string, renderTime: number) {
    this.metrics.renderTime = renderTime;
    this.metrics.componentRenderCount = (this.metrics.componentRenderCount || 0) + 1;
    this.checkBudgetViolation('renderTime', renderTime);
    
    if (process.env.NODE_ENV === 'development' && renderTime > this.budget.renderTime) {
      console.warn(`Slow render: ${componentName} took ${renderTime.toFixed(2)}ms`);
    }
  }

  public recordSuspenseFallback() {
    this.metrics.suspenseFallbackCount = (this.metrics.suspenseFallbackCount || 0) + 1;
  }

  public recordTransition() {
    this.metrics.transitionCount = (this.metrics.transitionCount || 0) + 1;
  }

  public recordDeferredUpdate() {
    this.metrics.deferredUpdateCount = (this.metrics.deferredUpdateCount || 0) + 1;
  }

  public getMetrics(): Partial<AdvancedPerformanceMetrics> {
    return { ...this.metrics };
  }

  public getViolations(): string[] {
    return [...this.violations];
  }

  public generateReport(): PerformanceReport {
    const score = this.calculatePerformanceScore();
    
    return {
      timestamp: new Date().toISOString(),
      score,
      metrics: this.getMetrics(),
      violations: this.getViolations(),
      budget: this.budget,
      recommendations: this.generateRecommendations(),
      grade: this.getPerformanceGrade(score),
    };
  }

  private calculatePerformanceScore(): number {
    let score = 100;
    
    // Deduct points for budget violations
    this.violations.forEach(violation => {
      if (violation.includes('LCP') || violation.includes('FCP')) score -= 15;
      else if (violation.includes('FID') || violation.includes('CLS')) score -= 20;
      else if (violation.includes('TTFB')) score -= 10;
      else if (violation.includes('RENDERTIME')) score -= 5;
      else score -= 3;
    });
    
    return Math.max(0, Math.min(100, score));
  }

  private generateRecommendations(): string[] {
    const recommendations: string[] = [];
    
    if (this.metrics.lcp && this.metrics.lcp > this.budget.lcp) {
      recommendations.push('Optimize Largest Contentful Paint - consider image optimization and critical resource preloading');
    }
    
    if (this.metrics.fid && this.metrics.fid > this.budget.fid) {
      recommendations.push('Reduce First Input Delay - minimize JavaScript execution time and use code splitting');
    }
    
    if (this.metrics.cls && this.metrics.cls > this.budget.cls) {
      recommendations.push('Improve Cumulative Layout Shift - set explicit dimensions for images and ads');
    }
    
    if (this.metrics.bundleSize && this.metrics.bundleSize > this.budget.bundleSize) {
      recommendations.push('Reduce bundle size - implement tree shaking and remove unused dependencies');
    }
    
    if (this.metrics.memoryUsage && this.metrics.memoryUsage > this.budget.memoryUsage) {
      recommendations.push('Optimize memory usage - check for memory leaks and optimize data structures');
    }
    
    return recommendations;
  }

  private getPerformanceGrade(score: number): string {
    if (score >= 95) return 'A+';
    if (score >= 90) return 'A';
    if (score >= 85) return 'B+';
    if (score >= 80) return 'B';
    if (score >= 75) return 'C+';
    if (score >= 70) return 'C';
    if (score >= 65) return 'D+';
    if (score >= 60) return 'D';
    return 'F';
  }
}

// Performance report interface
export interface PerformanceReport {
  timestamp: string;
  score: number;
  grade: string;
  metrics: Partial<AdvancedPerformanceMetrics>;
  violations: string[];
  budget: PerformanceBudget;
  recommendations: string[];
}

// Singleton instance
let advancedPerformanceMonitor: AdvancedPerformanceMonitor | null = null;

export const initializeAdvancedPerformanceMonitoring = (
  budget?: PerformanceBudget
): AdvancedPerformanceMonitor => {
  if (!advancedPerformanceMonitor) {
    advancedPerformanceMonitor = new AdvancedPerformanceMonitor(budget);
    advancedPerformanceMonitor.startMonitoring();
  }
  return advancedPerformanceMonitor;
};

export const getAdvancedPerformanceMonitor = (): AdvancedPerformanceMonitor | null => {
  return advancedPerformanceMonitor;
};

// React component performance decorator
export const withPerformanceMonitoring = <P extends object>(
  Component: React.ComponentType<P>,
  componentName?: string
) => {
  const WrappedComponent = React.forwardRef<any, P>((props, ref) => {
    const name = componentName || Component.displayName || Component.name || 'Unknown';
    
    React.useEffect(() => {
      const startTime = performance.now();
      
      return () => {
        const endTime = performance.now();
        const renderTime = endTime - startTime;
        advancedPerformanceMonitor?.recordRenderTime(name, renderTime);
      };
    });
    
    return <Component {...props} ref={ref} />;
  });
  
  WrappedComponent.displayName = `withPerformanceMonitoring(${componentName || Component.displayName || Component.name})`;
  
  return WrappedComponent;
};
