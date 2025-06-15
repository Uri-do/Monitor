import React, { lazy } from 'react';
import { LazyWrapper } from '@/components/Common';

// Lazy load the heavy Analytics component
const Analytics = lazy(() => import('./Analytics'));

/**
 * Lazy-loaded Analytics page component
 */
export const LazyAnalytics: React.FC = () => {
  return (
    <LazyWrapper minHeight="100vh">
      <Analytics />
    </LazyWrapper>
  );
};

export default LazyAnalytics;
