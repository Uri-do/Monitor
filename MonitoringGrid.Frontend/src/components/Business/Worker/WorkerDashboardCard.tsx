import React, { useState, useEffect } from 'react';
import {
  Card,
  CardContent,
  Typography,
  Button,
  Chip,
  Box,
  IconButton,
  CircularProgress,
  Grid,
  Tooltip,
  Alert,
  LinearProgress,
  Divider,
} from '@mui/material';
import {
  PlayArrow,
  Stop,
  Refresh,
  Build,
  CheckCircle,
  Error as ErrorIcon,
  Schedule,
  Memory,
  AutorenewOutlined,
  Timer,
  PlayCircle,
  Warning,
} from '@mui/icons-material';
import { useNavigate } from 'react-router-dom';
import { toast } from 'react-hot-toast';
import { WorkerStatusUpdate } from '@/services/signalRService';
import { useRealtimeDashboard } from '@/hooks/useRealtimeDashboard';
import { RunningIndicator } from '../Indicator/RunningIndicatorsDisplay';
import { workerApi } from '@/services/api';

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
  services: WorkerService[];
  timestamp: string;
}

interface WorkerDashboardCardProps {
  workerStatus?: WorkerStatusUpdate | null;
  realtimeEnabled?: boolean;
}

const WorkerDashboardCard: React.FC<WorkerDashboardCardProps> = ({
  workerStatus: realtimeWorkerStatus,
  realtimeEnabled = false,
}) => {
  const navigate = useNavigate();
  const [status, setStatus] = useState<WorkerStatus | null>(null);
  const [loading, setLoading] = useState(false);
  const [actionLoading, setActionLoading] = useState<string | null>(null);

  // Use real-time dashboard data
  const realtimeDashboard = useRealtimeDashboard();

  const fetchStatus = async () => {
    setLoading(true);
    try {
      const data = await workerApi.getStatus();
      setStatus(data);
    } catch (error) {
      console.error('Error fetching worker status:', error);
    } finally {
      setLoading(false);
    }
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
        case 'force-stop':
          result = await workerApi.forceStop();
          break;
        default:
          throw new Error(`Unknown action: ${action}`);
      }

      if (result.success) {
        toast.success(result.message);
        // Single status check for actions - rely on SignalR for real-time updates
        if (action === 'start' || action === 'restart') {
          setTimeout(fetchStatus, 2000);  // Single check after 2 seconds
        } else {
          await fetchStatus(); // Immediate check for stop action
        }
      } else {
        toast.error(result.message);
      }
    } catch (error: any) {
      // Try to extract the error message from the API response
      let errorMessage = `Failed to ${action} worker`;

      if (error?.response?.data?.message) {
        errorMessage = error.response.data.message;
      } else if (error?.response?.data?.Message) {
        errorMessage = error.response.data.Message;
      } else if (error?.message) {
        errorMessage = error.message;
      }

      toast.error(errorMessage);
      console.error(`Error ${action}ing worker:`, error);
    } finally {
      setActionLoading(null);
    }
  };

  const formatUptime = (startTime?: string) => {
    if (!startTime) return 'N/A';

    try {
      const start = new Date(startTime);
      const now = new Date();

      // Check if the date is valid
      if (isNaN(start.getTime())) {
        return 'Invalid Date';
      }

      const diff = now.getTime() - start.getTime();

      // If negative or very small, show as just started
      if (diff < 0 || diff < 60000) {
        // Less than 1 minute
        return 'Just started';
      }

      const hours = Math.floor(diff / (1000 * 60 * 60));
      const minutes = Math.floor((diff % (1000 * 60 * 60)) / (1000 * 60));

      if (hours === 0) {
        return `${minutes}m`;
      }

      return `${hours}h ${minutes}m`;
    } catch (error) {
      console.error('Error formatting uptime:', error);
      return 'Error';
    }
  };

  const formatCountdown = (seconds: number | null) => {
    if (seconds === null || seconds <= 0) return null;

    // Show seconds in the last 2 minutes (120 seconds)
    if (seconds <= 120) {
      return `${seconds}s`;
    }

    // Show minutes for longer durations
    const minutes = Math.ceil(seconds / 60);
    if (minutes < 60) {
      return `${minutes}m`;
    }

    // Show hours for very long durations
    const hours = Math.floor(minutes / 60);
    const remainingMinutes = minutes % 60;
    return `${hours}h ${remainingMinutes}m`;
  };

  // Track status changes for notifications
  const [previousStatus, setPreviousStatus] = React.useState<boolean | null>(null);

  // Merge real-time status with fetched status
  const currentStatus = React.useMemo(() => {
    if (realtimeWorkerStatus) {
      // Convert real-time status to WorkerStatus format
      return {
        isRunning: realtimeWorkerStatus.isRunning,
        mode: realtimeWorkerStatus.mode,
        processId: realtimeWorkerStatus.processId,
        startTime: realtimeWorkerStatus.lastHeartbeat, // Use lastHeartbeat as startTime approximation
        services: realtimeWorkerStatus.services,
        timestamp: realtimeWorkerStatus.lastHeartbeat,
      };
    }
    return status;
  }, [realtimeWorkerStatus, status]);

  // Notify on status changes
  React.useEffect(() => {
    if (currentStatus && previousStatus !== null && previousStatus !== currentStatus.isRunning) {
      if (currentStatus.isRunning) {
        toast.success('Worker service started successfully');
      } else {
        toast('Worker service stopped', { icon: 'ℹ️' });
      }
    }
    if (currentStatus) {
      setPreviousStatus(currentStatus.isRunning);
    }
  }, [currentStatus?.isRunning, previousStatus]);

  useEffect(() => {
    fetchStatus();

    // Only poll if real-time is not enabled - rely on SignalR for updates
    if (!realtimeEnabled) {
      const interval = setInterval(fetchStatus, 60000); // Poll every 60 seconds when SignalR is not available
      return () => clearInterval(interval);
    }
  }, [realtimeEnabled]);

  if (loading && !currentStatus) {
    return (
      <Card sx={{ height: '100%' }}>
        <CardContent sx={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
          <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
            <Box display="flex" alignItems="center" gap={1}>
              <Build sx={{ color: 'primary.main' }} />
              <Typography variant="h6" sx={{ fontWeight: 600 }}>
                Worker Service
              </Typography>
            </Box>
          </Box>
          <Box
            sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', flexGrow: 1 }}
          >
            <CircularProgress />
          </Box>
        </CardContent>
      </Card>
    );
  }

  return (
    <Card sx={{ height: '100%' }}>
      <CardContent sx={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
        <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
          <Box display="flex" alignItems="center" gap={1}>
            <Build sx={{ color: theme => theme.palette.primary.main }} />
            <Typography variant="h6" sx={{ fontWeight: 600 }}>
              Worker Service
            </Typography>
          </Box>
          <Box display="flex" alignItems="center" gap={1}>
            <IconButton
              size="small"
              onClick={fetchStatus}
              disabled={loading}
              sx={{
                backgroundColor: theme => theme.palette.primary.main,
                color: 'white',
                '&:hover': {
                  backgroundColor: theme => theme.palette.primary.dark,
                },
              }}
            >
              {loading ? <CircularProgress size={16} /> : <Refresh />}
            </IconButton>
            <IconButton
              size="small"
              onClick={() => navigate('/worker')}
              sx={{
                backgroundColor: 'secondary.main',
                color: 'white',
                '&:hover': {
                  backgroundColor: 'secondary.dark',
                },
              }}
            >
              <Build />
            </IconButton>
          </Box>
        </Box>

        <Box sx={{ flexGrow: 1 }}>
          {currentStatus ? (
            <>
              {/* Status Overview */}
              <Box sx={{ mb: 3 }}>
                <Grid container spacing={2} alignItems="center">
                  <Grid item xs={12} sm={6}>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                      {currentStatus.isRunning ? (
                        <CheckCircle
                          sx={{
                            color: 'success.main',
                            fontSize: 24,
                            animation: realtimeWorkerStatus ? 'pulse 2s infinite' : 'none',
                            '@keyframes pulse': {
                              '0%': { opacity: 1 },
                              '50%': { opacity: 0.7 },
                              '100%': { opacity: 1 },
                            },
                          }}
                        />
                      ) : (
                        <ErrorIcon sx={{ color: 'error.main', fontSize: 24 }} />
                      )}
                      <Box>
                        <Typography variant="subtitle1" sx={{ fontWeight: 600 }}>
                          {currentStatus.isRunning ? 'Running' : 'Stopped'}
                          {loading && (
                            <CircularProgress
                              size={12}
                              sx={{ ml: 1, color: 'primary.main' }}
                            />
                          )}
                          {realtimeWorkerStatus && !loading && (
                            <Chip
                              label="Live"
                              size="small"
                              color="success"
                              sx={{
                                ml: 1,
                                fontSize: '0.6rem',
                                height: 16,
                                animation: 'pulse 2s infinite',
                                '@keyframes pulse': {
                                  '0%': { opacity: 1 },
                                  '50%': { opacity: 0.8 },
                                  '100%': { opacity: 1 },
                                },
                              }}
                            />
                          )}
                        </Typography>
                        <Typography variant="caption" color="text.secondary">
                          {currentStatus.mode} Mode
                          {currentStatus.processId && ` (PID: ${currentStatus.processId})`}
                        </Typography>
                      </Box>
                    </Box>
                  </Grid>

                  <Grid item xs={12} sm={6}>
                    <Box sx={{ textAlign: { xs: 'left', sm: 'right' } }}>
                      <Box
                        sx={{
                          display: 'flex',
                          alignItems: 'center',
                          gap: 0.5,
                          justifyContent: { xs: 'flex-start', sm: 'flex-end' },
                        }}
                      >
                        <Schedule fontSize="small" />
                        <Typography variant="body2">
                          {realtimeWorkerStatus?.uptimeFormatted || formatUptime(currentStatus.startTime)}
                        </Typography>
                      </Box>
                      <Typography variant="caption" color="text.secondary">
                        Uptime
                        {realtimeWorkerStatus && (
                          <Chip
                            label="LIVE"
                            size="small"
                            color="success"
                            sx={{ ml: 0.5, fontSize: '0.5rem', height: 12 }}
                          />
                        )}
                      </Typography>
                    </Box>
                  </Grid>
                </Grid>
              </Box>

              {/* Action Buttons */}
              <Box sx={{ display: 'flex', gap: 1, mb: 3, flexWrap: 'wrap' }}>
                <Button
                  variant="contained"
                  color="success"
                  size="small"
                  onClick={() => performAction('start')}
                  disabled={
                    currentStatus.isRunning ||
                    actionLoading === 'start' ||
                    currentStatus.mode === 'Integrated'
                  }
                  startIcon={
                    actionLoading === 'start' ? <CircularProgress size={14} /> : <PlayArrow />
                  }
                >
                  Start
                </Button>

                <Button
                  variant="contained"
                  color="error"
                  size="small"
                  onClick={() => performAction('stop')}
                  disabled={
                    !currentStatus.isRunning ||
                    actionLoading === 'stop' ||
                    currentStatus.mode === 'Integrated'
                  }
                  startIcon={actionLoading === 'stop' ? <CircularProgress size={14} /> : <Stop />}
                >
                  Stop
                </Button>

                <Button
                  variant="outlined"
                  size="small"
                  onClick={() => performAction('restart')}
                  disabled={
                    !currentStatus.isRunning ||
                    actionLoading === 'restart' ||
                    currentStatus.mode === 'Integrated'
                  }
                  startIcon={
                    actionLoading === 'restart' ? (
                      <CircularProgress size={14} />
                    ) : (
                      <AutorenewOutlined />
                    )
                  }
                >
                  Restart
                </Button>

                <Tooltip title="Emergency stop - kills all worker processes">
                  <Button
                    variant="outlined"
                    color="error"
                    size="small"
                    onClick={() => performAction('force-stop')}
                    disabled={
                      actionLoading === 'force-stop' ||
                      currentStatus.mode === 'Integrated'
                    }
                    startIcon={
                      actionLoading === 'force-stop' ? (
                        <CircularProgress size={14} />
                      ) : (
                        <Warning />
                      )
                    }
                  >
                    Force Stop
                  </Button>
                </Tooltip>
              </Box>

              {/* Integration Mode Alert */}
              {currentStatus.mode === 'Integrated' && (
                <Alert severity="info" sx={{ mb: 2, fontSize: '0.8rem' }}>
                  Running in integrated mode. Use API restart to control.
                </Alert>
              )}

              {/* Real-time Status Alert */}
              {currentStatus.isRunning && currentStatus.mode !== 'Integrated' && (
                <Alert
                  severity={realtimeEnabled ? 'success' : 'info'}
                  sx={{ mb: 2, fontSize: '0.8rem' }}
                >
                  Real-time features: {realtimeEnabled ? 'Connected' : 'Disconnected'}
                  {!realtimeEnabled && ' - Go to Worker Management to enable'}
                </Alert>
              )}

              {/* Performance Metrics */}
              {currentStatus.isRunning && realtimeWorkerStatus && (
                <Box sx={{ mb: 3 }}>
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 2 }}>
                    <Build fontSize="small" />
                    <Typography variant="subtitle2" sx={{ fontWeight: 600 }}>
                      Performance Metrics
                    </Typography>
                    <Chip
                      label="LIVE"
                      size="small"
                      color="info"
                      sx={{ fontSize: '0.6rem', height: 16 }}
                    />
                  </Box>

                  <Grid container spacing={2}>
                    <Grid item xs={6}>
                      <Box sx={{ textAlign: 'center', p: 1, bgcolor: 'success.50', borderRadius: 1 }}>
                        <Typography variant="h6" sx={{ fontWeight: 'bold', color: 'success.main' }}>
                          {realtimeWorkerStatus.totalIndicatorsProcessed || 0}
                        </Typography>
                        <Typography variant="caption" color="text.secondary">
                          Total Processed
                        </Typography>
                      </Box>
                    </Grid>
                    <Grid item xs={6}>
                      <Box sx={{ textAlign: 'center', p: 1, bgcolor: 'info.50', borderRadius: 1 }}>
                        <Typography variant="h6" sx={{ fontWeight: 'bold', color: 'info.main' }}>
                          {realtimeWorkerStatus.successRate?.toFixed(1) || 0}%
                        </Typography>
                        <Typography variant="caption" color="text.secondary">
                          Success Rate
                        </Typography>
                      </Box>
                    </Grid>
                    <Grid item xs={6}>
                      <Box sx={{ textAlign: 'center', p: 1, bgcolor: 'success.50', borderRadius: 1 }}>
                        <Typography variant="body2" sx={{ fontWeight: 'bold', color: 'success.main' }}>
                          {realtimeWorkerStatus.successfulExecutions || 0}
                        </Typography>
                        <Typography variant="caption" color="text.secondary">
                          Successful
                        </Typography>
                      </Box>
                    </Grid>
                    <Grid item xs={6}>
                      <Box sx={{ textAlign: 'center', p: 1, bgcolor: 'error.50', borderRadius: 1 }}>
                        <Typography variant="body2" sx={{ fontWeight: 'bold', color: 'error.main' }}>
                          {realtimeWorkerStatus.failedExecutions || 0}
                        </Typography>
                        <Typography variant="caption" color="text.secondary">
                          Failed
                        </Typography>
                      </Box>
                    </Grid>
                  </Grid>

                  {realtimeWorkerStatus.currentActivity && (
                    <Box sx={{ mt: 2, p: 1, bgcolor: 'grey.50', borderRadius: 1 }}>
                      <Typography variant="caption" color="text.secondary">
                        Current Activity:
                      </Typography>
                      <Typography variant="body2" sx={{ fontWeight: 500 }}>
                        {realtimeWorkerStatus.currentActivity}
                      </Typography>
                    </Box>
                  )}
                </Box>
              )}

              {/* Services Summary */}
              <Box sx={{ mb: 3 }}>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1 }}>
                  <Memory fontSize="small" />
                  <Typography variant="subtitle2" sx={{ fontWeight: 600 }}>
                    Services ({currentStatus.services.length})
                  </Typography>
                </Box>

                {currentStatus.services.length > 0 ? (
                  <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.5 }}>
                    {currentStatus.services.slice(0, 4).map((service, index) => (
                      <Tooltip
                        key={index}
                        title={`${service.name}${service.description ? ` - ${service.description}` : ''}${service.processedCount ? ` (Processed: ${service.processedCount})` : ''}`}
                      >
                        <Chip
                          label={service.name.replace('MonitoringWorker', '').replace('Worker', '')}
                          color={service.status === 'Running' ? 'success' : 'error'}
                          size="small"
                          sx={{ fontSize: '0.7rem', height: 20 }}
                        />
                      </Tooltip>
                    ))}
                    {currentStatus.services.length > 4 && (
                      <Chip
                        label={`+${currentStatus.services.length - 4} more`}
                        size="small"
                        variant="outlined"
                        sx={{ fontSize: '0.7rem', height: 20 }}
                      />
                    )}
                  </Box>
                ) : (
                  <Typography variant="body2" color="text.secondary">
                    No services running
                  </Typography>
                )}
              </Box>

              {/* Countdown Timer */}
              {currentStatus.isRunning &&
                realtimeDashboard.countdown !== null &&
                realtimeDashboard.nextIndicatorDue && (
                  <>
                    <Divider sx={{ my: 2 }} />
                    <Box sx={{ mb: 3 }}>
                      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 2 }}>
                        <Timer fontSize="small" color="primary" />
                        <Typography variant="subtitle2" sx={{ fontWeight: 600 }}>
                          Next Indicator Execution
                        </Typography>
                      </Box>

                      <Box
                        sx={{
                          p: 2,
                          backgroundColor: 'primary.main',
                          borderRadius: 2,
                          color: 'white',
                          textAlign: 'center',
                        }}
                      >
                        <Typography variant="h4" sx={{ fontWeight: 'bold', mb: 1 }}>
                          {formatCountdown(realtimeDashboard.countdown)}
                        </Typography>
                        <Typography variant="body2" sx={{ opacity: 0.9 }}>
                          {realtimeDashboard.nextIndicatorDue.indicator}
                        </Typography>
                        <Typography variant="caption" sx={{ opacity: 0.7 }}>
                          by {realtimeDashboard.nextIndicatorDue.owner}
                        </Typography>
                      </Box>
                    </Box>
                  </>
                )}

              {/* Worker Executing Indicators */}
              {currentStatus.isRunning && realtimeDashboard.runningIndicators.length > 0 && (
                <>
                  <Divider sx={{ my: 2 }} />
                  <Box>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 2 }}>
                      <PlayCircle fontSize="small" color="warning" />
                      <Typography
                        variant="subtitle2"
                        sx={{ fontWeight: 600, color: 'warning.main' }}
                      >
                        Worker Executing ({realtimeDashboard.runningIndicators.length})
                      </Typography>
                      <Chip
                        label="LIVE"
                        size="small"
                        color="warning"
                        sx={{ fontSize: '0.7rem', height: 18 }}
                      />
                    </Box>

                    <Box>
                      {realtimeDashboard.runningIndicators.slice(0, 2).map(indicator => {
                        const runningIndicator: RunningIndicator = {
                          indicatorID: indicator.indicatorID,
                          indicator: indicator.indicator,
                          owner: indicator.owner,
                          startTime: indicator.startTime,
                          progress: indicator.progress,
                          currentStep: indicator.currentStep,
                          elapsedTime: indicator.elapsedTime,
                        };

                        return (
                          <Box
                            key={indicator.indicatorID}
                            sx={{
                              mb: 1.5,
                              p: 1.5,
                              border: 1,
                              borderColor: 'warning.main',
                              borderRadius: 1,
                              backgroundColor: 'warning.50',
                              position: 'relative',
                              '&::before': {
                                content: '""',
                                position: 'absolute',
                                left: 0,
                                top: 0,
                                bottom: 0,
                                width: 3,
                                bgcolor: 'warning.main',
                                borderRadius: '3px 0 0 3px',
                              },
                            }}
                          >
                            <Box
                              sx={{
                                display: 'flex',
                                justifyContent: 'space-between',
                                alignItems: 'center',
                                mb: 1,
                              }}
                            >
                              <Typography
                                variant="body2"
                                sx={{ fontWeight: 600, color: 'warning.dark' }}
                              >
                                ⚡ {runningIndicator.indicator}
                              </Typography>
                              <Chip
                                label="EXECUTING"
                                size="small"
                                color="warning"
                                sx={{ fontSize: '0.65rem', height: 18 }}
                              />
                            </Box>

                            <Typography variant="caption" color="text.secondary">
                              {runningIndicator.owner} • Started:{' '}
                              {new Date(runningIndicator.startTime).toLocaleTimeString()}
                            </Typography>

                            {runningIndicator.progress !== undefined && (
                              <Box sx={{ mt: 1 }}>
                                <Box
                                  sx={{
                                    display: 'flex',
                                    justifyContent: 'space-between',
                                    alignItems: 'center',
                                    mb: 0.5,
                                  }}
                                >
                                  <Typography variant="caption" color="text.secondary">
                                    Worker Progress
                                  </Typography>
                                  <Typography
                                    variant="caption"
                                    sx={{ fontWeight: 600, color: 'warning.dark' }}
                                  >
                                    {runningIndicator.progress}%
                                  </Typography>
                                </Box>
                                <LinearProgress
                                  variant="determinate"
                                  value={runningIndicator.progress}
                                  sx={{
                                    height: 4,
                                    borderRadius: 2,
                                    backgroundColor: 'warning.100',
                                    '& .MuiLinearProgress-bar': {
                                      backgroundColor: 'warning.main',
                                    },
                                  }}
                                />
                                {runningIndicator.currentStep && (
                                  <Typography
                                    variant="caption"
                                    color="text.secondary"
                                    sx={{ mt: 0.5, display: 'block' }}
                                  >
                                    {runningIndicator.currentStep}
                                  </Typography>
                                )}
                              </Box>
                            )}
                          </Box>
                        );
                      })}
                    </Box>

                    {realtimeDashboard.runningIndicators.length > 2 && (
                      <Typography variant="caption" color="text.secondary">
                        +{realtimeDashboard.runningIndicators.length - 2} more executing...
                      </Typography>
                    )}
                  </Box>
                </>
              )}
            </>
          ) : (
            <Box
              sx={{
                display: 'flex',
                flexDirection: 'column',
                alignItems: 'center',
                justifyContent: 'center',
                py: 4,
                textAlign: 'center',
              }}
            >
              <ErrorIcon sx={{ fontSize: 48, color: 'grey.400', mb: 2 }} />
              <Typography color="text.secondary" variant="body2">
                Unable to connect to Worker service
              </Typography>
              <Button size="small" onClick={fetchStatus} sx={{ mt: 1 }} startIcon={<Refresh />}>
                Retry
              </Button>
            </Box>
          )}
        </Box>
      </CardContent>
    </Card>
  );
};

export default WorkerDashboardCard;
