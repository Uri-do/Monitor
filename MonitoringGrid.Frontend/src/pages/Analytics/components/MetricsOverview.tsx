import React from 'react';
import { Grid } from '@mui/material';
import {
  Assessment,
  Warning,
  CheckCircle,
  Timeline,
} from '@mui/icons-material';
import { MetricCard } from '@/components/UI';
import { IndicatorDashboardDto, AlertStatisticsDto } from '@/types/api';

interface MetricsOverviewProps {
  indicatorDashboard?: IndicatorDashboardDto;
  alertStats?: AlertStatisticsDto;
}

export const MetricsOverview: React.FC<MetricsOverviewProps> = ({
  indicatorDashboard,
  alertStats,
}) => {
  return (
    <>
      {/* Key Metrics Cards */}
      <Grid item xs={12} sm={6} md={3}>
        <MetricCard
          title="Total Indicators"
          value={(indicatorDashboard?.totalIndicators || 0).toString()}
          icon={<Assessment />}
          gradient="primary"
          chip={{
            label: `${indicatorDashboard?.activeIndicators || 0} Active`,
            color: 'success',
          }}
        />
      </Grid>

      <Grid item xs={12} sm={6} md={3}>
        <MetricCard
          title="Alerts Today"
          value={(alertStats?.alertsToday || 0).toString()}
          icon={<Warning />}
          gradient="warning"
          chip={{
            label: `${alertStats?.unresolvedAlerts || 0} Unresolved`,
            color: 'error',
          }}
        />
      </Grid>

      <Grid item xs={12} sm={6} md={3}>
        <MetricCard
          title="Success Rate"
          value={alertStats?.successRate ? `${alertStats.successRate.toFixed(1)}%` : 'N/A'}
          icon={<CheckCircle />}
          gradient="success"
          subtitle="Based on recent executions"
        />
      </Grid>

      <Grid item xs={12} sm={6} md={3}>
        <MetricCard
          title="Avg Response"
          value={alertStats?.avgResponseTime ? `${alertStats.avgResponseTime}ms` : 'N/A'}
          icon={<Timeline />}
          gradient="info"
          subtitle="Average execution time"
        />
      </Grid>
    </>
  );
};

export default MetricsOverview;
