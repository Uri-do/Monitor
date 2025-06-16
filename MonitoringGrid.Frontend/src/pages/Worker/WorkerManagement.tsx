import React, { useState, useEffect } from 'react';
import {
  Box,
  Grid,
  Card,
  CardContent,
  Typography,
  Button,
  Chip,
  Alert,
  Divider,
  IconButton,
  Switch,
  FormControlLabel,
  CircularProgress,
  Paper,
  LinearProgress,
  Tooltip,
  Stack,
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
import { useRealtime } from '@/contexts/RealtimeContext';
import { useRealtimeDashboard } from '@/hooks/useRealtimeDashboard';
import RunningIndicatorsDisplay, {
  RunningIndicator,
} from '@/components/Business/Indicator/RunningIndicatorsDisplay';
import LiveExecutionLog from '@/components/Business/Indicator/LiveExecutionLog';
import { workerApi } from '@/services/api';
import { PageHeader, DataTable, StatusChip, LoadingSpinner } from '@/components/UI';

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
  const {
    isEnabled: realtimeEnabled,
    isConnected: signalRConnected,
    toggleRealtime,
  } = useRealtime();
  const [status, setStatus] = useState<WorkerStatus | null>(null);
  const [loading, setLoading] = useState(false);
  const [actionLoading, setActionLoading] = useState<string | null>(null);
  const [autoRefresh, setAutoRefresh] = useState(true); // Enable auto-refresh by default

  // Real-time dashboard data
  const {
    workerStatus: realtimeWorkerStatus,
    runningIndicators,
    countdown,
    nextIndicatorDue,
    isConnected: realtimeConnected,
    lastUpdate: realtimeLastUpdate,
    dashboardData,
  } = useRealtimeDashboard();

  // Fetch worker status
  const fetchStatus = async () => {
    setLoading(true);
    try {
      const response = await workerApi.getStatus();
      setStatus(response);
    } catch (error) {
      console.error('Failed to fetch worker status:', error);
      toast.error('Failed to fetch worker status');
    } finally {
      setLoading(false);
    }
  };

  // Auto-refresh effect
  useEffect(() => {
    fetchStatus();
  }, []);

  useEffect(() => {
    if (!autoRefresh) return;

    const interval = setInterval(fetchStatus, 3000); // More frequent polling when auto-refresh is enabled
    return () => clearInterval(interval);
  }, [autoRefresh]);

  // Perform worker actions
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

        // Add a delay for worker initialization before checking status
        if (action === 'start' || action === 'restart') {
          // Check status multiple times to catch the transition
          setTimeout(async () => {
            await fetchStatus();
          }, 1000); // First check after 1 second
          setTimeout(async () => {
            await fetchStatus();
          }, 3000); // Second check after 3 seconds
          setTimeout(async () => {
            await fetchStatus();
          }, 5000); // Final check after 5 seconds
        } else {
          await fetchStatus();
        }

        if (action === 'stop' && realtimeEnabled) {
          await toggleRealtime();
        }
      } else {
        toast.error(result.message);
      }
    } catch (error) {
      console.error(`Failed to ${action} worker:`, error);
      toast.error(`Failed to ${action} worker`);
    } finally {
      setActionLoading(null);
    }
  };

  const getStatusColor = (isRunning: boolean) => {
    return isRunning ? 'success' : 'error';
  };

  const getStatusText = (isRunning: boolean) => {
    return isRunning ? 'Running' : 'Stopped';
  };

  if (!status) {
    return (
      <Box>
        <PageHeader
          title="Worker Management"
          subtitle="Loading worker status..."
          icon={<Build />}
        />
        <Box sx={{ display: 'flex', justifyContent: 'center', p: 4 }}>
          <LoadingSpinner />
        </Box>
      </Box>
    );
  }

  return (
    <Box>
      <PageHeader
        title="Worker Management"
        subtitle="Control and monitor the MonitoringGrid Worker service"
        icon={<Build />}
        primaryAction={
          !status.isRunning
            ? {
                label: 'Start Worker',
                icon: actionLoading === 'start' ? <CircularProgress size={16} /> : <PlayArrow />,
                onClick: () => performAction('start'),
                gradient: 'success',
              }
            : {
                label: 'Stop Worker',
                icon: actionLoading === 'stop' ? <CircularProgress size={16} /> : <Stop />,
                onClick: () => performAction('stop'),
                gradient: 'error',
              }
        }
        secondaryActions={[
          {
            label: 'Restart',
            icon:
              actionLoading === 'restart' ? <CircularProgress size={16} /> : <AutorenewOutlined />,
            onClick: () => performAction('restart'),
            gradient: 'warning',
          },
        ]}
        onRefresh={fetchStatus}
        refreshing={loading}
      />

      <Grid container spacing={3}>
        {/* Status Overview */}
        <Grid item xs={12} md={6}>
          <Card>
            <CardContent>
              <Typography
                variant="h6"
                gutterBottom
                sx={{ display: 'flex', alignItems: 'center', gap: 1 }}
              >
                <CheckCircle />
                Worker Status
              </Typography>
              <Stack spacing={2}>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
                  <StatusChip status={getStatusText(status.isRunning)} />
                  {status.isRunning && (
                    <Box
                      sx={{
                        width: 12,
                        height: 12,
                        borderRadius: '50%',
                        bgcolor: 'success.main',
                        animation: 'pulse 2s infinite',
                        '@keyframes pulse': {
                          '0%': { opacity: 1 },
                          '50%': { opacity: 0.5 },
                          '100%': { opacity: 1 },
                        },
                      }}
                    />
                  )}
                </Box>
                <Typography variant="body2" color="text.secondary">
                  Mode: {status.mode}
                </Typography>
                {status.processId && (
                  <Typography variant="body2" color="text.secondary">
                    Process ID: {status.processId}
                  </Typography>
                )}
                {status.startTime && (
                  <Typography variant="body2" color="text.secondary">
                    Started: {new Date(status.startTime).toLocaleString()}
                  </Typography>
                )}
                {status.uptime && (
                  <Typography variant="body2" color="text.secondary">
                    Uptime: {status.uptime}
                  </Typography>
                )}
              </Stack>
            </CardContent>
          </Card>
        </Grid>

        {/* Real-time Connection Status */}
        <Grid item xs={12} md={6}>
          <Card>
            <CardContent>
              <Typography
                variant="h6"
                gutterBottom
                sx={{ display: 'flex', alignItems: 'center', gap: 1 }}
              >
                {signalRConnected ? <Wifi /> : <WifiOff />}
                Real-time Connection
              </Typography>
              <Stack spacing={2}>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
                  <StatusChip status={signalRConnected ? 'Connected' : 'Disconnected'} />
                  <FormControlLabel
                    control={
                      <Switch checked={realtimeEnabled} onChange={toggleRealtime} color="primary" />
                    }
                    label="Enable Real-time"
                  />
                </Box>
                {realtimeLastUpdate && (
                  <Typography variant="body2" color="text.secondary">
                    Last update: {new Date(realtimeLastUpdate).toLocaleString()}
                  </Typography>
                )}
              </Stack>
            </CardContent>
          </Card>
        </Grid>

        {/* Running Indicators */}
        {runningIndicators && runningIndicators.length > 0 && (
          <Grid item xs={12}>
            <Card>
              <CardContent>
                <Typography
                  variant="h6"
                  gutterBottom
                  sx={{ display: 'flex', alignItems: 'center', gap: 1 }}
                >
                  <TrendingUp />
                  Currently Running Indicators ({runningIndicators.length})
                </Typography>
                <RunningIndicatorsDisplay runningIndicators={runningIndicators} />
              </CardContent>
            </Card>
          </Grid>
        )}

        {/* Next Indicator Due */}
        {nextIndicatorDue && (
          <Grid item xs={12}>
            <Paper
              sx={{
                p: 3,
                bgcolor: 'warning.50',
                border: '1px solid',
                borderColor: 'warning.light',
              }}
            >
              <Typography
                variant="h6"
                gutterBottom
                sx={{ display: 'flex', alignItems: 'center', gap: 1 }}
              >
                <Timer />
                Next Indicator Execution
                {countdown !== null && countdown !== undefined && countdown > 0 && (
                  <Chip
                    label={`${Math.floor(countdown / 60)}:${(countdown % 60).toString().padStart(2, '0')}`}
                    size="small"
                    color="warning"
                  />
                )}
              </Typography>
              <Typography variant="subtitle1" fontWeight="medium">
                {nextIndicatorDue.indicator}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Owner: {nextIndicatorDue.owner} • Due in: {nextIndicatorDue.minutesUntilDue} min
              </Typography>
            </Paper>
          </Grid>
        )}

        {/* Live Execution Log - Only show when worker is running */}
        {status.isRunning && (
          <Grid item xs={12}>
            <LiveExecutionLog
              maxEntries={30}
              autoScroll={true}
              showOnlyErrors={false}
            />
          </Grid>
        )}

        {/* Worker Services */}
        <Grid item xs={12}>
          <Card>
            <CardContent>
              <Typography
                variant="h6"
                gutterBottom
                sx={{ display: 'flex', alignItems: 'center', gap: 1 }}
              >
                <Memory />
                Worker Services ({status.services.length})
              </Typography>
              <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
                Background services that handle Indicator monitoring, scheduled tasks, health
                checks, and alert processing
              </Typography>

              {status.services.length > 0 ? (
                <Stack spacing={2}>
                  {status.services.map((service, index) => (
                    <Paper
                      key={index}
                      sx={{
                        p: 3,
                        border: 1,
                        borderColor: service.status === 'Running' ? 'success.light' : 'divider',
                        bgcolor: service.status === 'Running' ? 'success.50' : 'background.paper',
                      }}
                    >
                      <Box
                        sx={{
                          display: 'flex',
                          justifyContent: 'space-between',
                          alignItems: 'flex-start',
                          mb: 1,
                        }}
                      >
                        <Typography variant="subtitle1" fontWeight="medium">
                          {service.name}
                        </Typography>
                        <StatusChip status={service.status} />
                      </Box>
                      {service.description && (
                        <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
                          {service.description}
                        </Typography>
                      )}
                      {service.currentActivity && (
                        <Typography variant="body2" sx={{ mb: 1 }}>
                          Current: {service.currentActivity}
                        </Typography>
                      )}
                      {service.lastActivity && (
                        <Typography variant="caption" color="text.secondary">
                          Last activity: {service.lastActivity}
                        </Typography>
                      )}
                      {service.errorMessage && (
                        <Alert severity="error" sx={{ mt: 1 }}>
                          {service.errorMessage}
                        </Alert>
                      )}
                    </Paper>
                  ))}
                </Stack>
              ) : (
                <Typography
                  variant="body2"
                  color="text.secondary"
                  sx={{ textAlign: 'center', py: 4 }}
                >
                  No worker services running
                </Typography>
              )}
            </CardContent>
          </Card>
        </Grid>
      </Grid>
    </Box>
  );
};

export default WorkerManagement;
