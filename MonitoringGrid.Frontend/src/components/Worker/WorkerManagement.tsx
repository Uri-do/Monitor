import React, { useState, useEffect } from 'react';
import {
  Card,
  CardContent,
  CardHeader,
  Typography,
  Button,
  Chip,
  Alert,
  Box,
  Divider,
  IconButton,
  Switch,
  FormControlLabel,
  CircularProgress,
  Grid,
  Paper,
  LinearProgress,
  Tooltip,
} from '@mui/material';
import {
  PlayArrow,
  Stop,
  Refresh,
  Schedule,
  Memory,
  Error as ErrorIcon,
  CheckCircle,
  Build,
  AutorenewOutlined,
  Wifi,
  WifiOff,
  Timer,
  TrendingUp,
  AccessTime,
} from '@mui/icons-material';
import { toast } from 'react-hot-toast';
import { useRealtime } from '../../contexts/RealtimeContext';
import { useRealtimeDashboard } from '../../hooks/useRealtimeDashboard';

interface WorkerService {
  name: string;
  status: string;
  lastActivity?: string;
  errorMessage?: string;
}

interface WorkerStatus {
  isRunning: boolean;
  mode: string;
  processId?: number;
  startTime?: string;
  uptime?: string;
  services: WorkerService[];
  timestamp: string;
}

interface WorkerActionResult {
  success: boolean;
  message: string;
  processId?: number;
  timestamp: string;
}

