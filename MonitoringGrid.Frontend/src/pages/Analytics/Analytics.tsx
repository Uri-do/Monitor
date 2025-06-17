import React, { useState } from 'react';
import { Box, Grid } from '@mui/material';
import {
  Assessment,
  Analytics as AnalyticsIcon,
} from '@mui/icons-material';
import { useAlertStatistics } from '@/hooks/useAlerts';
import { useIndicatorDashboard } from '@/hooks/useIndicators';
import { PageHeader, LoadingSpinner, Select } from '@/components/UI';

// Import analytics components
import {
  MetricsOverview,
  AlertTrendsChart,
  SeverityDistributionChart,
  PerformanceTable,
} from './components';

// Analytics data will be loaded from real API endpoints
// No mock data - show empty state when no real data is available

const Analytics: React.FC = () => {
  const [timeRange, setTimeRange] = useState('7d');

  // Use enhanced hooks for data fetching
  const timeRangeDays = timeRange === '7d' ? 7 : timeRange === '30d' ? 30 : 90;
  const { data: alertStats, isLoading: alertStatsLoading } = useAlertStatistics(timeRangeDays);
  const { data: indicatorDashboard, isLoading: indicatorLoading } = useIndicatorDashboard();

  const isLoading = alertStatsLoading || indicatorLoading;

  if (isLoading) {
    return <LoadingSpinner />;
  }

  return (
    <Box>
      <PageHeader
        title="Analytics & Insights"
        subtitle="Comprehensive monitoring system analytics and performance metrics"
        icon={<AnalyticsIcon />}
        secondaryActions={[
          {
            label: 'Export Report',
            icon: <Assessment />,
            onClick: () => {
              // TODO: Implement export functionality
              console.log('Export report');
            },
            gradient: 'info',
          },
        ]}
      />

      {/* Time Range Selector */}
      <Box sx={{ mb: 3 }}>
        <Select
          label="Time Range"
          value={timeRange}
          onChange={e => setTimeRange(e.target.value as string)}
          options={[
            { value: '7d', label: 'Last 7 Days' },
            { value: '30d', label: 'Last 30 Days' },
            { value: '90d', label: 'Last 90 Days' },
          ]}
          sx={{ minWidth: 200 }}
        />
      </Box>

      <Grid container spacing={3}>
        {/* Key Metrics Cards */}
        <MetricsOverview
          indicatorDashboard={indicatorDashboard}
          alertStats={alertStats}
        />

        {/* Alert Trends Chart */}
        <AlertTrendsChart alertStats={alertStats} />

        {/* Alert Severity Distribution */}
        <SeverityDistributionChart alertStats={alertStats} />

        {/* KPI Performance Table */}
        <PerformanceTable />
      </Grid>
    </Box>
  );
};

export default Analytics;
