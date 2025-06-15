import React from 'react';
import ReactDOM from 'react-dom/client';
import { HelmetProvider } from 'react-helmet-async';
import App from './App.tsx';
import { initializePerformanceMonitoring } from '@/utils/performance';
import { validateEnvironment } from '@/utils/security';
import { initializeAdvancedPerformanceMonitoring } from '@/utils/advancedPerformance';
import { initializeAdvancedSecurity } from '@/security/advancedSecurity';
import { initializeEnterpriseMonitoring } from '@/monitoring/enterpriseMonitoring';
import { initializeMicrofrontendArchitecture } from '@/architecture/microfrontend';

// Initialize enterprise-grade systems
validateEnvironment();
initializePerformanceMonitoring();
initializeAdvancedPerformanceMonitoring();
initializeAdvancedSecurity();
initializeEnterpriseMonitoring();
initializeMicrofrontendArchitecture();

// Performance monitoring for initial load
const startTime = performance.now();

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <HelmetProvider>
      <App />
    </HelmetProvider>
  </React.StrictMode>
);

// Report initial load time
window.addEventListener('load', () => {
  const loadTime = performance.now() - startTime;
  console.log(`Initial load time: ${loadTime.toFixed(2)}ms`);

  // Report to monitoring service in production
  if (process.env.NODE_ENV === 'production') {
    // Example: analytics.track('app_load_time', { duration: loadTime });
  }
});
