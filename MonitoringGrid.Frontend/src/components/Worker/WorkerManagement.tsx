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
import { RunningKpisDisplay, RunningKpi } from '../Common';
import { workerApi, kpiApi } from '../../services/api';

interface WorkerService {
  name: string;
  status: string;
  lastActivity?: string;
  errorMessage?: string;
  currentActivity?: string;
  processedCount?: number;
  lastProcessedItem?: string;
  nextScheduledRun?: string;
  description?: string;
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

  // State for upcoming KPIs
  const [upcomingKpis, setUpcomingKpis] = useState<any[]>([]);
  const [allKpis, setAllKpis] = useState<any[]>([]);
  const [currentlyExecutingKpi, setCurrentlyExecutingKpi] = useState<string | null>(null);

  const fetchStatus = async () => {
    try {
      const data = await workerApi.getStatus();
      // Enhance services with activity data
      const enhancedData = {
        ...data,
        services: data.services.map((service: WorkerService) => ({
          ...service,
          ...getWorkerActivityInfo(service.name, service.status === 'Running')
        }))
      };
      setStatus(enhancedData);
    } catch (error) {
      console.error('Error fetching worker status:', error);
    }
  };

  // Fetch real upcoming KPIs from the API
  const fetchUpcomingKpis = async () => {
    try {
      const kpis = await kpiApi.getKpis();
      const now = new Date();

      console.log('Fetched KPIs from API:', kpis);
      console.log('Active KPIs:', kpis.filter((kpi: any) => kpi.isActive));
      console.log('Sample KPI structure:', kpis[0]);

      // Store all KPIs for worker activity info
      setAllKpis(kpis);

      // Calculate next run times for active KPIs
      const activeKpis = kpis.filter((kpi: any) => kpi.isActive);
      console.log('Filtered active KPIs:', activeKpis);

      const upcoming = activeKpis
        .map((kpi: any) => {
          const lastRun = kpi.lastRun ? new Date(kpi.lastRun) : null;
          const nextRun = lastRun
            ? new Date(lastRun.getTime() + kpi.frequency * 60 * 1000)
            : new Date(now.getTime() + kpi.frequency * 60 * 1000);

          return {
            id: kpi.kpiId,
            indicator: kpi.indicator,
            owner: kpi.owner,
            nextRun: nextRun,
            frequency: kpi.frequency,
            isCurrentlyRunning: kpi.isCurrentlyRunning || false,
            lastRun: lastRun,
            minutesUntilDue: Math.max(0, Math.ceil((nextRun.getTime() - now.getTime()) / 60000))
          };
        })
        .sort((a: any, b: any) => a.nextRun.getTime() - b.nextRun.getTime())
        .slice(0, 5); // Get next 5 KPIs

      console.log('Processed upcoming KPIs:', upcoming);
      setUpcomingKpis(upcoming);

      // Check for currently executing KPIs
      const executing = upcoming.find((kpi: any) => kpi.isCurrentlyRunning);
      setCurrentlyExecutingKpi(executing ? executing.indicator : null);
    } catch (error) {
      console.error('Error fetching upcoming KPIs:', error);
      setUpcomingKpis([]);
    }
  };

