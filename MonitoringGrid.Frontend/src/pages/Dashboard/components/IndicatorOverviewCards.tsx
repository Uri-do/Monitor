import React from 'react';
import { Grid, Typography, Box, Chip, Skeleton } from '@mui/material';
import { TrendingUp, TrendingDown, Warning, CheckCircle, PlayCircle } from '@mui/icons-material';
import { IndicatorDashboardDto, AlertDashboardDto } from '../../../types/api';
import { MetricCard } from '@/components';

interface IndicatorOverviewCardsProps {
  indicatorDashboard?: IndicatorDashboardDto;
  alertDashboard?: AlertDashboardDto;
  indicatorLoading: boolean;
  alertLoading: boolean;
}

const IndicatorOverviewCards: React.FC<IndicatorOverviewCardsProps> = ({
  indicatorDashboard,
  alertDashboard,
  indicatorLoading,
  alertLoading,
}) => {
  return (
    <>
      {/* Total Indicators Card */}
      <Grid item xs={12} sm={6} md={3}>
        <MetricCard
          title="Total Indicators"
          value={indicatorLoading ? '...' : (indicatorDashboard?.totalIndicators || 0).toString()}
          gradient="primary"
          chip={{
            label: `${indicatorDashboard?.activeIndicators || 0} Active`,
            color: 'primary',
          }}
        />
      </Grid>

      {/* Indicators Due Card */}
      <Grid item xs={12} sm={6} md={3}>
        <MetricCard
          title="Indicators Due"
          value={indicatorLoading ? '...' : (indicatorDashboard?.dueIndicators || 0).toString()}
          gradient={indicatorDashboard?.dueIndicators ? 'warning' : 'success'}
          chip={{
            label: indicatorDashboard?.dueIndicators ? 'Needs Attention' : 'All Up to Date',
            color: indicatorDashboard?.dueIndicators ? 'warning' : 'success',
          }}
        />
      </Grid>

      {/* Indicators Running Card */}
      <Grid item xs={12} sm={6} md={3}>
        <MetricCard
          title="Indicators Running"
          value={indicatorLoading ? '...' : (indicatorDashboard?.runningIndicators || 0).toString()}
          gradient={indicatorDashboard?.runningIndicators ? 'info' : 'secondary'}
          chip={{
            label: indicatorDashboard?.runningIndicators ? 'Executing Now' : 'All Idle',
            color: indicatorDashboard?.runningIndicators ? 'info' : 'secondary',
          }}
        />
      </Grid>

      {/* Alerts Today Card */}
      <Grid item xs={12} sm={6} md={3}>
        <MetricCard
          title="Alerts Today"
          value={alertLoading ? '...' : (alertDashboard?.totalAlertsToday || 0).toString()}
          gradient="error"
          chip={
            alertDashboard?.alertTrendPercentage !== undefined
              ? {
                  label: `${alertDashboard.alertTrendPercentage > 0 ? '+' : ''}${alertDashboard.alertTrendPercentage.toFixed(1)}%`,
                  color: alertDashboard.alertTrendPercentage > 0 ? 'error' : 'success',
                }
              : undefined
          }
        />
      </Grid>
    </>
  );
};

export default IndicatorOverviewCards;
