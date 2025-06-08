import React, { useState, useEffect } from 'react';
import {
  Grid,
  Card,
  CardContent,
  Typography,
  Box,
  Chip,
  List,
  ListItem,
  ListItemText,
  ListItemIcon,
  IconButton,
  LinearProgress,
  Skeleton,
  Divider,
  Tooltip,
  Badge,
  useTheme,
} from '@mui/material';
import {
  TrendingUp,
  TrendingDown,
  Warning,
  CheckCircle,
  Error,
  PlayArrow,
  Refresh,
  Schedule,
  Speed,
  Timer,
  AccessTime,
  PlayCircle,
  History,
} from '@mui/icons-material';
import { useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { kpiApi, alertApi } from '@/services/api';
import { format, formatDistanceToNow } from 'date-fns';

const Dashboard: React.FC = () => {
  const navigate = useNavigate();
  const theme = useTheme();
  const [countdown, setCountdown] = useState<number | null>(null);

  // Fetch dashboard data with more frequent refresh for real-time updates
  const { data: kpiDashboard, isLoading: kpiLoading, refetch: refetchKpi } = useQuery({
    queryKey: ['kpi-dashboard'],
    queryFn: kpiApi.getDashboard,
    refetchInterval: 15000, // Refresh every 15 seconds for real-time updates
  });

  const { data: alertDashboard, isLoading: alertLoading, refetch: refetchAlert } = useQuery({
    queryKey: ['alert-dashboard'],
    queryFn: alertApi.getDashboard,
    refetchInterval: 30000, // Refresh every 30 seconds
  });

  // Countdown timer for next KPI due
  useEffect(() => {
    if (kpiDashboard?.nextKpiDue?.minutesUntilDue !== undefined) {
      setCountdown(kpiDashboard.nextKpiDue.minutesUntilDue * 60); // Convert to seconds
    }
  }, [kpiDashboard?.nextKpiDue?.minutesUntilDue]);

  // Update countdown every second
  useEffect(() => {
    if (countdown === null || countdown <= 0) return;

    const timer = setInterval(() => {
      setCountdown(prev => {
        if (prev === null || prev <= 1) {
          // Refresh data when countdown reaches 0
          refetchKpi();
          return null;
        }
        return prev - 1;
      });
    }, 1000);

    return () => clearInterval(timer);
  }, [countdown, refetchKpi]);

  const handleRefresh = () => {
    refetchKpi();
    refetchAlert();
  };

  const formatCountdown = (seconds: number): string => {
    const mins = Math.floor(seconds / 60);
    const secs = seconds % 60;
    return `${mins}:${secs.toString().padStart(2, '0')}`;
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

  const getSeverityColor = (severity: string) => {
    switch (severity.toLowerCase()) {
      case 'critical':
        return 'error';
      case 'high':
        return 'warning';
      case 'medium':
        return 'info';
      case 'low':
        return 'success';
      default:
        return 'default';
    }
  };

  if (kpiLoading || alertLoading) {
    return (
      <Box>
        <Typography variant="h4" gutterBottom>
          Dashboard
        </Typography>
        <LinearProgress />
      </Box>
    );
  }

  return (
    <Box sx={{ maxWidth: '1400px', margin: '0 auto' }}>
      {/* Header */}
      <Box
        display="flex"
        justifyContent="space-between"
        alignItems="center"
        mb={4}
        sx={{
          background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
          borderRadius: 3,
          p: 3,
          color: 'white',
          boxShadow: '0px 4px 20px rgba(102, 126, 234, 0.3)',
        }}
      >
        <Box>
          <Box display="flex" alignItems="center" gap={2} mb={1}>
            <Typography variant="h4" sx={{ fontWeight: 700 }}>
              Dashboard
            </Typography>
            <Chip
              label="LIVE"
              size="small"
              sx={{
                backgroundColor: 'rgba(76, 175, 80, 0.2)',
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
          </Box>
          <Typography variant="subtitle1" sx={{ opacity: 0.9 }}>
            Monitor your KPIs and system health in real-time
          </Typography>
          {kpiDashboard?.lastUpdate && (
            <Typography variant="caption" sx={{ opacity: 0.8, mt: 0.5, display: 'block' }}>
              Last updated: {formatDistanceToNow(new Date(kpiDashboard.lastUpdate), { addSuffix: true })}
            </Typography>
          )}
        </Box>
        <Box display="flex" alignItems="center" gap={1}>
          {kpiDashboard?.nextKpiDue && countdown !== null && countdown > 0 && (
            <Tooltip title="Next KPI execution countdown">
              <Chip
                icon={<Timer sx={{ fontSize: '16px !important' }} />}
                label={formatCountdown(countdown)}
                size="small"
                sx={{
                  backgroundColor: 'rgba(255, 255, 255, 0.2)',
                  color: 'white',
                  fontWeight: 600,
                  mr: 1,
                }}
              />
            </Tooltip>
          )}
          <IconButton
            onClick={handleRefresh}
            sx={{
              color: 'white',
              backgroundColor: 'rgba(255, 255, 255, 0.1)',
              '&:hover': {
                backgroundColor: 'rgba(255, 255, 255, 0.2)',
              },
            }}
          >
            <Refresh />
          </IconButton>
        </Box>
      </Box>

      <Grid container spacing={3}>
        {/* KPI Overview Cards */}
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

        {/* Move Unresolved Alerts to second row */}

        {/* Running KPIs */}
        <Grid item xs={12} md={6}>
          <Card sx={{ height: '100%' }}>
            <CardContent sx={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
              <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
                <Box display="flex" alignItems="center" gap={1}>
                  <PlayCircle sx={{ color: 'primary.main' }} />
                  <Typography variant="h6" sx={{ fontWeight: 600 }}>
                    Currently Running KPIs
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
                {kpiDashboard?.runningKpis && kpiDashboard.runningKpis.length > 0 ? (
                  <List sx={{ p: 0 }}>
                    {kpiDashboard.runningKpis.map((kpi) => (
                      <ListItem
                        key={kpi.kpiId}
                        sx={{
                          borderRadius: 2,
                          mb: 1,
                          border: '1px solid',
                          borderColor: 'primary.light',
                          backgroundColor: 'primary.50',
                          '&:hover': {
                            backgroundColor: 'primary.100',
                            transform: 'translateX(4px)',
                            transition: 'all 0.2s ease-in-out',
                          },
                        }}
                      >
                        <ListItemIcon sx={{ minWidth: 'auto', mr: 2 }}>
                          <Box display="flex" alignItems="center" gap={1}>
                            <Chip
                              label="Running"
                              color="primary"
                              size="small"
                              icon={<PlayCircle sx={{ fontSize: '14px !important' }} />}
                              sx={{
                                fontWeight: 600,
                                animation: 'pulse 2s infinite',
                                '@keyframes pulse': {
                                  '0%': { opacity: 1 },
                                  '50%': { opacity: 0.7 },
                                  '100%': { opacity: 1 },
                                },
                              }}
                            />
                            {kpi.executionDurationSeconds && (
                              <Tooltip title="Execution Duration">
                                <Chip
                                  icon={<Timer sx={{ fontSize: '14px !important' }} />}
                                  label={`${Math.floor(kpi.executionDurationSeconds / 60)}:${(kpi.executionDurationSeconds % 60).toString().padStart(2, '0')}`}
                                  size="small"
                                  variant="outlined"
                                  sx={{
                                    fontSize: '0.7rem',
                                    height: 20,
                                    color: kpi.executionDurationSeconds > 300 ? 'warning.main' : 'text.secondary'
                                  }}
                                />
                              </Tooltip>
                            )}
                          </Box>
                        </ListItemIcon>
                        <ListItemText
                          primary={
                            <Typography variant="subtitle2" sx={{ fontWeight: 600, mb: 0.5 }}>
                              {kpi.indicator}
                            </Typography>
                          }
                          secondary={
                            <Box>
                              <Typography variant="body2" color="text.secondary" sx={{ mb: 0.5 }}>
                                Owner: {kpi.owner}
                              </Typography>
                              <Typography variant="caption" color="text.secondary">
                                Context: {kpi.executionContext || 'Manual'}
                                {kpi.executionStartTime && ` • Started: ${formatDistanceToNow(new Date(kpi.executionStartTime), { addSuffix: true })}`}
                              </Typography>
                            </Box>
                          }
                        />
                      </ListItem>
                    ))}
                  </List>
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
                    <PlayCircle sx={{ fontSize: 48, color: 'grey.400', mb: 2 }} />
                    <Typography color="text.secondary" variant="body2" sx={{ fontWeight: 500 }}>
                      No KPIs currently running
                    </Typography>
                    <Typography color="text.secondary" variant="caption">
                      All KPIs are idle
                    </Typography>
                  </Box>
                )}
              </Box>
            </CardContent>
          </Card>
        </Grid>

        {/* Next KPI Due */}
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
                      <Box textAlign="center">
                        <Typography variant="body2" color="text.secondary" gutterBottom>
                          Countdown
                        </Typography>
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

                    <Box display="flex" alignItems="center" gap={1}>
                      <Chip
                        label={kpiDashboard.nextKpiDue.status}
                        color={getStatusColor(kpiDashboard.nextKpiDue.status)}
                        size="small"
                        sx={{ fontWeight: 600 }}
                      />
                      <Typography variant="caption" color="text.secondary">
                        {(kpiDashboard.nextKpiDue.minutesUntilDue ?? 0) <= 5 ? 'Executing soon...' : 'Scheduled'}
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
                    <AccessTime sx={{ fontSize: 48, color: 'grey.400', mb: 2 }} />
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

        {/* Recent KPI Executions */}
        <Grid item xs={12} md={6}>
          <Card sx={{ height: '100%' }}>
            <CardContent sx={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
              <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
                <Box display="flex" alignItems="center" gap={1}>
                  <History sx={{ color: 'success.main' }} />
                  <Typography variant="h6" sx={{ fontWeight: 600 }}>
                    Recent Executions
                  </Typography>
                </Box>
                <IconButton
                  size="small"
                  onClick={() => navigate('/kpis')}
                  sx={{
                    backgroundColor: 'success.main',
                    color: 'white',
                    '&:hover': {
                      backgroundColor: 'success.dark',
                    },
                  }}
                >
                  <PlayArrow />
                </IconButton>
              </Box>

              <Box sx={{ flexGrow: 1, overflow: 'hidden' }}>
                {kpiDashboard?.recentExecutions && kpiDashboard.recentExecutions.length > 0 ? (
                  <List sx={{ p: 0 }}>
                    {kpiDashboard.recentExecutions.slice(0, 5).map((execution, index) => (
                      <ListItem
                        key={`${execution.kpiId}-${index}`}
                        sx={{
                          borderRadius: 2,
                          mb: 1,
                          border: '1px solid',
                          borderColor: 'divider',
                          backgroundColor: 'success.50',
                          '&:hover': {
                            backgroundColor: 'success.100',
                            transform: 'translateX(4px)',
                            transition: 'all 0.2s ease-in-out',
                          },
                        }}
                      >
                        <ListItemIcon sx={{ minWidth: 'auto', mr: 2 }}>
                          <Box display="flex" alignItems="center" gap={1}>
                            <Chip
                              label="Success"
                              color="success"
                              size="small"
                              sx={{ fontWeight: 600 }}
                            />
                            {execution.avgExecutionTimeMs && (
                              <Tooltip title="Execution Time">
                                <Chip
                                  icon={<Speed sx={{ fontSize: '14px !important' }} />}
                                  label={`${execution.avgExecutionTimeMs}ms`}
                                  size="small"
                                  variant="outlined"
                                  sx={{
                                    fontSize: '0.7rem',
                                    height: 20,
                                    color: (execution.avgExecutionTimeMs ?? 0) > 1000 ? 'warning.main' : 'text.secondary'
                                  }}
                                />
                              </Tooltip>
                            )}
                          </Box>
                        </ListItemIcon>
                        <ListItemText
                          primary={
                            <Typography variant="subtitle2" sx={{ fontWeight: 600, mb: 0.5 }}>
                              {execution.indicator}
                            </Typography>
                          }
                          secondary={
                            <Box>
                              <Typography variant="body2" color="text.secondary" sx={{ mb: 0.5 }}>
                                Value: {execution.totalExecutions}
                              </Typography>
                              <Typography variant="caption" color="text.secondary">
                                {formatDistanceToNow(new Date(), { addSuffix: true })}
                              </Typography>
                            </Box>
                          }
                        />
                      </ListItem>
                    ))}
                  </List>
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
                    <History sx={{ fontSize: 48, color: 'grey.400', mb: 2 }} />
                    <Typography color="text.secondary" variant="body2" sx={{ fontWeight: 500 }}>
                      No recent executions
                    </Typography>
                    <Typography color="text.secondary" variant="caption">
                      KPI execution history will appear here
                    </Typography>
                  </Box>
                )}
              </Box>
            </CardContent>
          </Card>
        </Grid>

        {/* Recent Alerts */}
        <Grid item xs={12} md={6}>
          <Card sx={{ height: '100%' }}>
            <CardContent sx={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
              <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
                <Box display="flex" alignItems="center" gap={1}>
                  <Warning sx={{ color: 'warning.main' }} />
                  <Typography variant="h6" sx={{ fontWeight: 600 }}>
                    Recent Alerts
                  </Typography>
                </Box>
                <IconButton
                  size="small"
                  onClick={() => navigate('/alerts')}
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
                {alertDashboard?.recentAlerts && alertDashboard.recentAlerts.length > 0 ? (
                  <List sx={{ p: 0 }}>
                    {alertDashboard.recentAlerts.slice(0, 5).map((alert) => (
                      <ListItem
                        key={alert.alertId}
                        button
                        onClick={() => navigate(`/alerts/${alert.alertId}`)}
                        sx={{
                          borderRadius: 2,
                          mb: 1,
                          border: '1px solid',
                          borderColor: 'divider',
                          backgroundColor: 'background.paper',
                          '&:hover': {
                            backgroundColor: 'action.hover',
                            transform: 'translateX(4px)',
                            transition: 'all 0.2s ease-in-out',
                          },
                        }}
                      >
                        <ListItemIcon sx={{ minWidth: 'auto', mr: 2 }}>
                          <Chip
                            label={alert.severity}
                            color={getSeverityColor(alert.severity)}
                            size="small"
                            sx={{ fontWeight: 600 }}
                          />
                        </ListItemIcon>
                        <ListItemText
                          primary={
                            <Typography variant="subtitle2" sx={{ fontWeight: 600, mb: 0.5 }}>
                              {alert.kpiIndicator}
                            </Typography>
                          }
                          secondary={
                            <Box>
                              <Typography variant="body2" color="text.secondary" sx={{ mb: 0.5 }}>
                                {alert.message}
                              </Typography>
                              <Typography variant="caption" color="text.secondary">
                                {format(new Date(alert.triggerTime), 'MMM dd, HH:mm')}
                              </Typography>
                            </Box>
                          }
                        />
                      </ListItem>
                    ))}
                  </List>
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
                    <CheckCircle sx={{ fontSize: 48, color: 'success.main', mb: 2 }} />
                    <Typography color="text.secondary" variant="body2" sx={{ fontWeight: 500 }}>
                      No recent alerts
                    </Typography>
                    <Typography color="text.secondary" variant="caption">
                      Your system is running smoothly
                    </Typography>
                  </Box>
                )}
              </Box>
            </CardContent>
          </Card>
        </Grid>

        {/* KPIs Due for Execution */}
        <Grid item xs={12} md={6}>
          <Card sx={{ height: '100%' }}>
            <CardContent sx={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
              <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
                <Box display="flex" alignItems="center" gap={1}>
                  <TrendingUp sx={{ color: 'info.main' }} />
                  <Typography variant="h6" sx={{ fontWeight: 600 }}>
                    KPIs Due for Execution
                  </Typography>
                </Box>
                <IconButton
                  size="small"
                  onClick={() => navigate('/kpis')}
                  sx={{
                    backgroundColor: 'info.main',
                    color: 'white',
                    '&:hover': {
                      backgroundColor: 'info.dark',
                    },
                  }}
                >
                  <PlayArrow />
                </IconButton>
              </Box>

              <Box sx={{ flexGrow: 1, overflow: 'hidden' }}>
                {kpiDashboard?.dueKpis && kpiDashboard.dueKpis.length > 0 ? (
                  <List sx={{ p: 0 }}>
                    {kpiDashboard.dueKpis.slice(0, 5).map((kpi) => (
                      <ListItem
                        key={kpi.kpiId}
                        button
                        onClick={() => navigate(`/kpis/${kpi.kpiId}`)}
                        sx={{
                          borderRadius: 2,
                          mb: 1,
                          border: '1px solid',
                          borderColor: 'divider',
                          backgroundColor: 'background.paper',
                          '&:hover': {
                            backgroundColor: 'action.hover',
                            transform: 'translateX(4px)',
                            transition: 'all 0.2s ease-in-out',
                          },
                        }}
                      >
                        <ListItemIcon sx={{ minWidth: 'auto', mr: 2 }}>
                          <Chip
                            label={kpi.status}
                            color={getStatusColor(kpi.status)}
                            size="small"
                            sx={{ fontWeight: 600 }}
                          />
                        </ListItemIcon>
                        <ListItemText
                          primary={
                            <Box display="flex" alignItems="center" gap={1} mb={0.5}>
                              <Typography variant="subtitle2" sx={{ fontWeight: 600 }}>
                                {kpi.indicator}
                              </Typography>
                              <Chip
                                label={kpi.isActive ? 'Active' : 'Inactive'}
                                color={kpi.isActive ? 'success' : 'default'}
                                size="small"
                                sx={{
                                  height: 20,
                                  fontSize: '0.7rem',
                                  fontWeight: 500,
                                }}
                              />
                            </Box>
                          }
                          secondary={
                            <Box>
                              <Typography variant="body2" color="text.secondary" sx={{ mb: 0.5 }}>
                                Owner: {kpi.owner}
                              </Typography>
                              <Typography variant="caption" color="text.secondary">
                                Frequency: {kpi.frequency} minutes
                                {kpi.lastRun && ` • Last run: ${format(new Date(kpi.lastRun), 'MMM dd, HH:mm')}`}
                              </Typography>
                            </Box>
                          }
                        />
                      </ListItem>
                    ))}
                  </List>
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
                    <CheckCircle sx={{ fontSize: 48, color: 'success.main', mb: 2 }} />
                    <Typography color="text.secondary" variant="body2" sx={{ fontWeight: 500 }}>
                      All KPIs are up to date
                    </Typography>
                    <Typography color="text.secondary" variant="caption">
                      No immediate action required
                    </Typography>
                  </Box>
                )}
              </Box>
            </CardContent>
          </Card>
        </Grid>

        {/* Top Alerting KPIs */}
        {alertDashboard?.topAlertingKpis && alertDashboard.topAlertingKpis.length > 0 && (
          <Grid item xs={12}>
            <Card sx={{
              background: theme.palette.mode === 'light'
                ? 'linear-gradient(135deg, #ff5722 0%, #d84315 100%)'
                : 'linear-gradient(135deg, rgba(244, 67, 54, 0.8) 0%, rgba(211, 47, 47, 0.8) 100%)',
              color: 'white',
            }}>
              <CardContent>
                <Box display="flex" alignItems="center" gap={1} mb={3}>
                  <Error sx={{ fontSize: 28 }} />
                  <Typography variant="h6" sx={{ fontWeight: 600 }}>
                    Top Alerting KPIs (Last 7 Days)
                  </Typography>
                </Box>
                <List sx={{ p: 0 }}>
                  {alertDashboard.topAlertingKpis.slice(0, 5).map((kpi) => (
                    <ListItem
                      key={kpi.kpiId}
                      button
                      onClick={() => navigate(`/kpis/${kpi.kpiId}`)}
                      sx={{
                        borderRadius: 2,
                        mb: 1,
                        backgroundColor: 'rgba(255, 255, 255, 0.1)',
                        border: '1px solid rgba(255, 255, 255, 0.2)',
                        '&:hover': {
                          backgroundColor: 'rgba(255, 255, 255, 0.2)',
                          transform: 'translateX(4px)',
                          transition: 'all 0.2s ease-in-out',
                        },
                      }}
                    >
                      <ListItemText
                        primary={
                          <Box display="flex" alignItems="center" gap={1} mb={1}>
                            <Typography variant="subtitle1" sx={{ fontWeight: 600, color: 'white' }}>
                              {kpi.indicator}
                            </Typography>
                            <Chip
                              label="Active"
                              size="small"
                              sx={{
                                backgroundColor: 'rgba(76, 175, 80, 0.3)',
                                color: 'white',
                                fontWeight: 600,
                                height: 20,
                                fontSize: '0.7rem',
                              }}
                            />
                          </Box>
                        }
                        secondary={
                          <Box display="flex" alignItems="center" gap={1} flexWrap="wrap">
                            <Typography variant="body2" sx={{ color: 'rgba(255, 255, 255, 0.8)' }}>
                              Owner: {kpi.owner}
                            </Typography>
                            <Chip
                              label={`${kpi.alertCount} alerts`}
                              size="small"
                              sx={{
                                backgroundColor: 'rgba(244, 67, 54, 0.3)',
                                color: 'white',
                                fontWeight: 600,
                              }}
                            />
                            {kpi.unresolvedCount > 0 && (
                              <Chip
                                label={`${kpi.unresolvedCount} unresolved`}
                                size="small"
                                sx={{
                                  backgroundColor: 'rgba(255, 152, 0, 0.3)',
                                  color: 'white',
                                  fontWeight: 600,
                                }}
                              />
                            )}
                          </Box>
                        }
                      />
                    </ListItem>
                  ))}
                </List>
              </CardContent>
            </Card>
          </Grid>
        )}
      </Grid>
    </Box>
  );
};

export default Dashboard;