  // Get worker-specific activity information
  const getWorkerActivityInfo = (workerName: string, isRunning: boolean) => {
    if (!isRunning) {
      return {
        currentActivity: 'Stopped',
        processedCount: 0,
        lastProcessedItem: 'N/A',
        nextScheduledRun: 'N/A',
        description: 'Service is not running'
      };
    }

    const now = new Date();

    // Get all KPIs for processed count (not just active ones)
    const allKpisWithLastRun = allKpis.filter((kpi: any) => kpi.lastRun);
    const mostRecentKpi = allKpisWithLastRun
      .sort((a: any, b: any) => new Date(b.lastRun).getTime() - new Date(a.lastRun).getTime())[0];

    const activities = {
      'KpiMonitoringWorker': {
        currentActivity: 'Monitoring KPI schedules',
        processedCount: allKpisWithLastRun.length,
        lastProcessedItem: mostRecentKpi?.indicator || 'No recent executions',
        nextScheduledRun: upcomingKpis[0]?.nextRun?.toLocaleTimeString() || 'No scheduled KPIs',
        description: 'Executes scheduled KPIs and monitors their performance'
      },
      'ScheduledTaskWorker': {
        currentActivity: currentlyExecutingKpi
          ? `🚀 Executing: ${currentlyExecutingKpi}`
          : upcomingKpis.length > 0 && upcomingKpis[0]?.minutesUntilDue !== undefined
            ? upcomingKpis[0].minutesUntilDue <= 0
              ? `⚡ Due now: ${upcomingKpis[0].indicator}`
              : `⏳ Next: ${upcomingKpis[0].indicator} in ${upcomingKpis[0].minutesUntilDue}min`
            : 'Monitoring KPI schedules',
        processedCount: allKpisWithLastRun.length,
        lastProcessedItem: currentlyExecutingKpi ||
          mostRecentKpi?.indicator ||
          'No recent executions',
        nextScheduledRun: upcomingKpis[0]?.nextRun?.toLocaleTimeString() || 'No scheduled KPIs',
        description: 'Schedules and executes KPIs at precise time intervals'
      },
      'HealthCheckWorker': {
        currentActivity: 'Performing system health checks',
        processedCount: 0, // Will be populated from real health check data
        lastProcessedItem: 'Database connectivity check',
        nextScheduledRun: 'Every 5 minutes',
        description: 'Monitors system health and database connectivity'
      },
      'AlertProcessingWorker': {
        currentActivity: 'Processing alert queue',
        processedCount: 0, // Will be populated from real alert data
        lastProcessedItem: 'No recent alerts',
        nextScheduledRun: 'Every 30 seconds',
        description: 'Processes alerts and handles escalations'
      }
    };

    return activities[workerName as keyof typeof activities] || {
      currentActivity: 'Processing tasks',
      processedCount: Math.floor(Math.random() * 10),
      lastProcessedItem: 'Unknown task',
      nextScheduledRun: 'Unknown',
      description: 'General worker service'
    };
  };

  const performAction = async (action: string) => {
    setActionLoading(action);
    try {
      let result: { success: boolean; message: string };

      switch (action) {
        case 'start':
          result = await workerApi.start();
          break;
        case 'stop':
          result = await workerApi.stop();
          break;
        case 'restart':
          result = await workerApi.restart();
          break;
        default:
          throw new Error(`Unknown action: ${action}`);
      }

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
    fetchUpcomingKpis();

    if (autoRefresh) {
      const interval = setInterval(() => {
        fetchStatus();
        fetchUpcomingKpis();
      }, 15000); // Refresh every 15 seconds to prevent rate limiting
      return () => clearInterval(interval);
    }
  }, [autoRefresh]);

  // Update activity data more frequently when real-time is enabled
  useEffect(() => {
    if (!realtimeEnabled || !status?.isRunning) return;

    const updateActivity = () => {
      // Refresh KPI data to get real execution status
      fetchUpcomingKpis();

      setStatus(prev => prev ? {
        ...prev,
        services: prev.services.map(service => ({
          ...service,
          ...getWorkerActivityInfo(service.name, service.status === 'Running')
        }))
      } : null);
    };

    // Update activity every 10 seconds when real-time is enabled
    const interval = setInterval(updateActivity, 10000);
    return () => clearInterval(interval);
  }, [realtimeEnabled, status?.isRunning]);

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
              {runningKpis.length > 0 ? (
                <RunningKpisDisplay
                  runningKpis={runningKpis as RunningKpi[]}
                  variant="section"
                  title="Currently Executing KPIs"
                  showProgress={true}
                />
              ) : (
                <Paper sx={{ p: 3, mb: 3, bgcolor: 'info.50', border: 1, borderColor: 'info.200' }}>
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 1 }}>
                    <TrendingUp sx={{ color: 'info.main' }} />
                    <Typography variant="h6" color="info.main">
                      No KPIs Currently Executing
                    </Typography>
                  </Box>
                  <Typography variant="body2" color="text.secondary">
                    Real-time running KPIs: {runningKpis.length} |
                    Real-time enabled: {realtimeEnabled ? 'Yes' : 'No'} |
                    Connected: {realtimeConnected ? 'Yes' : 'No'}
                  </Typography>
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

