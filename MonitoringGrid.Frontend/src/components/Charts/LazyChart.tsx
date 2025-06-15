import React, { lazy } from 'react';
import { LazyWrapper } from '@/components/Common';
import { ChartProps } from './Chart';

// Lazy load the heavy Chart component
const Chart = lazy(() => import('./Chart'));

/**
 * Lazy-loaded Chart component with proper loading states
 */
export const LazyChart: React.FC<ChartProps> = (props) => {
  return (
    <LazyWrapper minHeight={props.height || 400}>
      <Chart {...props} />
    </LazyWrapper>
  );
};

export default LazyChart;
