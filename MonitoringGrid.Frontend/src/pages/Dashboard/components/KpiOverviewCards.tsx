import React from 'react';
import { Grid, Card, CardContent, Typography, Box, Chip, Skeleton } from '@mui/material';
import { TrendingUp, TrendingDown, Warning, CheckCircle, PlayCircle } from '@mui/icons-material';
import { KpiDashboardDto, AlertDashboardDto } from '../../../types/api';

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
        <Card sx={{
          background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
          color: 'white',
          position: 'relative',
          overflow: 'hidden',
          '&::before': {
            content: '""',
            position: 'absolute',
            top: 0,
            right: 0,
            width: '100px',
            height: '100px',
            background: 'rgba(255, 255, 255, 0.1)',
            borderRadius: '50%',
            transform: 'translate(30px, -30px)',
          }
        }}>
          <CardContent sx={{ position: 'relative', zIndex: 1 }}>
            <Typography variant="subtitle2" sx={{ opacity: 0.9, mb: 2 }}>
              Total KPIs
            </Typography>
            {kpiLoading ? (
              <Skeleton variant="text" width={80} height={60} sx={{ bgcolor: 'rgba(255, 255, 255, 0.2)' }} />
            ) : (
              <Typography variant="h3" sx={{ fontWeight: 700, mb: 2 }}>
                {kpiDashboard?.totalKpis || 0}
              </Typography>
            )}
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
          </CardContent>
        </Card>
      </Grid>

      {/* KPIs Due Card */}
      <Grid item xs={12} sm={6} md={3}>
        <Card sx={{
          background: kpiDashboard?.kpisDue
            ? 'linear-gradient(135deg, #ff9800 0%, #f57c00 100%)'
            : 'linear-gradient(135deg, #4caf50 0%, #388e3c 100%)',
          color: 'white',
          position: 'relative',
          overflow: 'hidden',
          '&::before': {
            content: '""',
            position: 'absolute',
            top: 0,
            right: 0,
            width: '80px',
            height: '80px',
            background: 'rgba(255, 255, 255, 0.1)',
            borderRadius: '50%',
            transform: 'translate(25px, -25px)',
          }
        }}>
          <CardContent sx={{ position: 'relative', zIndex: 1 }}>
            <Typography variant="subtitle2" sx={{ opacity: 0.9, mb: 2 }}>
              KPIs Due
            </Typography>
            {kpiLoading ? (
              <Skeleton variant="text" width={60} height={60} sx={{ bgcolor: 'rgba(255, 255, 255, 0.2)' }} />
            ) : (
              <Typography variant="h3" sx={{ fontWeight: 700, mb: 2 }}>
                {kpiDashboard?.kpisDue || 0}
              </Typography>
            )}
            <Box display="flex" alignItems="center">
              {kpiDashboard?.kpisDue ? (
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
              )}
            </Box>
          </CardContent>
        </Card>
      </Grid>

      {/* KPIs Running Card */}
      <Grid item xs={12} sm={6} md={3}>
        <Card sx={{
          background: kpiDashboard?.kpisRunning
            ? 'linear-gradient(135deg, #2196f3 0%, #1976d2 100%)'
            : 'linear-gradient(135deg, #9e9e9e 0%, #757575 100%)',
          color: 'white',
          position: 'relative',
          overflow: 'hidden',
          '&::before': {
            content: '""',
            position: 'absolute',
            top: 0,
            right: 0,
            width: '70px',
            height: '70px',
            background: 'rgba(255, 255, 255, 0.1)',
            borderRadius: '50%',
            transform: 'translate(20px, -20px)',
          }
        }}>
          <CardContent sx={{ position: 'relative', zIndex: 1 }}>
            <Typography variant="subtitle2" sx={{ opacity: 0.9, mb: 2 }}>
              KPIs Running
            </Typography>
            {kpiLoading ? (
              <Skeleton variant="text" width={60} height={60} sx={{ bgcolor: 'rgba(255, 255, 255, 0.2)' }} />
            ) : (
              <Typography variant="h3" sx={{ fontWeight: 700, mb: 2 }}>
                {kpiDashboard?.kpisRunning || 0}
              </Typography>
            )}
            <Box display="flex" alignItems="center">
              {kpiDashboard?.kpisRunning ? (
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
              )}
            </Box>
          </CardContent>
        </Card>
      </Grid>

      {/* Alerts Today Card */}
      <Grid item xs={12} sm={6} md={3}>
        <Card sx={{
          background: 'linear-gradient(135deg, #ff5722 0%, #d84315 100%)',
          color: 'white',
          position: 'relative',
          overflow: 'hidden',
          '&::before': {
            content: '""',
            position: 'absolute',
            top: 0,
            right: 0,
            width: '90px',
            height: '90px',
            background: 'rgba(255, 255, 255, 0.1)',
            borderRadius: '50%',
            transform: 'translate(30px, -30px)',
          }
        }}>
          <CardContent sx={{ position: 'relative', zIndex: 1 }}>
            <Typography variant="subtitle2" sx={{ opacity: 0.9, mb: 2 }}>
              Alerts Today
            </Typography>
            {alertLoading ? (
              <Skeleton variant="text" width={60} height={60} sx={{ bgcolor: 'rgba(255, 255, 255, 0.2)' }} />
            ) : (
              <Typography variant="h3" sx={{ fontWeight: 700, mb: 2 }}>
                {alertDashboard?.totalAlertsToday || 0}
              </Typography>
            )}
            <Box display="flex" alignItems="center">
              {alertDashboard?.alertTrendPercentage !== undefined && (
                <Chip
                  label={`${alertDashboard.alertTrendPercentage > 0 ? '+' : ''}${alertDashboard.alertTrendPercentage.toFixed(1)}%`}
                  size="small"
                  icon={alertDashboard.alertTrendPercentage > 0 ?
                    <TrendingUp sx={{ fontSize: '16px !important' }} /> :
                    <TrendingDown sx={{ fontSize: '16px !important' }} />
                  }
                  sx={{
                    backgroundColor: alertDashboard.alertTrendPercentage > 0
                      ? 'rgba(244, 67, 54, 0.2)'
                      : 'rgba(76, 175, 80, 0.2)',
                    color: 'white',
                    fontWeight: 600,
                  }}
                />
              )}
            </Box>
          </CardContent>
        </Card>
      </Grid>
    </>
  );
};

export default KpiOverviewCards;
