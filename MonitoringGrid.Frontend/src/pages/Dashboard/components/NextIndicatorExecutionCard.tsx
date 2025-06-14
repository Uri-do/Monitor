import React, { useState, useEffect } from 'react';
import {
  Grid,
  CardContent,
  Typography,
  Box,
  Chip,
  IconButton,
  Divider,
  Badge,
  useTheme,
  LinearProgress,
  Tooltip,
} from '@mui/material';
import {
  Schedule,
  PlayArrow,
  Timer,
  PlayCircle,
  AccessTime,
  Wifi,
  WifiOff,
} from '@mui/icons-material';
import { useNavigate } from 'react-router-dom';
import { format } from 'date-fns';
import { IndicatorDashboardDto } from '../../../types/api';
import {
  formatCountdownWithContext,
  getCountdownSeverity,
  shouldCountdownPulse,
} from '../../../utils/countdown';
import { Card } from '@/components';

interface NextIndicatorExecutionCardProps {
  indicatorDashboard?: IndicatorDashboardDto;
  countdown?: number | null;
  isConnected?: boolean;
}

const NextIndicatorExecutionCard: React.FC<NextIndicatorExecutionCardProps> = ({
  indicatorDashboard,
  countdown,
  isConnected = false,
}) => {
  const navigate = useNavigate();
  const theme = useTheme();

  const getCountdownProgress = (seconds: number, frequency: number): number => {
    if (seconds <= 0) return 100;
    const totalInterval = frequency * 60; // Convert minutes to seconds
    const elapsed = totalInterval - seconds;
    return Math.min((elapsed / totalInterval) * 100, 100);
  };

  const getStatusColor = (status: string | undefined) => {
    if (!status) return 'secondary';
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
        return 'secondary';
    }
  };

  return (
    <Grid item xs={12} md={6}>
      <Card sx={{ height: '100%' }}>
        <CardContent sx={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
          <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
            <Box display="flex" alignItems="center" gap={1}>
              <Schedule sx={{ color: theme => theme.palette.primary.main }} />
              <Typography variant="h6" sx={{ fontWeight: 600 }}>
                Next Indicator Execution
              </Typography>
              <Tooltip
                title={isConnected ? 'Real-time updates active' : 'Real-time updates disconnected'}
              >
                <Chip
                  icon={
                    isConnected ? (
                      <Wifi sx={{ fontSize: '14px !important' }} />
                    ) : (
                      <WifiOff sx={{ fontSize: '14px !important' }} />
                    )
                  }
                  label={isConnected ? 'Live' : 'Offline'}
                  size="small"
                  color={isConnected ? 'success' : 'error'}
                  sx={{
                    fontSize: '0.7rem',
                    height: 20,
                    animation: isConnected ? 'pulse 2s infinite' : 'none',
                    '@keyframes pulse': {
                      '0%': { opacity: 1 },
                      '50%': { opacity: 0.8 },
                      '100%': { opacity: 1 },
                    },
                  }}
                />
              </Tooltip>
            </Box>
            <IconButton
              size="small"
              onClick={() => navigate('/indicators')}
              sx={{
                backgroundColor: theme => theme.palette.primary.main,
                color: 'white',
                '&:hover': {
                  backgroundColor: theme => theme.palette.primary.dark,
                },
              }}
            >
              <PlayArrow />
            </IconButton>
          </Box>

          <Box sx={{ flexGrow: 1, overflow: 'hidden' }}>
            {indicatorDashboard?.nextIndicatorDue ? (
              <Box
                sx={{
                  p: 3,
                  borderRadius: 2,
                  background:
                    theme.palette.mode === 'light'
                      ? 'linear-gradient(135deg, #e3f2fd 0%, #bbdefb 100%)'
                      : 'linear-gradient(135deg, rgba(25, 118, 210, 0.1) 0%, rgba(25, 118, 210, 0.2) 100%)',
                  border: '1px solid',
                  borderColor: theme => theme.palette.primary.light,
                  position: 'relative',
                  overflow: 'hidden',
                }}
              >
                <Box display="flex" alignItems="center" gap={2} mb={2}>
                  <Badge
                    badgeContent={<Timer sx={{ fontSize: 12, color: 'white' }} />}
                    sx={{
                      '& .MuiBadge-badge': {
                        backgroundColor: theme =>
                          indicatorDashboard.nextIndicatorDue?.status === 'never_run'
                            ? theme.palette.warning.main
                            : theme.palette.primary.main,
                        color: 'white',
                      },
                    }}
                  >
                    <PlayCircle sx={{ fontSize: 32, color: theme => theme.palette.primary.main }} />
                  </Badge>
                  <Box>
                    <Typography
                      variant="h6"
                      sx={{ fontWeight: 600, color: theme => theme.palette.primary.dark }}
                    >
                      {indicatorDashboard.nextIndicatorDue?.indicatorName}
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      Status: {indicatorDashboard.nextIndicatorDue?.status}
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
                      {indicatorDashboard.nextIndicatorDue?.nextDue
                        ? format(
                            new Date(indicatorDashboard.nextIndicatorDue.nextDue),
                            'MMM dd, HH:mm:ss'
                          )
                        : 'N/A'}
                    </Typography>
                  </Box>
                  <Box textAlign="center" sx={{ minWidth: 120 }}>
                    <Typography variant="body2" color="text.secondary" gutterBottom>
                      Countdown
                    </Typography>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                      <AccessTime
                        sx={{
                          fontSize: 16,
                          color: theme =>
                            countdown !== null && countdown !== undefined && countdown <= 60
                              ? theme.palette.warning.main
                              : theme.palette.primary.main,
                          animation:
                            countdown !== null && countdown !== undefined && countdown <= 60
                              ? 'pulse 1s infinite'
                              : 'none',
                          '@keyframes pulse': {
                            '0%': { opacity: 1 },
                            '50%': { opacity: 0.7 },
                            '100%': { opacity: 1 },
                          },
                        }}
                      />
                      <Chip
                        label={formatCountdownWithContext(countdown ?? null).display}
                        color={getCountdownSeverity(countdown ?? null)}
                        aria-label={formatCountdownWithContext(countdown ?? null).ariaLabel}
                        sx={{
                          fontWeight: 600,
                          fontSize: '0.9rem',
                          animation: shouldCountdownPulse(countdown ?? null)
                            ? 'pulse 1s infinite'
                            : 'none',
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
                {countdown !== null && countdown !== undefined && countdown > 0 && (
                  <Box sx={{ mb: 2 }}>
                    <Box display="flex" justifyContent="space-between" alignItems="center" mb={1}>
                      <Typography variant="caption" color="text.secondary">
                        Progress to next execution
                      </Typography>
                      <Typography variant="caption" color="text.secondary">
                        {countdown !== null && countdown !== undefined
                          ? Math.round(
                              getCountdownProgress(
                                countdown,
                                60 // Default frequency since we don't have this data
                              )
                            )
                          : 0}
                        %
                      </Typography>
                    </Box>
                    <LinearProgress
                      variant="determinate"
                      value={
                        countdown !== null && countdown !== undefined
                          ? getCountdownProgress(countdown, 60) // Default frequency
                          : 0
                      }
                      sx={{
                        height: 6,
                        borderRadius: 3,
                        backgroundColor: theme.palette.mode === 'light' ? 'grey.200' : 'grey.700',
                        '& .MuiLinearProgress-bar': {
                          borderRadius: 3,
                          backgroundColor: theme =>
                            countdown !== null && countdown !== undefined && countdown <= 300
                              ? theme.palette.warning.main
                              : theme.palette.primary.main,
                        },
                      }}
                    />
                  </Box>
                )}

                <Box display="flex" alignItems="center" gap={1}>
                  <Chip
                    label={indicatorDashboard.nextIndicatorDue?.status || 'Unknown'}
                    color={getStatusColor(indicatorDashboard.nextIndicatorDue?.status)}
                    size="small"
                    sx={{ fontWeight: 600 }}
                  />
                  <Typography variant="caption" color="text.secondary">
                    Next execution scheduled • Priority:{' '}
                    {indicatorDashboard.nextIndicatorDue?.priority}
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
                  No upcoming executions
                </Typography>
                <Typography color="text.secondary" variant="caption">
                  {indicatorDashboard?.activeIndicators === 0
                    ? 'No active Indicators - add Indicators to start monitoring'
                    : indicatorDashboard?.activeIndicators === 1
                      ? 'One Indicator is active - check Indicator management for scheduling'
                      : 'All Indicators are inactive or have no schedule'}
                </Typography>
              </Box>
            )}
          </Box>
        </CardContent>
      </Card>
    </Grid>
  );
};

export default NextIndicatorExecutionCard;
