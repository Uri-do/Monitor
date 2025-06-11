import React from 'react';
import { Grid, Typography, Box, Chip, Skeleton } from '@mui/material';
import { TrendingUp, TrendingDown, Warning, CheckCircle, PlayCircle } from '@mui/icons-material';
import { KpiDashboardDto, AlertDashboardDto } from '../../../types/api';
import { MetricCard } from '@/components';

interface KpiOverviewCardsProps {
  kpiDashboard?: KpiDashboardDto;
  alertDashboard?: AlertDashboardDto;
  kpiLoading: boolean;
  alertLoading: boolean;
}

const KpiOverviewCards: React.FC<KpiOverviewCardsProps> = ({
  kpiDashboard,
  alertDashboard,
  kpiLoading,
  alertLoading,
}) => {
  return (
    <>
      {/* Total KPIs Card */}
      <Grid item xs={12} sm={6} md={3}>
        <MetricCard
          title="Total KPIs"
          value={kpiLoading ? "..." : (kpiDashboard?.totalKpis || 0).toString()}
          gradient="primary"
          chip={{
            label: `${kpiDashboard?.activeKpis || 0} Active`,
            color: 'primary'
          }}
        />
      </Grid>

      {/* KPIs Due Card */}
      <Grid item xs={12} sm={6} md={3}>
        <MetricCard
          title="KPIs Due"
          value={kpiLoading ? "..." : (kpiDashboard?.kpisDue || 0).toString()}
          gradient={kpiDashboard?.kpisDue ? 'warning' : 'success'}
          chip={{
            label: kpiDashboard?.kpisDue ? "Needs Attention" : "All Up to Date",
            color: kpiDashboard?.kpisDue ? 'warning' : 'success'
          }}
        />
      </Grid>

      {/* KPIs Running Card */}
      <Grid item xs={12} sm={6} md={3}>
        <MetricCard
          title="KPIs Running"
          value={kpiLoading ? "..." : (kpiDashboard?.kpisRunning || 0).toString()}
          gradient={kpiDashboard?.kpisRunning ? 'info' : 'secondary'}
          chip={{
            label: kpiDashboard?.kpisRunning ? "Executing Now" : "All Idle",
            color: kpiDashboard?.kpisRunning ? 'info' : 'secondary'
          }}
        />
      </Grid>

      {/* Alerts Today Card */}
      <Grid item xs={12} sm={6} md={3}>
        <MetricCard
          title="Alerts Today"
          value={alertLoading ? "..." : (alertDashboard?.totalAlertsToday || 0).toString()}
          gradient="error"
          chip={alertDashboard?.alertTrendPercentage !== undefined ? {
            label: `${alertDashboard.alertTrendPercentage > 0 ? '+' : ''}${alertDashboard.alertTrendPercentage.toFixed(1)}%`,
            color: alertDashboard.alertTrendPercentage > 0 ? 'error' : 'success'
          } : undefined}
        />
      </Grid>
    </>
  );
};

export default KpiOverviewCards;