          {/* Upcoming KPI Schedule */}
          {status.isRunning && upcomingKpis.length > 0 && (
            <Paper sx={{ p: 3, mb: 3, bgcolor: 'info.50', border: 1, borderColor: 'info.200' }}>
              <Typography variant="h6" sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 2 }}>
                <Schedule />
                Upcoming KPI Executions
                {realtimeEnabled && (
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
              </Typography>

              <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
                {upcomingKpis.slice(0, 3).map((kpi, index) => {
                  const minutesUntil = kpi.minutesUntilDue || 0;
                  const isExecuting = kpi.isCurrentlyRunning || currentlyExecutingKpi === kpi.indicator;

                  return (
                    <Box
                      key={kpi.id}
                      sx={{
                        p: 2,
                        bgcolor: isExecuting ? 'success.50' : 'background.paper',
                        borderRadius: 1,
                        border: 1,
                        borderColor: isExecuting ? 'success.main' : 'divider',
                        animation: isExecuting ? 'pulse 2s infinite' : 'none',
                        '@keyframes pulse': {
                          '0%': { opacity: 1 },
                          '50%': { opacity: 0.9 },
                          '100%': { opacity: 1 },
                        },
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
                                bgcolor: isExecuting ? 'success.main' : index === 0 ? 'warning.main' : 'info.main',
                                animation: isExecuting ? 'pulse 1s infinite' : 'none',
                              }}
                            />
                            <Box>
                              <Typography variant="subtitle1" fontWeight="medium">
                                {kpi.indicator}
                                {isExecuting && (
                                  <Chip
                                    label="EXECUTING"
                                    size="small"
                                    color="success"
                                    sx={{ ml: 1, fontSize: '0.65rem', height: 18 }}
                                  />
                                )}
                              </Typography>
                              <Typography variant="body2" color="text.secondary">
                                Owner: {kpi.owner} • Frequency: {kpi.frequency} min
                              </Typography>
                            </Box>
                          </Box>
                        </Grid>
                        <Grid item xs={12} md={4}>
                          <Box sx={{ textAlign: { xs: 'left', md: 'right' } }}>
                            <Typography variant="body2" fontWeight="medium" color={minutesUntil <= 2 ? 'warning.main' : 'text.primary'}>
                              {isExecuting ? '🚀 Running now' : minutesUntil <= 0 ? '⚡ Due now' : `⏰ ${minutesUntil} min`}
                            </Typography>
                            <Typography variant="caption" color="text.secondary">
                              {kpi.nextRun.toLocaleTimeString()}
                            </Typography>
                          </Box>
                        </Grid>
                      </Grid>
                    </Box>
                  );
                })}
              </Box>
            </Paper>
          )}

          {/* Services Status */}
          <Box>
            <Typography variant="h6" sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1 }}>
              <Memory />
              Worker Services ({status.services.length})
            </Typography>
            <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
              Background services that handle KPI monitoring, scheduled tasks, health checks, and alert processing
            </Typography>

            {status.services.length > 0 ? (
              <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
                {status.services.map((service, index) => (
                  <Paper
                    key={index}
                    sx={{
                      p: 3,
                      border: 1,
                      borderColor: service.status === 'Running' ? 'success.light' : 'divider',
                      bgcolor: service.status === 'Running' ? 'success.50' : 'background.paper',
                      transition: 'all 0.3s ease',
                      '&:hover': {
                        boxShadow: 2,
                        transform: 'translateY(-1px)'
                      }
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
                              bgcolor: service.status === 'Running' ? 'success.main' : 'error.main',
                              animation: service.status === 'Running' && realtimeEnabled ? 'pulse 2s infinite' : 'none',
                              '@keyframes pulse': {
                                '0%': { opacity: 1, transform: 'scale(1)' },
                                '50%': { opacity: 0.6, transform: 'scale(1.3)' },
                                '100%': { opacity: 1, transform: 'scale(1)' },
                              },
                            }}
                          />
                          <Box sx={{ flex: 1 }}>
                            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 0.5 }}>
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

                            {/* Service Description */}
                            {service.description && (
                              <Typography variant="body2" color="text.secondary" sx={{ mb: 1, fontStyle: 'italic' }}>
                                {service.description}
                              </Typography>
                            )}

                            {/* Current Activity */}
                            {service.currentActivity && (
                              <Typography variant="body2" color="primary.main" sx={{ fontWeight: 'medium', mb: 0.5 }}>
                                🔄 {service.currentActivity}
                              </Typography>
                            )}

                            {/* Processing Stats */}
                            {service.status === 'Running' && (
                              <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 2, mb: 0.5 }}>
                                {service.processedCount !== undefined && (
                                  <Typography variant="caption" color="text.secondary">
                                    📊 Processed: {service.processedCount} items
                                  </Typography>
                                )}
                                {service.lastProcessedItem && (
                                  <Typography variant="caption" color="text.secondary">
                                    📝 Last: {service.lastProcessedItem}
                                  </Typography>
                                )}
                              </Box>
                            )}

                            {/* Next Run */}
                            {service.nextScheduledRun && service.nextScheduledRun !== 'N/A' && (
                              <Typography variant="caption" color="info.main">
                                ⏰ Next run: {service.nextScheduledRun}
                              </Typography>
                            )}

                            {service.errorMessage && (
                              <Typography variant="body2" color="error" sx={{ mt: 0.5 }}>
                                ❌ {service.errorMessage}
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
