/**
 * Bundle optimization utilities for performance monitoring and optimization
 */

// Dynamic import wrapper with error handling and preloading
export const dynamicImport = async <T = any>(
  importFn: () => Promise<{ default: T }>,
  fallback?: T,
  preload = false
): Promise<T> => {
  try {
    if (preload) {
      // Preload the module without blocking
      importFn().catch(() => {
        // Silently handle preload failures
      });
    }

    const module = await importFn();
    return module.default;
  } catch (error) {
    console.error('Dynamic import failed:', error);
    if (fallback) {
      return fallback;
    }
    throw error;
  }
};

// Preload critical resources
export const preloadCriticalResources = () => {
  const criticalResources = [
    // Critical CSS
    '/assets/css/critical.css',
    // Critical fonts
    '/assets/fonts/roboto-regular.woff2',
    '/assets/fonts/roboto-medium.woff2',
    // Critical images
    '/assets/images/logo.svg',
  ];

  criticalResources.forEach(resource => {
    const link = document.createElement('link');
    link.rel = 'preload';
    link.href = resource;
    
    if (resource.endsWith('.css')) {
      link.as = 'style';
    } else if (resource.endsWith('.woff2') || resource.endsWith('.woff')) {
      link.as = 'font';
      link.type = 'font/woff2';
      link.crossOrigin = 'anonymous';
    } else if (resource.match(/\.(jpg|jpeg|png|webp|svg)$/)) {
      link.as = 'image';
    }
    
    document.head.appendChild(link);
  });
};

// Lazy load non-critical resources
export const lazyLoadNonCriticalResources = () => {
  // Load non-critical CSS
  const loadCSS = (href: string) => {
    const link = document.createElement('link');
    link.rel = 'stylesheet';
    link.href = href;
    link.media = 'print';
    link.onload = () => {
      link.media = 'all';
    };
    document.head.appendChild(link);
  };

  // Load non-critical resources after page load
  if (document.readyState === 'complete') {
    loadNonCritical();
  } else {
    window.addEventListener('load', loadNonCritical);
  }

  function loadNonCritical() {
    // Load analytics scripts
    requestIdleCallback(() => {
      // Load analytics or other non-critical scripts
    });
  }
};

// Bundle size monitoring
export const monitorBundleSize = () => {
  if ('performance' in window && 'getEntriesByType' in performance) {
    const resources = performance.getEntriesByType('resource') as PerformanceResourceTiming[];
    
    let totalJSSize = 0;
    let totalCSSSize = 0;
    let totalImageSize = 0;
    
    resources.forEach(resource => {
      const size = resource.transferSize || 0;
      
      if (resource.name.includes('.js')) {
        totalJSSize += size;
      } else if (resource.name.includes('.css')) {
        totalCSSSize += size;
      } else if (resource.name.match(/\.(jpg|jpeg|png|webp|svg|gif)$/)) {
        totalImageSize += size;
      }
    });
    
    const bundleInfo = {
      totalJS: Math.round(totalJSSize / 1024), // KB
      totalCSS: Math.round(totalCSSSize / 1024), // KB
      totalImages: Math.round(totalImageSize / 1024), // KB
      total: Math.round((totalJSSize + totalCSSSize + totalImageSize) / 1024), // KB
    };
    
    // Log bundle size in development
    if (process.env.NODE_ENV === 'development') {
      console.group('📦 Bundle Size Analysis');
      console.log(`JavaScript: ${bundleInfo.totalJS} KB`);
      console.log(`CSS: ${bundleInfo.totalCSS} KB`);
      console.log(`Images: ${bundleInfo.totalImages} KB`);
      console.log(`Total: ${bundleInfo.total} KB`);
      console.groupEnd();
    }
    
    return bundleInfo;
  }
  
  return null;
};

// Code splitting utilities
export const createAsyncComponent = <T extends React.ComponentType<any>>(
  importFn: () => Promise<{ default: T }>,
  fallback?: React.ComponentType
) => {
  return React.lazy(async () => {
    try {
      return await importFn();
    } catch (error) {
      console.error('Failed to load component:', error);
      if (fallback) {
        return { default: fallback };
      }
      throw error;
    }
  });
};

// Resource hints for better loading performance
export const addResourceHints = () => {
  // DNS prefetch for external domains
  const externalDomains = [
    'fonts.googleapis.com',
    'fonts.gstatic.com',
  ];
  
  externalDomains.forEach(domain => {
    const link = document.createElement('link');
    link.rel = 'dns-prefetch';
    link.href = `//${domain}`;
    document.head.appendChild(link);
  });
  
  // Preconnect to critical external resources
  const criticalDomains = [
    'fonts.googleapis.com',
  ];
  
  criticalDomains.forEach(domain => {
    const link = document.createElement('link');
    link.rel = 'preconnect';
    link.href = `https://${domain}`;
    link.crossOrigin = 'anonymous';
    document.head.appendChild(link);
  });
};

// Initialize bundle optimizations
export const initializeBundleOptimizations = () => {
  // Run optimizations when DOM is ready
  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', runOptimizations);
  } else {
    runOptimizations();
  }
  
  function runOptimizations() {
    addResourceHints();
    preloadCriticalResources();
    lazyLoadNonCriticalResources();
    
    // Monitor bundle size after load
    window.addEventListener('load', () => {
      setTimeout(monitorBundleSize, 1000);
    });
  }
};

// Tree shaking helper - mark unused exports
export const markUnused = (...args: any[]) => {
  // This function helps with tree shaking by explicitly marking unused code
  if (process.env.NODE_ENV === 'development') {
    console.warn('Unused code detected:', args);
  }
};

export default {
  dynamicImport,
  preloadCriticalResources,
  lazyLoadNonCriticalResources,
  monitorBundleSize,
  createAsyncComponent,
  addResourceHints,
  initializeBundleOptimizations,
  markUnused,
};
