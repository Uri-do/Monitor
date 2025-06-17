import React from 'react';
import {
  Box,
  Typography,
  Paper,
  Chip,
} from '@mui/material';
import { TrendingUp } from '@mui/icons-material';
import { IndicatorItem } from './IndicatorItem';
import { EmptyState } from './EmptyState';
import { RunningIndicator } from '../types';

interface SectionVariantProps {
  title: string;
  runningIndicators: RunningIndicator[];
  displayIndicators: RunningIndicator[];
  showProgress: boolean;
}

export const SectionVariant: React.FC<SectionVariantProps> = ({
  title,
  runningIndicators,
  displayIndicators,
  showProgress,
}) => {
  return (
    <Paper sx={{ p: 3, mb: 3, bgcolor: 'success.50', border: 1, borderColor: 'success.200' }}>
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 2 }}>
        <TrendingUp sx={{ color: 'success.main' }} />
        <Typography variant="h6" color="success.main">
          {title} ({runningIndicators.length})
        </Typography>
        <Chip
          label="LIVE"
          size="small"
          color="success"
          sx={{
            animation: 'pulse 2s infinite',
            '@keyframes pulse': {
              '0%': { opacity: 1 },
              '50%': { opacity: 0.7 },
              '100%': { opacity: 1 },
            },
          }}
        />
      </Box>

      {runningIndicators.length > 0 ? (
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
          {displayIndicators.map((indicator) => (
            <IndicatorItem
              key={indicator.indicatorID}
              indicator={indicator}
              variant="section"
              showProgress={showProgress}
            />
          ))}
        </Box>
      ) : (
        <EmptyState />
      )}
    </Paper>
  );
};

export default SectionVariant;
