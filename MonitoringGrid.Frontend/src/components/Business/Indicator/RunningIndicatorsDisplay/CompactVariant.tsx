import React from 'react';
import { Box } from '@mui/material';
import { IndicatorItem } from './IndicatorItem';
import { EmptyState } from './EmptyState';
import { RunningIndicator } from '../types';

interface CompactVariantProps {
  runningIndicators: RunningIndicator[];
  displayIndicators: RunningIndicator[];
  showProgress: boolean;
}

export const CompactVariant: React.FC<CompactVariantProps> = ({
  runningIndicators,
  displayIndicators,
  showProgress,
}) => {
  return (
    <Box>
      {runningIndicators.length > 0 ? (
        displayIndicators.map((indicator) => (
          <IndicatorItem
            key={indicator.indicatorID}
            indicator={indicator}
            variant="compact"
            showProgress={showProgress}
          />
        ))
      ) : (
        <EmptyState />
      )}
    </Box>
  );
};

export default CompactVariant;
