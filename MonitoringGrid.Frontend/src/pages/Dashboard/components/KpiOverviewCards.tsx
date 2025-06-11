import React from 'react';
import { Grid, Typography, Box, Chip, Skeleton } from '@mui/material';
import { TrendingUp, TrendingDown, Warning, CheckCircle, PlayCircle } from '@mui/icons-material';
import { KpiDashboardDto, AlertDashboardDto } from '../../../types/api';
import { UltimateMetricCard } from '@/components/UltimateEnterprise';

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
        <UltimateMetricCard
          title="Total KPIs"
          value={
            kpiLoading ? (
              <Skeleton
                variant="text"
                width={80}
                height={60}
                sx={{ bgcolor: 'rgba(255, 255, 255, 0.2)' }}
              />
            ) : (
              (kpiDashboard?.totalKpis || 0).toString()
            )
          }
          gradient="primary"
          chip={
            <Box display="flex" alignItems="center" gap={1} flexWrap="wrap">
              <Chip
                label={`${kpiDashboard?.activeKpis || 0} Active`}
                size="small"
                sx={{
                  backgroundColor: 'rgba(255, 255, 255, 0.2)',
                  color: 'white',
                  fontWeight: 600,
                }}
              />
              <Chip
                label={`${kpiDashboard?.inactiveKpis || 0} Inactive`}
                size="small"
                sx={{
                  backgroundColor: 'rgba(255, 255, 255, 0.1)',
                  color: 'white',
                  fontWeight: 600,
                }}
              />
            </Box>
          }
        />
      </Grid>

      {/* KPIs Due Card */}
      <Grid item xs={12} sm={6} md={3}>
        <UltimateMetricCard
          title="KPIs Due"
          value={
            kpiLoading ? (
              <Skeleton
                variant="text"
                width={60}
                height={60}
                sx={{ bgcolor: 'rgba(255, 255, 255, 0.2)' }}
              />
            ) : (
              (kpiDashboard?.kpisDue || 0).toString()
            )
          }
          gradient={kpiDashboard?.kpisDue ? 'warning' : 'success'}
          chip={
            kpiDashboard?.kpisDue ? (
              <Chip
                label="Needs Attention"
                size="small"
                icon={<Warning sx={{ fontSize: '16px !important' }} />}
                sx={{
                  backgroundColor: 'rgba(255, 255, 255, 0.2)',
                  color: 'white',
                  fontWeight: 600,
                }}
              />
            ) : (
              <Chip
                label="All Up to Date"
                size="small"
                icon={<CheckCircle sx={{ fontSize: '16px !important' }} />}
                sx={{
                  backgroundColor: 'rgba(255, 255, 255, 0.2)',
                  color: 'white',
                  fontWeight: 600,
                }}
              />
            )
          }
        />
      </Grid>

      {/* KPIs Running Card */}
      <Grid item xs={12} sm={6} md={3}>
        <UltimateMetricCard
          title="KPIs Running"
          value={
            kpiLoading ? (
              <Skeleton
                variant="text"
                width={60}
                height={60}
                sx={{ bgcolor: 'rgba(255, 255, 255, 0.2)' }}
              />
            ) : (
              (kpiDashboard?.kpisRunning || 0).toString()
            )
          }
          gradient={kpiDashboard?.kpisRunning ? 'info' : 'secondary'}
          chip={
            kpiDashboard?.kpisRunning ? (
              <Chip
                label="Executing Now"
                size="small"
                icon={<PlayCircle sx={{ fontSize: '16px !important' }} />}
                sx={{
                  backgroundColor: 'rgba(255, 255, 255, 0.2)',
                  color: 'white',
                  fontWeight: 600,
                  animation: 'pulse 2s infinite',
                  '@keyframes pulse': {
                    '0%': { opacity: 1 },
                    '50%': { opacity: 0.7 },
                    '100%': { opacity: 1 },
                  },
                }}
              />
            ) : (
              <Chip
                label="All Idle"
                size="small"
                icon={<CheckCircle sx={{ fontSize: '16px !important' }} />}
                sx={{
                  backgroundColor: 'rgba(255, 255, 255, 0.2)',
                  color: 'white',
                  fontWeight: 600,
                }}
              />
            )
          }
        />
      </Grid>

      {/* Alerts Today Card */}
      <Grid item xs={12} sm={6} md={3}>
        <UltimateMetricCard
          title="Alerts Today"
          value={
            alertLoading ? (
              <Skeleton
                variant="text"
                width={60}
                height={60}
                sx={{ bgcolor: 'rgba(255, 255, 255, 0.2)' }}
              />
            ) : (
              (alertDashboard?.totalAlertsToday || 0).toString()
            )
          }
          gradient="error"
          chip={
            alertDashboard?.alertTrendPercentage !== undefined && (
              <Chip
                label={`${alertDashboard.alertTrendPercentage > 0 ? '+' : ''}${alertDashboard.alertTrendPercentage.toFixed(1)}%`}
                size="small"
                icon={
                  alertDashboard.alertTrendPercentage > 0 ? (
                    <TrendingUp sx={{ fontSize: '16px !important' }} />
                  ) : (
                    <TrendingDown sx={{ fontSize: '16px !important' }} />
                  )
                }
                sx={{
                  backgroundColor:
                    alertDashboard.alertTrendPercentage > 0
                      ? 'rgba(244, 67, 54, 0.2)'
                      : 'rgba(76, 175, 80, 0.2)',
                  color: 'white',
                  fontWeight: 600,
                }}
              />
            )
          }
        />
      </Grid>
    </>
  );
};

export default KpiOverviewCards;
