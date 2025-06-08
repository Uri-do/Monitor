import React, { useState, useEffect } from 'react';
import {
  Grid,
  Card,
  CardContent,
  Typography,
  Box,
  Chip,
  IconButton,
  Divider,
  Badge,
  useTheme,
  LinearProgress,
} from '@mui/material';
import { Schedule, PlayArrow, Timer, PlayCircle, AccessTime } from '@mui/icons-material';
import { useNavigate } from 'react-router-dom';
import { format } from 'date-fns';
import { KpiDashboardDto } from '../../../types/api';

interface NextKpiExecutionCardProps {
  kpiDashboard?: KpiDashboardDto;
  countdown?: number | null;
}

const NextKpiExecutionCard: React.FC<NextKpiExecutionCardProps> = ({
  kpiDashboard,
  countdown,
}) => {
  const navigate = useNavigate();
  const theme = useTheme();

  const formatCountdown = (seconds: number): string => {
    if (seconds <= 0) return 'Due Now!';

    const days = Math.floor(seconds / (24 * 60 * 60));
    const hours = Math.floor((seconds % (24 * 60 * 60)) / (60 * 60));
    const mins = Math.floor((seconds % (60 * 60)) / 60);
    const secs = seconds % 60;

    if (days > 0) {
      return `${days}d ${hours}h ${mins}m`;
    } else if (hours > 0) {
      return `${hours}h ${mins}m ${secs}s`;
    } else if (mins > 0) {
      return `${mins}m ${secs}s`;
    } else {
      return `${secs}s`;
    }
  };

  const getCountdownProgress = (seconds: number, frequency: number): number => {
    if (seconds <= 0) return 100;
    const totalInterval = frequency * 60; // Convert minutes to seconds
    const elapsed = totalInterval - seconds;
    return Math.min((elapsed / totalInterval) * 100, 100);
  };

  const getStatusColor = (status: string) => {
    switch (status.toLowerCase()) {
      case 'running':
        return 'success';
      case 'due':
      case 'due soon':
        return 'warning';
      case 'error':
      case 'never run':
        return 'error';
      default:
        return 'default';
    }
  };

  return (
    <Grid item xs={12} md={6}>
      <Card sx={{ height: '100%' }}>
        <CardContent sx={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
          <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
            <Box display="flex" alignItems="center" gap={1}>
              <Schedule sx={{ color: 'primary.main' }} />
              <Typography variant="h6" sx={{ fontWeight: 600 }}>
                Next KPI Execution
              </Typography>
            </Box>
            <IconButton
              size="small"
              onClick={() => navigate('/kpis')}
              sx={{
                backgroundColor: 'primary.main',
                color: 'white',
                '&:hover': {
                  backgroundColor: 'primary.dark',
                },
              }}
            >
              <PlayArrow />
            </IconButton>
          </Box>

          <Box sx={{ flexGrow: 1, overflow: 'hidden' }}>
            {kpiDashboard?.nextKpiDue ? (
              <Box
                sx={{
                  p: 3,
                  borderRadius: 2,
                  background: theme.palette.mode === 'light'
                    ? 'linear-gradient(135deg, #e3f2fd 0%, #bbdefb 100%)'
                    : 'linear-gradient(135deg, rgba(25, 118, 210, 0.1) 0%, rgba(25, 118, 210, 0.2) 100%)',
                  border: '1px solid',
                  borderColor: 'primary.light',
                  position: 'relative',
                  overflow: 'hidden',
                }}
              >
                <Box display="flex" alignItems="center" gap={2} mb={2}>
                  <Badge
                    badgeContent={
                      <Timer sx={{ fontSize: 12, color: 'white' }} />
                    }
                    sx={{
                      '& .MuiBadge-badge': {
                        backgroundColor: kpiDashboard.nextKpiDue.status === 'Due Soon' ? 'warning.main' : 'primary.main',
                        color: 'white',
                      }
                    }}
                  >
                    <PlayCircle sx={{ fontSize: 32, color: 'primary.main' }} />
                  </Badge>
                  <Box>
                    <Typography variant="h6" sx={{ fontWeight: 600, color: 'primary.dark' }}>
                      {kpiDashboard.nextKpiDue.indicator}
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      Owner: {kpiDashboard.nextKpiDue.owner}
                    </Typography>
                  </Box>
                </Box>

                <Divider sx={{ my: 2 }} />

                <Box display="flex" alignItems="center" justifyContent="space-between" mb={2}>
                  <Box>
                    <Typography variant="body2" color="text.secondary" gutterBottom>
                      Next Run
                    </Typography>
                    <Typography variant="body1" sx={{ fontWeight: 600 }}>
                      {kpiDashboard.nextKpiDue.nextRun ? format(new Date(kpiDashboard.nextKpiDue.nextRun), 'MMM dd, HH:mm:ss') : 'N/A'}
                    </Typography>
                  </Box>
                  <Box textAlign="center" sx={{ minWidth: 120 }}>
                    <Typography variant="body2" color="text.secondary" gutterBottom>
                      Countdown
                    </Typography>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                      <AccessTime sx={{
                        fontSize: 16,
                        color: countdown !== null && countdown <= 60 ? 'warning.main' : 'primary.main',
                        animation: countdown !== null && countdown <= 60 ? 'pulse 1s infinite' : 'none',
                        '@keyframes pulse': {
                          '0%': { opacity: 1 },
                          '50%': { opacity: 0.7 },
                          '100%': { opacity: 1 },
                        },
                      }} />
                      <Chip
                        label={countdown !== null && countdown > 0 ? formatCountdown(countdown) : 'Due Now!'}
                        color={countdown !== null && countdown <= 300 ? 'warning' : 'primary'} // Warning if less than 5 minutes
                        sx={{
                          fontWeight: 600,
                          fontSize: '0.9rem',
                          animation: countdown !== null && countdown <= 60 ? 'pulse 1s infinite' : 'none',
                          '@keyframes pulse': {
                            '0%': { opacity: 1 },
                            '50%': { opacity: 0.7 },
                            '100%': { opacity: 1 },
                          },
                        }}
                      />
                    </Box>
                  </Box>
                </Box>

                {/* Progress Bar */}
                {countdown !== null && countdown > 0 && (
                  <Box sx={{ mb: 2 }}>
                    <Box display="flex" justifyContent="space-between" alignItems="center" mb={1}>
                      <Typography variant="caption" color="text.secondary">
                        Progress to next execution
                      </Typography>
                      <Typography variant="caption" color="text.secondary">
                        {Math.round(getCountdownProgress(countdown, kpiDashboard.nextKpiDue.frequency))}%
                      </Typography>
                    </Box>
                    <LinearProgress
                      variant="determinate"
                      value={getCountdownProgress(countdown, kpiDashboard.nextKpiDue.frequency)}
                      sx={{
                        height: 6,
                        borderRadius: 3,
                        backgroundColor: theme.palette.mode === 'light' ? 'grey.200' : 'grey.700',
                        '& .MuiLinearProgress-bar': {
                          borderRadius: 3,
                          backgroundColor: countdown <= 300 ? 'warning.main' : 'primary.main',
                        },
                      }}
                    />
                  </Box>
                )}

                <Box display="flex" alignItems="center" gap={1}>
                  <Chip
                    label={kpiDashboard.nextKpiDue.status}
                    color={getStatusColor(kpiDashboard.nextKpiDue.status)}
                    size="small"
                    sx={{ fontWeight: 600 }}
                  />
                  <Typography variant="caption" color="text.secondary">
                    {(kpiDashboard.nextKpiDue.minutesUntilDue ?? 0) <= 5 ? 'Executing soon...' : 'Scheduled'} •
                    Whole time scheduling ({kpiDashboard.nextKpiDue.frequency} min intervals)
                  </Typography>
                </Box>
              </Box>
            ) : (
              <Box
                display="flex"
                flexDirection="column"
                alignItems="center"
                justifyContent="center"
                py={4}
                sx={{
                  backgroundColor: theme.palette.mode === 'light' ? 'grey.50' : 'grey.900',
                  borderRadius: 2,
                  border: '2px dashed',
                  borderColor: theme.palette.mode === 'light' ? 'grey.300' : 'grey.700',
                }}
              >
                <Schedule sx={{ fontSize: 48, color: 'grey.400', mb: 2 }} />
                <Typography color="text.secondary" variant="body2" sx={{ fontWeight: 500 }}>
                  No KPIs scheduled
                </Typography>
                <Typography color="text.secondary" variant="caption">
                  All KPIs are inactive or have no schedule
                </Typography>
              </Box>
            )}
          </Box>
        </CardContent>
      </Card>
    </Grid>
  );
};

export default NextKpiExecutionCard;