const WorkerManagement: React.FC = () => {
  const { isEnabled: realtimeEnabled, isConnected: signalRConnected, toggleRealtime } = useRealtime();
  const [status, setStatus] = useState<WorkerStatus | null>(null);
  const [loading, setLoading] = useState(false);
  const [actionLoading, setActionLoading] = useState<string | null>(null);
  const [autoRefresh, setAutoRefresh] = useState(false); // Disabled by default to prevent rate limiting

  // Real-time dashboard data
  const {
    workerStatus: realtimeWorkerStatus,
    runningKpis,
    countdown,
    nextKpiDue,
    isConnected: realtimeConnected,
    lastUpdate: realtimeLastUpdate,
    dashboardData,
  } = useRealtimeDashboard();

  const fetchStatus = async () => {
    try {
      const response = await fetch('/api/worker/status');
      if (response.ok) {
        const data = await response.json();
        setStatus(data);
      } else {
        console.error('Failed to fetch worker status');
      }
    } catch (error) {
      console.error('Error fetching worker status:', error);
    }
  };

  const performAction = async (action: string) => {
    setActionLoading(action);
    try {
      const response = await fetch(`/api/worker/${action}`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
      });

      const result: WorkerActionResult = await response.json();

      if (result.success) {
        toast.success(result.message);
        await fetchStatus(); // Refresh status

        // If worker was stopped, disable real-time features
        if (action === 'stop' && realtimeEnabled) {
          await toggleRealtime();
        }
      } else {
        toast.error(result.message);
      }
    } catch (error) {
      toast.error(`Failed to ${action} worker`);
      console.error(`Error ${action}ing worker:`, error);
    } finally {
      setActionLoading(null);
    }
  };

  const toggleRealtimeFeatures = async () => {
    try {
      await toggleRealtime();
      toast.success(`Real-time features ${realtimeEnabled ? 'disabled' : 'enabled'}`);
    } catch (error) {
      toast.error(`Failed to ${realtimeEnabled ? 'disable' : 'enable'} real-time features`);
      console.error('Real-time toggle error:', error);
    }
  };

  // Real-time uptime calculation
  const [realtimeUptime, setRealtimeUptime] = useState<string>('0h 0m');

  const formatUptime = (startTime?: string) => {
    if (!startTime) return 'N/A';

    const start = new Date(startTime);
    const now = new Date();
    const diff = now.getTime() - start.getTime();

    const hours = Math.floor(diff / (1000 * 60 * 60));
    const minutes = Math.floor((diff % (1000 * 60 * 60)) / (1000 * 60));

    return `${hours}h ${minutes}m`;
  };

  // Update uptime in real-time when worker is running
  useEffect(() => {
    if (!status?.isRunning || !status?.startTime) {
      setRealtimeUptime('0h 0m');
      return;
    }

    const updateUptime = () => {
      const uptime = formatUptime(status.startTime);
      setRealtimeUptime(uptime);
    };

    // Update immediately
    updateUptime();

    // Update every 30 seconds for real-time feel
    const interval = setInterval(updateUptime, 30000);

    return () => clearInterval(interval);
  }, [status?.isRunning, status?.startTime]);

  const formatDateTime = (dateTime?: string) => {
    if (!dateTime) return 'N/A';
    return new Date(dateTime).toLocaleString();
  };

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

  const getCountdownProgress = (seconds: number, frequencyMinutes: number): number => {
    if (seconds <= 0) return 100;
    const totalSeconds = frequencyMinutes * 60; // Convert minutes to seconds
    const elapsed = totalSeconds - seconds;
    return Math.max(0, Math.min(100, (elapsed / totalSeconds) * 100));
  };

  useEffect(() => {
    fetchStatus();

    if (autoRefresh) {
      const interval = setInterval(fetchStatus, 15000); // Refresh every 15 seconds to prevent rate limiting
      return () => clearInterval(interval);
    }
  }, [autoRefresh]);

  if (!status) {
    return (
      <Box sx={{ p: 3 }}>
        <Card>
          <CardHeader>
            <Typography
              variant="h4"
              component="h1"
              sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1 }}
            >
              <Build />
              Worker Management
            </Typography>
            <Typography variant="body2" color="text.secondary">
              Loading worker status...
            </Typography>
          </CardHeader>
          <CardContent>
            <Box sx={{ display: 'flex', justifyContent: 'center', p: 4 }}>
              <CircularProgress />
            </Box>
          </CardContent>
        </Card>
      </Box>
    );
  }

  return (
    <Box sx={{ p: 3 }}>
      <Card sx={{ mb: 3 }}>
        <CardHeader>
          <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
            <Box>
              <Typography
                variant="h4"
                component="h1"
                sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1 }}
              >
                <Build />
                Worker Management
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Control and monitor the MonitoringGrid Worker service
              </Typography>
            </Box>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
              <FormControlLabel
                control={
                  <Switch
                    checked={autoRefresh}
                    onChange={e => setAutoRefresh(e.target.checked)}
                    color="primary"
                  />
                }
                label="Auto Refresh"
              />
              <IconButton onClick={fetchStatus} disabled={loading} color="primary">
                {loading ? <CircularProgress size={20} /> : <Refresh />}
              </IconButton>
            </Box>
          </Box>
        </CardHeader>
        <CardContent>
          {/* Status Overview */}
          <Paper sx={{ p: 3, mb: 3, bgcolor: 'grey.50' }}>
            <Grid container spacing={3} alignItems="center">
              <Grid item xs={12} md={6}>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
                  {status.isRunning ? (
                    <Box sx={{ position: 'relative' }}>
                      <CheckCircle sx={{ color: 'success.main', fontSize: 32 }} />
                      {/* Pulsing indicator for active worker */}
                      <Box
                        sx={{
                          position: 'absolute',
                          top: -2,
                          right: -2,
                          width: 12,
                          height: 12,
                          borderRadius: '50%',
                          bgcolor: 'success.main',
                          animation: 'pulse 2s infinite',
                          '@keyframes pulse': {
                            '0%': { opacity: 1, transform: 'scale(1)' },
                            '50%': { opacity: 0.7, transform: 'scale(1.1)' },
                            '100%': { opacity: 1, transform: 'scale(1)' },
                          },
                        }}
                      />
                    </Box>
                  ) : (
                    <ErrorIcon sx={{ color: 'error.main', fontSize: 32 }} />
                  )}
                  <Box>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 0.5 }}>
                      <Typography variant="h6">Status:</Typography>
                      <Chip
                        label={status.isRunning ? 'Running' : 'Stopped'}
                        color={status.isRunning ? 'success' : 'error'}
                        size="small"
                        sx={{
                          animation: status.isRunning ? 'pulse 3s infinite' : 'none',
                          '@keyframes pulse': {
                            '0%': { opacity: 1 },
                            '50%': { opacity: 0.8 },
                            '100%': { opacity: 1 },
                          },
                        }}
                      />
                      {status.isRunning && realtimeEnabled && (
                        <Chip
                          label="LIVE"
                          size="small"
                          color="primary"
                          sx={{
                            fontSize: '0.7rem',
                            height: 20,
                            animation: 'pulse 2s infinite',
                            '@keyframes pulse': {
                              '0%': { opacity: 1 },
                              '50%': { opacity: 0.7 },
                              '100%': { opacity: 1 },
                            },
                          }}
                        />
                      )}
                    </Box>
                    <Typography variant="body2" color="text.secondary">
                      Mode: {status.mode}
                      {status.processId && ` (PID: ${status.processId})`}
                    </Typography>
                  </Box>
                </Box>
              </Grid>

              <Grid item xs={12} md={6}>
                <Box sx={{ textAlign: { xs: 'left', md: 'right' } }}>
                  <Box
                    sx={{
                      display: 'flex',
                      alignItems: 'center',
                      gap: 1,
                      justifyContent: { xs: 'flex-start', md: 'flex-end' },
                      mb: 0.5,
                    }}
                  >
                    <Schedule fontSize="small" sx={{
                      color: status.isRunning ? 'success.main' : 'text.secondary',
                      animation: status.isRunning && realtimeEnabled ? 'pulse 3s infinite' : 'none',
                      '@keyframes pulse': {
                        '0%': { opacity: 1 },
                        '50%': { opacity: 0.7 },
                        '100%': { opacity: 1 },
                      },
                    }} />
                    <Typography variant="body2" sx={{
                      fontWeight: status.isRunning ? 'medium' : 'normal',
                      color: status.isRunning ? 'success.main' : 'text.primary'
                    }}>
                      Uptime: {status.isRunning ? realtimeUptime : 'N/A'}
                    </Typography>
                    {status.isRunning && realtimeEnabled && (
                      <Box
                        sx={{
                          width: 6,
                          height: 6,
                          borderRadius: '50%',
                          bgcolor: 'success.main',
                          animation: 'pulse 1s infinite',
                          '@keyframes pulse': {
                            '0%': { opacity: 1, transform: 'scale(1)' },
                            '50%': { opacity: 0.5, transform: 'scale(1.2)' },
                            '100%': { opacity: 1, transform: 'scale(1)' },
                          },
                        }}
                      />
                    )}
                  </Box>
                  <Typography variant="body2" color="text.secondary">
                    Started: {formatDateTime(status.startTime)}
                  </Typography>
                </Box>
              </Grid>
            </Grid>
          </Paper>

          {/* Action Buttons */}
          <Box sx={{ display: 'flex', gap: 2, mb: 3, flexWrap: 'wrap' }}>
            <Button
              variant="contained"
              color="success"
              onClick={() => performAction('start')}
              disabled={
                status.isRunning || actionLoading === 'start' || status.mode === 'Integrated'
              }
              startIcon={actionLoading === 'start' ? <CircularProgress size={16} /> : <PlayArrow />}
            >
              {actionLoading === 'start' ? 'Starting...' : 'Start Worker'}
            </Button>

            <Button
              variant="contained"
              color="error"
              onClick={() => performAction('stop')}
              disabled={
                !status.isRunning || actionLoading === 'stop' || status.mode === 'Integrated'
              }
              startIcon={actionLoading === 'stop' ? <CircularProgress size={16} /> : <Stop />}
            >
              {actionLoading === 'stop' ? 'Stopping...' : 'Stop Worker'}
            </Button>

            <Button
              variant="outlined"
              onClick={() => performAction('restart')}
              disabled={
                !status.isRunning || actionLoading === 'restart' || status.mode === 'Integrated'
              }
              startIcon={
                actionLoading === 'restart' ? <CircularProgress size={16} /> : <AutorenewOutlined />
              }
            >
              {actionLoading === 'restart' ? 'Restarting...' : 'Restart Worker'}
            </Button>

            {/* Real-time Features Toggle */}
            <Button
              variant={realtimeEnabled ? "contained" : "outlined"}
              color={realtimeEnabled ? "primary" : "secondary"}
              onClick={toggleRealtimeFeatures}
              disabled={!status.isRunning || status.mode === 'Integrated'}
              startIcon={realtimeEnabled ? <Wifi /> : <WifiOff />}
            >
              {realtimeEnabled ? 'Disable Real-time' : 'Enable Real-time'}
            </Button>
          </Box>

          {/* Real-time Features Status */}
          {status.isRunning && status.mode !== 'Integrated' && (
            <Alert
              severity={realtimeEnabled ? "success" : "info"}
              sx={{ mb: 3 }}
              action={
                <Button
                  color="inherit"
                  size="small"
                  onClick={toggleRealtimeFeatures}
                  startIcon={realtimeEnabled ? <WifiOff /> : <Wifi />}
                >
                  {realtimeEnabled ? 'Disable' : 'Enable'}
                </Button>
              }
            >
              Real-time features are {realtimeEnabled ? 'enabled' : 'disabled'}.
              {realtimeEnabled
                ? ' Dashboard will show live updates and countdown timers.'
                : ' Enable to see live KPI execution status and countdown timers.'
              }
              {realtimeEnabled && (
                <Box sx={{ mt: 1, display: 'flex', alignItems: 'center', gap: 1 }}>
                  <Box
                    sx={{
                      width: 8,
                      height: 8,
                      borderRadius: '50%',
                      bgcolor: realtimeConnected ? 'success.main' : 'error.main',
                      animation: realtimeConnected ? 'pulse 2s infinite' : 'none',
                      '@keyframes pulse': {
                        '0%': { opacity: 1 },
                        '50%': { opacity: 0.5 },
                        '100%': { opacity: 1 },
                      },
                    }}
                  />
                  <Typography variant="caption">
                    {realtimeConnected ? 'Connected' : 'Disconnected'}
                  </Typography>
                  {realtimeLastUpdate && (
                    <Typography variant="caption" color="text.secondary">
                      • Last update: {formatDateTime(realtimeLastUpdate.toISOString())}
                    </Typography>
                  )}
                </Box>
              )}
            </Alert>
          )}

          {/* Integration Mode Alert */}
          {status.mode === 'Integrated' && (
            <Alert severity="info" sx={{ mb: 3 }}>
              Worker services are running in integrated mode. To control them separately, set{' '}
              <code>EnableWorkerServices</code> to <code>false</code> in configuration and restart
              the API.
            </Alert>
          )}

          {/* Real-time Monitoring Section */}
          {realtimeEnabled && realtimeConnected && (
            <>
              {/* Next KPI Execution Countdown */}
              {(nextKpiDue || dashboardData?.nextKpiDue) && countdown !== null && countdown !== undefined && (
                <Paper sx={{ p: 3, mb: 3, bgcolor: 'primary.50', border: 1, borderColor: 'primary.200' }}>
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 2 }}>
                    <Timer sx={{ color: 'primary.main' }} />
                    <Typography variant="h6" color="primary.main">
                      Next KPI Execution
                    </Typography>
                    <Chip
                      label="LIVE"
                      size="small"
                      color="success"
                      sx={{
                        animation: 'pulse 2s infinite',
                        '@keyframes pulse': {
                          '0%': { opacity: 1 },
                          '50%': { opacity: 0.7 },
                          '100%': { opacity: 1 },
                        },
                      }}
                    />
                  </Box>

                  <Grid container spacing={2} alignItems="center">
                    <Grid item xs={12} md={8}>
                      <Box>
                        <Typography variant="subtitle1" fontWeight="medium">
                          {nextKpiDue?.indicator || dashboardData?.nextKpiDue?.indicator}
                        </Typography>
                        <Typography variant="body2" color="text.secondary">
                          Owner: {nextKpiDue?.owner || dashboardData?.nextKpiDue?.owner} •
                          Frequency: {dashboardData?.nextKpiDue?.frequency || 30} min
                        </Typography>
                      </Box>
                    </Grid>
                    <Grid item xs={12} md={4}>
                      <Box sx={{ textAlign: { xs: 'left', md: 'right' } }}>
                        <Chip
                          label={countdown > 0 ? formatCountdown(countdown) : 'Due Now!'}
                          color={countdown <= 300 ? 'warning' : 'primary'}
                          sx={{
                            fontWeight: 600,
                            fontSize: '0.9rem',
                            animation: countdown <= 60 ? 'pulse 1s infinite' : 'none',
                          }}
                        />
                      </Box>
                    </Grid>
                  </Grid>

                  {/* Progress Bar */}
                  {countdown > 0 && (
                    <Box sx={{ mt: 2 }}>
                      <Box display="flex" justifyContent="space-between" alignItems="center" mb={1}>
                        <Typography variant="caption" color="text.secondary">
                          Progress to next execution
                        </Typography>
                        <Typography variant="caption" color="text.secondary">
                          {Math.round(getCountdownProgress(countdown, dashboardData?.nextKpiDue?.frequency || 30))}%
                        </Typography>
                      </Box>
                      <LinearProgress
                        variant="determinate"
                        value={getCountdownProgress(countdown, dashboardData?.nextKpiDue?.frequency || 30)}
                        sx={{
                          height: 6,
                          borderRadius: 3,
                          bgcolor: 'grey.200',
                          '& .MuiLinearProgress-bar': {
                            borderRadius: 3,
                            bgcolor: countdown <= 300 ? 'warning.main' : 'primary.main',
                          },
                        }}
                      />
                    </Box>
                  )}
                </Paper>
              )}

              {/* Running KPIs */}
              {runningKpis.length > 0 && (
                <Paper sx={{ p: 3, mb: 3, bgcolor: 'success.50', border: 1, borderColor: 'success.200' }}>
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 2 }}>
                    <TrendingUp sx={{ color: 'success.main' }} />
                    <Typography variant="h6" color="success.main">
                      Currently Executing KPIs ({runningKpis.length})
                    </Typography>
                    <Chip
                      label="LIVE"
                      size="small"
                      color="success"
                      sx={{
                        animation: 'pulse 2s infinite',
                        '@keyframes pulse': {
                          '0%': { opacity: 1 },
                          '50%': { opacity: 0.7 },
                          '100%': { opacity: 1 },
                        },
                      }}
                    />
                  </Box>

                  <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
                    {runningKpis.map((kpi, index) => (
                      <Box
                        key={index}
                        sx={{
                          p: 2,
                          bgcolor: 'background.paper',
                          borderRadius: 1,
                          border: 1,
                          borderColor: 'divider',
                        }}
                      >
                        <Grid container spacing={2} alignItems="center">
                          <Grid item xs={12} md={8}>
                            <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
                              <Box
                                sx={{
                                  width: 8,
                                  height: 8,
                                  borderRadius: '50%',
                                  bgcolor: 'success.main',
                                  animation: 'pulse 1s infinite',
                                }}
                              />
                              <Box>
                                <Typography variant="subtitle1" fontWeight="medium">
                                  {kpi.indicator}
                                </Typography>
                                <Typography variant="body2" color="text.secondary">
                                  Owner: {kpi.owner}
                                </Typography>
                              </Box>
                            </Box>
                          </Grid>
                          <Grid item xs={12} md={4}>
                            <Box sx={{ textAlign: { xs: 'left', md: 'right' } }}>
                              <Typography variant="body2" color="text.secondary">
                                Started: {formatDateTime(kpi.startTime)}
                              </Typography>
                              {kpi.progress !== undefined && (
                                <Box sx={{ mt: 1 }}>
                                  <LinearProgress
                                    variant="determinate"
                                    value={kpi.progress}
                                    sx={{ height: 4, borderRadius: 2 }}
                                  />
                                  <Typography variant="caption" color="text.secondary">
                                    {kpi.progress}% complete
                                  </Typography>
                                </Box>
                              )}
                            </Box>
                          </Grid>
                        </Grid>
                      </Box>
                    ))}
                  </Box>
                </Paper>
              )}

              {/* Real-time Status Indicator when no data */}
              {(!nextKpiDue && !dashboardData?.nextKpiDue && runningKpis.length === 0) && (
                <Paper sx={{ p: 3, mb: 3, bgcolor: 'info.50', border: 1, borderColor: 'info.200' }}>
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 1 }}>
                    <AccessTime sx={{ color: 'info.main' }} />
                    <Typography variant="h6" color="info.main">
                      Real-time Monitoring Active
                    </Typography>
                    <Chip
                      label="LIVE"
                      size="small"
                      color="success"
                      sx={{
                        animation: 'pulse 2s infinite',
                        '@keyframes pulse': {
                          '0%': { opacity: 1 },
                          '50%': { opacity: 0.7 },
                          '100%': { opacity: 1 },
                        },
                      }}
                    />
                  </Box>
                  <Typography variant="body2" color="text.secondary">
                    Waiting for KPI execution data... The system is monitoring for scheduled KPIs and will display countdown timers and execution status when available.
                  </Typography>
                </Paper>
              )}
            </>
          )}

          <Divider sx={{ mb: 3 }} />

          {/* Services Status */}
          <Box>
            <Typography variant="h6" sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 2 }}>
              <Memory />
              Worker Services ({status.services.length})
            </Typography>

            {status.services.length > 0 ? (
              <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
                {status.services.map((service, index) => (
                  <Paper key={index} sx={{ p: 2, border: 1, borderColor: 'divider' }}>
                    <Grid container spacing={2} alignItems="center">
                      <Grid item xs={12} md={8}>
                        <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
                          <Box
                            sx={{
                              width: 8,
                              height: 8,
                              borderRadius: '50%',
                              bgcolor: service.status === 'Running' ? 'success.main' : 'error.main',
                              animation: service.status === 'Running' && realtimeEnabled ? 'pulse 2s infinite' : 'none',
                              '@keyframes pulse': {
                                '0%': { opacity: 1, transform: 'scale(1)' },
                                '50%': { opacity: 0.6, transform: 'scale(1.3)' },
                                '100%': { opacity: 1, transform: 'scale(1)' },
                              },
                            }}
                          />
                          <Box>
                            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                              <Typography variant="subtitle1" fontWeight="medium">
                                {service.name}
                              </Typography>
                              {service.status === 'Running' && realtimeEnabled && (
                                <Chip
                                  label="ACTIVE"
                                  size="small"
                                  color="success"
                                  sx={{
                                    fontSize: '0.65rem',
                                    height: 18,
                                    animation: 'pulse 3s infinite',
                                    '@keyframes pulse': {
                                      '0%': { opacity: 1 },
                                      '50%': { opacity: 0.7 },
                                      '100%': { opacity: 1 },
                                    },
                                  }}
                                />
                              )}
                            </Box>
                            {service.errorMessage && (
                              <Typography variant="body2" color="error">
                                {service.errorMessage}
                              </Typography>
                            )}
                          </Box>
                        </Box>
                      </Grid>

                      <Grid item xs={12} md={4}>
                        <Box sx={{ textAlign: { xs: 'left', md: 'right' } }}>
                          <Chip
                            label={service.status}
                            color={service.status === 'Running' ? 'success' : 'error'}
                            size="small"
                            sx={{
                              mb: service.lastActivity ? 1 : 0,
                              animation: service.status === 'Running' && realtimeEnabled ? 'pulse 4s infinite' : 'none',
                              '@keyframes pulse': {
                                '0%': { opacity: 1 },
                                '50%': { opacity: 0.8 },
                                '100%': { opacity: 1 },
                              },
                            }}
                          />
                          {service.lastActivity && (
                            <Typography variant="body2" color="text.secondary">
                              Last: {formatDateTime(service.lastActivity)}
                            </Typography>
                          )}
                        </Box>
                      </Grid>
                    </Grid>
                  </Paper>
                ))}
              </Box>
            ) : (
              <Paper sx={{ p: 4, textAlign: 'center' }}>
                <Typography variant="body1" color="text.secondary">
                  No worker services running
                </Typography>
              </Paper>
            )}
          </Box>

          {/* Last Updated */}
          <Box sx={{ textAlign: 'center', mt: 3 }}>
            <Typography variant="caption" color="text.secondary">
              Last updated: {formatDateTime(status.timestamp)}
            </Typography>
          </Box>
        </CardContent>
      </Card>
    </Box>
  );
};

export default WorkerManagement;
