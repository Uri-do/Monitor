import React, { lazy } from 'react';
import { LazyWrapper } from '@/components/Common';

// Lazy load the heavy CollectorStatisticsViewer component
const CollectorStatisticsViewer = lazy(() => import('./CollectorStatisticsViewer'));

interface LazyCollectorStatisticsViewerProps {
  collectorId: number;
  selectedItemName?: string;
  fromDate?: string;
  toDate?: string;
  mode?: 'chart' | 'table' | 'both';
  showControls?: boolean;
}

/**
 * Lazy-loaded CollectorStatisticsViewer component
 */
export const LazyCollectorStatisticsViewer: React.FC<LazyCollectorStatisticsViewerProps> = (props) => {
  return (
    <LazyWrapper minHeight={600}>
      <CollectorStatisticsViewer {...props} />
    </LazyWrapper>
  );
};

export default LazyCollectorStatisticsViewer;
