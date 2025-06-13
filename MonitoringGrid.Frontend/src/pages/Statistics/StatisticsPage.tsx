import React from 'react';
import {
  Box,
} from '@mui/material';
import { PageHeader } from '@/components/UI';
import { StatsExplorer } from '@/components/StatsExplorer';

const StatisticsPage: React.FC = () => {
  return (
    <Box>
      <PageHeader
        title="Statistics & Analytics"
        subtitle="Browse and analyze collector statistics and performance data"
      />

      <StatsExplorer />
    </Box>
  );
};

export default StatisticsPage;
